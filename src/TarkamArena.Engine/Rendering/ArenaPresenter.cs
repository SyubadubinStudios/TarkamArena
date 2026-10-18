using System.Numerics;
using TarkamArena.Core.Arena;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Engine.Audio;
using TarkamArena.Physics;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

public sealed record PresenterOptions(bool Weather = true, bool LowQuality = false, bool Physics = true, int NpcCount = -1);

/// <summary>
/// The 3D view of a match. It owns the ThreeNet scene and turns <see cref="MatchState"/> plus its
/// event stream into fighters, stage, crowd, props, particles, camera work and sound. It only reads
/// the simulation; nothing here can change the outcome of a fight.
/// </summary>
public sealed class ArenaPresenter : IDisposable
{
    private readonly MatchContext _context;
    private readonly MeshCache _cache;
    private readonly FighterModel[] _fighters;
    private readonly StageRig _rig;
    private readonly NpcDirector _npcs;
    private readonly CrowdRenderer _crowd;
    private readonly ArenaPhysics? _physics;
    private readonly PropRenderer? _props;
    private readonly VfxSystem _vfx;
    private readonly CameraDirector _camera;
    private readonly Node[] _projectiles;
    private readonly Node _hazard;
    private readonly Node _hazardWarning;
    private readonly (Vector3 Position, string Id)[] _fighterSpots;
    private readonly Random _random = new(9);
    private float _time;
    private float _flash;
    private int _lastFrame = -1;
    private readonly Vector3[] _pulserScale;

    public Scene Scene { get; }
    public Node Camera => _camera.Camera;
    public AudioDirector? Audio { get; set; }

    /// <summary>Raised for every simulation event (once per simulated frame), for the HUD.</summary>
    public event Action<MatchEvent>? EventRaised;

    public ArenaPresenter(MatchContext context, PresenterOptions? options = null)
    {
        options ??= new PresenterOptions();
        _context = context;
        Scene = new Scene();
        _cache = new MeshCache(Scene);
        var stage = context.Stage;

        if (options.Physics) _physics = new ArenaPhysics(stage.HalfWidth, multithreaded: true);
        _rig = StageBuilder.Build(Scene, _cache, stage, _physics, options.Weather);
        if (_physics is not null) _props = new PropRenderer(Scene, _cache);
        _pulserScale = [.. _rig.Pulsers.Select(n => n.Scale)];

        int npcCount = options.NpcCount >= 0 ? options.NpcCount : options.LowQuality ? stage.CrowdDensity / 2 : stage.CrowdDensity;
        _npcs = new NpcDirector(stage, npcCount, (int)context.Setup.Seed);
        _crowd = new CrowdRenderer(Scene, _cache, stage, npcCount);

        _fighters = [.. context.Characters.Select(c => FighterModel.Create(Scene, _cache, c))];
        _fighterSpots = new (Vector3, string)[_fighters.Length];

        _vfx = new VfxSystem(Scene, stage, _fighters.Length, options.Weather, options.LowQuality);
        _camera = new CameraDirector(Scene);

        _projectiles = new Node[MatchState.MaxProjectiles];
        for (int i = 0; i < _projectiles.Length; i++)
        {
            _projectiles[i] = Scene.AddMesh(_cache.Sphere, _cache.Pbr(Palette.Spanduk, 0.3f, 0, 4f), name: "projectile");
            _projectiles[i].Visible = false;
            _projectiles[i].CastShadow = false;
        }

        _hazard = Scene.CreateNode(name: "hazard");
        BuildHazardVisual(stage);
        _hazard.Visible = false;
        _hazardWarning = _cache.Add(_cache.Plane, _cache.Unlit(Palette.Cabai, 0.45f), Vector3.Zero, Vector3.One, euler: new Vector3(-MathF.PI / 2, 0, 0));
        _hazardWarning.Visible = false;
    }

    public FighterModel Fighter(int slot) => _fighters[slot];

    private void BuildHazardVisual(StageDefinition stage)
    {
        // One visual per stage that represents whatever its fight-affecting event is.
        var kind = stage.Events.FirstOrDefault(e => e.AffectsFight).Kind;
        switch (kind)
        {
            case ArenaEventKind.FallingTree:
                _cache.Add(_cache.Cylinder, _cache.Pbr(0x5A3A1E, 0.9f), new Vector3(0, 1.5f, 0), new Vector3(0.6f, 3f, 0.6f), _hazard);
                _cache.Add(_cache.Sphere, _cache.Pbr(0x3A6A2A, 0.8f), new Vector3(0, 3.4f, 0), new Vector3(2.4f), _hazard);
                break;
            case ArenaEventKind.FruitThrow:
                _cache.Add(_cache.Sphere, _cache.Pbr(0x9AAA3A, 0.6f), new Vector3(0, 0.4f, 0), new Vector3(0.6f), _hazard);
                break;
            case ArenaEventKind.BigWave or ArenaEventKind.WaterCannon or ArenaEventKind.WaterBomb:
                _cache.Add(_cache.Box, _cache.Unlit(0x9AD0F0, 0.6f), new Vector3(0, 0.7f, 0), new Vector3(1.6f, 1.4f, 3f), _hazard);
                break;
            case ArenaEventKind.FireSpread or ArenaEventKind.Eruption:
                _cache.Add(_cache.Cone, _cache.Pbr(0xFF6A1A, 0.4f, 0, 6f), new Vector3(0, 0.9f, 0), new Vector3(1.4f, 1.8f, 1.4f), _hazard);
                break;
            case ArenaEventKind.BassDrop:
                _cache.Add(_cache.Torus, _cache.Pbr(0xFF3EA5, 0.3f, 0, 5f), new Vector3(0, 0.5f, 0), new Vector3(2.2f, 2.2f, 2.2f), _hazard, new Vector3(MathF.PI / 2, 0, 0));
                break;
            case ArenaEventKind.MotorbikeSwarm:
                for (int i = 0; i < 3; i++)
                    _cache.Add(_cache.Box, _cache.Pbr(0x00A651, 0.4f, 0.3f), new Vector3(i * 0.8f - 0.8f, 0.6f, i * 0.5f - 0.5f), new Vector3(1.6f, 0.8f, 0.4f), _hazard);
                break;
            case ArenaEventKind.RagingGoat:
                _cache.Add(_cache.Sphere, _cache.Pbr(0xDCD6C8, 0.8f), new Vector3(0, 0.7f, 0), new Vector3(1.3f, 0.8f, 0.7f), _hazard);
                _cache.Add(_cache.Cone, _cache.Pbr(0x6A5A4E, 0.5f), new Vector3(0.6f, 1.2f, 0), new Vector3(0.15f, 0.4f, 0.15f), _hazard);
                break;
            default:
                // Carts, buses, passenger rushes, the ogoh-ogoh spin.
                _cache.Add(_cache.Box, _cache.Pbr(0x8C5A2B, 0.8f), new Vector3(0, 0.75f, 0), new Vector3(1.4f, 1.0f, 1.0f), _hazard);
                foreach (float x in new[] { -0.5f, 0.5f })
                    _cache.Add(_cache.Cylinder, _cache.Pbr(0x222222, 0.9f), new Vector3(x, 0.25f, 0.55f), new Vector3(0.5f, 0.1f, 0.5f), _hazard, new Vector3(MathF.PI / 2, 0, 0));
                break;
        }
    }

    /// <summary>Feeds one newly simulated frame. Call after every <see cref="MatchSimulation.Step"/>.</summary>
    public void OnSimulated(in MatchState state)
    {
        if (state.Frame == _lastFrame) return;
        _lastFrame = state.Frame;
        Audio?.OnEvents(state, _context);
        foreach (ref readonly var e in state.CurrentEvents)
        {
            EventRaised?.Invoke(e);
            var at = new Vector3(e.Position.X.ToFloat(), e.Position.Y.ToFloat(), 0.1f);
            switch (e.Kind)
            {
                case MatchEventKind.Hit or MatchEventKind.CounterHit:
                    bool heavy = e.Value > 60;
                    _vfx.HitSparks(at, e.Value);
                    if (e.Kind == MatchEventKind.CounterHit)
                    {
                        _vfx.CounterSparks(at);
                        _vfx.Shockwave(at, new Vector3(1f, 0.75f, 0.2f), 2.6f, 0.3f);
                    }
                    else if (heavy) _vfx.Shockwave(at, new Vector3(1f, 0.9f, 0.7f), 1.8f, 0.25f);
                    _camera.Shake(heavy || e.Kind == MatchEventKind.CounterHit ? 0.45f : 0.15f);
                    _physics?.Explode(at, 2.5f, e.Value / 60f);
                    break;
                case MatchEventKind.Block: _vfx.BlockSparks(at); break;
                case MatchEventKind.Parry:
                    _vfx.BlockSparks(at);
                    _vfx.BlockSparks(at + new Vector3(0, 0.2f, 0));
                    _vfx.Shockwave(at, new Vector3(0.4f, 0.75f, 1f), 2.2f, 0.3f);
                    _flash = 0.25f;
                    break;
                case MatchEventKind.Launch: _vfx.Shockwave(at, new Vector3(1f, 1f, 1f), 1.6f, 0.22f); break;
                case MatchEventKind.TagSwitch: _vfx.Shockwave(at + new Vector3(0, 1, 0), new Vector3(0.96f, 0.76f, 0.1f), 2.4f, 0.4f); break;
                case MatchEventKind.Land or MatchEventKind.Dodge: _vfx.Dust(at, 6); break;
                case MatchEventKind.Knockdown:
                    _vfx.Dust(at, 18);
                    _camera.Shake(0.3f);
                    _physics?.Explode(at, 3f, 1.5f);
                    break;
                case MatchEventKind.KO:
                    _vfx.HitSparks(at, 200);
                    _vfx.CounterSparks(at);
                    _vfx.Shockwave(at, new Vector3(1f, 0.3f, 0.2f), 4f, 0.5f);
                    _vfx.Shockwave(at, new Vector3(1f, 0.9f, 0.6f), 2.6f, 0.35f);
                    _camera.Punch(0.9f);
                    _camera.Shake(0.9f);
                    _flash = 0.5f;
                    _physics?.Explode(at, 5f, 4f);
                    break;
                case MatchEventKind.RageActivated:
                    _vfx.FinisherBurst(at + new Vector3(0, 1, 0), gold: false);
                    _vfx.Shockwave(at + new Vector3(0, 1, 0), new Vector3(1f, 0.2f, 0.1f), 5f, 0.5f);
                    _camera.Shake(0.4f);
                    _physics?.Explode(at, 4f, 3f);
                    break;
                case MatchEventKind.TeamCombo or MatchEventKind.Mercy: _vfx.Confetti(at + new Vector3(0, 1.5f, 0)); break;
                case MatchEventKind.FinisherStart:
                    var kind = _context.Finishers[e.Slot][e.Value].Kind;
                    if (kind == FinisherKind.Friendship) _vfx.Confetti(at + new Vector3(0, 1.5f, 0));
                    else _vfx.FinisherBurst(at + new Vector3(0, 1f, 0), gold: kind == FinisherKind.Legendary);
                    if (kind is FinisherKind.Fatality or FinisherKind.Brutality && _physics is not null)
                        _physics.SpawnDebris(at + new Vector3(0, 0.8f, 0), 14, _context.Characters[e.Target].Body.PrimaryColor, 0.18f, _random);
                    _camera.Shake(0.7f);
                    _camera.Punch(1.2f);
                    _vfx.Shockwave(at + new Vector3(0, 1, 0), kind == FinisherKind.Legendary ? new Vector3(1f, 0.85f, 0.3f) : new Vector3(1f, 0.15f, 0.1f), 6f, 0.7f);
                    _flash = 0.6f;
                    break;
                case MatchEventKind.HazardHit:
                    _camera.Shake(0.5f);
                    _vfx.Dust(at, 20);
                    break;
                case MatchEventKind.ArenaEvent:
                    var ev = _context.Stage.Events[e.Value].Kind;
                    if (ev is ArenaEventKind.Fireworks or ArenaEventKind.Procession) _vfx.Confetti(new Vector3(_random.Next(-6, 7), 4, -5));
                    if (ev == ArenaEventKind.BassDrop)
                    {
                        _camera.Shake(0.8f);
                        _physics?.Explode(new Vector3(0, 0, -6), 20f, 2.5f);
                    }
                    if (ev == ArenaEventKind.Blackout) _flash = -1.5f;
                    break;
            }
        }
    }

    /// <summary>Updates every visual for the latest state. Call once per rendered frame.</summary>
    public void Render(in MatchState state, float dt)
    {
        dt = MathF.Min(dt, 0.1f);
        _time += dt;

        float minX = float.MaxValue, maxX = float.MinValue, maxY = 0;
        int spots = 0;
        for (int i = 0; i < state.FighterCount; i++)
        {
            ref readonly var f = ref state.Fighters[i];
            _fighters[i].Apply(f, _context, dt, _time);
            if (!f.IsActive) continue;
            var p = f.Position.ToVector2();
            minX = MathF.Min(minX, p.X);
            maxX = MathF.Max(maxX, p.X);
            maxY = MathF.Max(maxY, p.Y);
            _fighterSpots[spots++] = (new Vector3(p.X, 0, 0), _context.Characters[i].Id);
            _vfx.SetRage(i, f.RageActive, new Vector3(p.X, p.Y, 0));
            if (f.AdrenalineActive && dt > 0) _vfx.Aura(new Vector3(p.X, p.Y, 0), dt);
        }
        if (spots == 0) { minX = -2; maxX = 2; }

        for (int i = 0; i < MatchState.MaxProjectiles; i++)
        {
            ref readonly var pr = ref state.Projectiles[i];
            var node = _projectiles[i];
            node.Visible = pr.Active;
            if (!pr.Active) continue;
            var pos = pr.Position.ToVector2();
            node.Position = new Vector3(pos.X, pos.Y, 0);
            if (dt > 0) _vfx.Trail(node.Position);
            float pulse = 0.35f + 0.06f * MathF.Sin(_time * 30f);
            node.Scale = new Vector3(pulse);
        }

        ref readonly var h = ref state.Hazard;
        _hazard.Visible = h.Active && h.Warning <= 0;
        _hazardWarning.Visible = h.Active && h.Warning > 0 && (h.Warning / 6) % 2 == 0;
        if (h.Active)
        {
            var hp = h.Position.ToVector2();
            var size = h.Size.ToVector2();
            _hazard.Position = new Vector3(hp.X, 0, 0);
            _hazard.Rotation = Quaternion.CreateFromYawPitchRoll(h.VelocityX.Raw < 0 ? MathF.PI : 0, 0, 0);
            _hazardWarning.Position = new Vector3(hp.X, 0.02f, 0);
            _hazardWarning.Scale = new Vector3(MathF.Max(size.X, 1.2f), 2.4f, 1);
            if (h.VelocityX.Raw == 0 && h.Warning <= 0) _hazard.Position = new Vector3(hp.X, MathF.Max(0, 6f - (40 - h.Life) * 0.6f), 0);
        }

        var mode = state.Phase switch
        {
            MatchPhase.Intro => CameraDirector.Mode.Intro,
            MatchPhase.FinisherPerforming => CameraDirector.Mode.Finisher,
            MatchPhase.MatchOver => CameraDirector.Mode.Victory,
            _ => CameraDirector.Mode.Fight,
        };
        var focus = Vector3.Zero;
        if (state.FinisherVictim >= 0) focus = new Vector3(state.Fighters[state.FinisherVictim].Position.X.ToFloat(), 0, 0);
        else if (state.MatchWinnerTeam >= 0)
            for (int i = 0; i < state.FighterCount; i++)
                if (state.Fighters[i].IsActive && state.Fighters[i].Team == state.MatchWinnerTeam) { focus = new Vector3(state.Fighters[i].Position.X.ToFloat(), 0, 0); break; }
        _camera.Update(dt, minX, maxX, maxY, mode, focus, _context.Stage.HalfWidth);

        float crowd01 = state.Crowd / (float)MatchSimulation.CrowdMax;
        _npcs.Update(dt, crowd01, _fighterSpots.AsSpan(0, spots), state.CurrentEvents);
        _crowd.Update(_npcs.Agents);

        if (_physics is not null)
        {
            _physics.Update(dt);
            _props!.Update(_physics);
        }

        // Stage animation: spinning effigies, sound-system lights pulsing on the beat, flickering lamps.
        foreach (var (node, speed) in _rig.Spinners) node.EulerAngles = new Vector3(0, _time * speed, 0);
        float beat = MathF.Pow(MathF.Max(0, MathF.Sin(_time * MathF.PI * 128f / 60f)), 8);
        for (int i = 0; i < _rig.Pulsers.Count; i++)
        {
            // Speaker cones thump outwards, LED strips flare, alternating so the wall looks alive.
            float s = 1f + beat * (i % 2 == 0 ? 0.18f : 0.5f);
            var b = _pulserScale[i];
            _rig.Pulsers[i].Scale = new Vector3(b.X * s, b.Y * (i % 2 == 0 ? 1f : s), b.Z * s);
        }

        // Impact flash and blackouts dim or boost the sun.
        if (_flash > 0) _flash = MathF.Max(0, _flash - dt);
        else if (_flash < 0) _flash = MathF.Min(0, _flash + dt);
        float sunIntensity = _rig.BaseSunIntensity * (1f + _flash * 1.6f) * (_flash < 0 ? 0.15f : 1f);
        var light = _rig.Sun.Light!.Value;
        light.Intensity = sunIntensity;
        _rig.Sun.Light = light;
        for (int i = 0; i < _rig.Lamps.Count; i++)
        {
            var lamp = _rig.Lamps[i].Light!.Value;
            lamp.Intensity = (_flash < 0 ? 0.3f : 1f) * (6f + MathF.Sin(_time * 7f + i * 1.7f) * 0.6f);
            _rig.Lamps[i].Light = lamp;
        }

        Audio?.SetCrowd(crowd01);
        _vfx.Update(dt, _camera.Camera.WorldMatrix);
        Scene.UpdateAnimations(dt);
        Audio?.Update(Scene, _camera.Camera, dt);
    }

    public void Dispose()
    {
        _physics?.Dispose();
        Scene.Dispose();
    }
}
