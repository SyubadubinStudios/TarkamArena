using System.Numerics;
using TarkamArena.Core.Content;
using ThreeNet;
using ThreeNet.Effects;

namespace TarkamArena.Engine.Rendering;

/// <summary>
/// Particle effects for the fight: hit sparks, block sparks, dust, rage auras, finisher bursts,
/// confetti and stage weather. Each effect is one preallocated CPU particle system re-uploaded per
/// frame, so spawning effects never allocates.
/// </summary>
public sealed class VfxSystem
{
    private sealed record Emitter(ParticleEffect Effect, Geometry Geometry, Node Node);

    private readonly Scene _scene;
    private readonly Emitter _sparks;
    private readonly Emitter _blockSparks;
    private readonly Emitter _dust;
    private readonly Emitter _burst;
    private readonly Emitter _goldBurst;
    private readonly Emitter _confetti;
    private readonly Emitter? _weather;
    private readonly List<Emitter> _all = [];
    private readonly FireEffect[] _rageFires;
    private readonly Emitter _counterSparks;
    private readonly Emitter _aura;
    private readonly Emitter _trail;
    private readonly Ring[] _rings = new Ring[6];
    private int _nextRing;
    private double _time;

    /// <summary>An expanding, fading shockwave ring facing the camera.</summary>
    private struct Ring
    {
        public Node Node;
        public Material Material;
        public Vector3 Color;
        public float Age, Duration, MaxScale;
        public bool Active;
    }

    public VfxSystem(Scene scene, StageDefinition stage, int fighters, bool weather, bool lowQuality)
    {
        _scene = scene;
        var glow = scene.CreateShader(EffectShaders.Particle);
        var smoke = scene.CreateShader(EffectShaders.Smoke);
        int scale = lowQuality ? 2 : 1;

        _sparks = Add(new ParticleEffect(400 / scale, 11)
        {
            Rate = 0, Lifetime = 0.35f, Variation = 0.5f, Size = 0.09f, Growth = 0.2f, Shape = EmitterShape.Sphere, EmitterRadius = 0.08f,
            Radial = true, Spread = 6f, Gravity = new Vector3(0, -9, 0), Drag = 0.5f, StretchBySpeed = 0.04f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.95f, 0.6f), new Vector3(1f, 0.3f, 0.05f), 4f));

        _blockSparks = Add(new ParticleEffect(200 / scale, 12)
        {
            Rate = 0, Lifetime = 0.25f, Variation = 0.4f, Size = 0.07f, Growth = 0.1f, Shape = EmitterShape.Sphere, EmitterRadius = 0.05f,
            Radial = true, Spread = 4f, Drag = 0.7f, StretchBySpeed = 0.03f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(0.6f, 0.85f, 1f), new Vector3(0.2f, 0.4f, 1f), 4f));

        _dust = Add(new ParticleEffect(260 / scale, 13)
        {
            Rate = 0, Lifetime = 0.7f, Variation = 0.5f, Size = 0.14f, Growth = 2.2f, Shape = EmitterShape.Disc, EmitterRadius = 0.35f,
            Rise = 0.5f, Spread = 1.6f, Drag = 0.8f, Gravity = new Vector3(0, 0.3f, 0),
        }, ParticleEffect.SmokeMaterial(smoke, Palette.Hex3(Palette.Lighten(stage.Palette.Ground, 0.4f)), 0.16f));

        _burst = Add(new ParticleEffect(700 / scale, 14)
        {
            Rate = 0, Lifetime = 1.2f, Variation = 0.6f, Size = 0.16f, Growth = 0.3f, Shape = EmitterShape.Sphere, EmitterRadius = 0.3f,
            Radial = true, Spread = 7f, Gravity = new Vector3(0, -4, 0), Drag = 0.4f, Turbulence = 2f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.25f, 0.1f), new Vector3(0.4f, 0f, 0.3f), 5f));

        _goldBurst = Add(new ParticleEffect(700 / scale, 19)
        {
            Rate = 0, Lifetime = 1.6f, Variation = 0.6f, Size = 0.18f, Growth = 0.4f, Shape = EmitterShape.Sphere, EmitterRadius = 0.4f,
            Radial = true, Spread = 6f, Gravity = new Vector3(0, 1.5f, 0), Drag = 0.5f, Turbulence = 2.5f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.85f, 0.3f), new Vector3(0.2f, 0.8f, 1f), 6f));

        _counterSparks = Add(new ParticleEffect(300 / scale, 21)
        {
            Rate = 0, Lifetime = 0.5f, Variation = 0.5f, Size = 0.14f, Growth = 0.1f, Shape = EmitterShape.Sphere, EmitterRadius = 0.1f,
            Radial = true, Spread = 9f, Gravity = new Vector3(0, -6, 0), Drag = 0.6f, StretchBySpeed = 0.07f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.85f, 0.2f), new Vector3(1f, 0.15f, 0.05f), 6f));

        _aura = Add(new ParticleEffect(260 / scale, 22)
        {
            Rate = 0, Lifetime = 0.8f, Variation = 0.5f, Size = 0.07f, Growth = 0.2f, Shape = EmitterShape.Box, EmitterSize = new Vector3(0.35f, 0.8f, 0.25f),
            Rise = 1.4f, Spread = 0.3f, Gravity = new Vector3(0, 1.2f, 0), Turbulence = 1.5f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(0.5f, 1f, 0.55f), new Vector3(0.1f, 0.6f, 1f), 3f));

        _trail = Add(new ParticleEffect(300 / scale, 23)
        {
            Rate = 0, Lifetime = 0.35f, Variation = 0.4f, Size = 0.16f, Growth = 0.05f, Shape = EmitterShape.Sphere, EmitterRadius = 0.06f,
            Spread = 0.2f, Drag = 0.9f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.9f, 0.4f), new Vector3(1f, 0.25f, 0.1f), 4f));

        var torus = scene.CreateTorusGeometry(0.5f, 0.035f, 8, 48);
        for (int i = 0; i < _rings.Length; i++)
        {
            var options = MaterialOptions.Basic(new Vector4(1, 1, 1, 0));
            options.AlphaMode = AlphaMode.Additive;
            options.DepthWrite = false;
            options.CullMode = CullMode.None;
            var material = scene.CreateMaterial(options);
            var node = scene.AddMesh(torus, material, name: "shockwave");
            node.CastShadow = false;
            node.ReceiveShadow = false;
            node.Visible = false;
            _rings[i] = new Ring { Node = node, Material = material };
        }

        _confetti = Add(new ParticleEffect(500 / scale, 15)
        {
            Rate = 0, Lifetime = 2.6f, Variation = 0.5f, Size = 0.09f, Growth = 1f, Shape = EmitterShape.Box, EmitterSize = new Vector3(3, 0.2f, 1),
            Rise = 5f, Spread = 2f, Gravity = new Vector3(0, -2.5f, 0), Drag = 0.6f, Turbulence = 3f, SpinRate = 8f,
        }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.85f, 0.1f), new Vector3(0.1f, 0.9f, 0.4f), 2.5f));

        if (weather)
        {
            var w = stage.Weather;
            if (w is Weather.Rain or Weather.Storm)
            {
                _weather = Add(new ParticleEffect(1400 / scale, 16)
                {
                    Rate = 900 / scale, Lifetime = 1.0f, Size = 0.03f, Growth = 1f, Shape = EmitterShape.Box, EmitterSize = new Vector3(16, 0.1f, 6),
                    Origin = new Vector3(0, 9, -1), Direction = new Vector3(0, -1, 0), Rise = 11f, Gravity = new Vector3(0, -12, 0), StretchBySpeed = 0.06f,
                    Wind = w == Weather.Storm ? new Vector3(-4, 0, 0) : Vector3.Zero, WindInfluence = 0.5f,
                }, ParticleEffect.GlowMaterial(glow, new Vector3(0.6f, 0.7f, 0.9f), new Vector3(0.4f, 0.5f, 0.7f), 0.6f));
            }
            else if (w is Weather.Ash || stage.Id == "hutan-gambut-riau")
            {
                _weather = Add(new ParticleEffect(600 / scale, 17)
                {
                    Rate = 120 / scale, Lifetime = 5f, Size = 0.05f, Growth = 1f, Shape = EmitterShape.Box, EmitterSize = new Vector3(16, 0.1f, 6),
                    Origin = new Vector3(0, 7, -1), Direction = new Vector3(0, -1, 0), Rise = 0.6f, Gravity = new Vector3(0, -0.4f, 0), Turbulence = 1.5f,
                }, ParticleEffect.GlowMaterial(glow, new Vector3(1f, 0.55f, 0.2f), new Vector3(0.3f, 0.3f, 0.3f), 1.5f));
            }
            else if (w is Weather.Fog)
            {
                _weather = Add(new ParticleEffect(120, 18)
                {
                    Rate = 12, Lifetime = 9f, Size = 2.5f, Growth = 1.6f, Shape = EmitterShape.Box, EmitterSize = new Vector3(14, 0.5f, 4),
                    Origin = new Vector3(0, 0.6f, -4), Rise = 0.05f, Spread = 0.25f, Turbulence = 0.3f,
                }, ParticleEffect.SmokeMaterial(smoke, Palette.Hex3(stage.Palette.Fog), 0.07f));
            }
        }

        _rageFires = new FireEffect[fighters];
        for (int i = 0; i < fighters; i++)
        {
            _rageFires[i] = new FireEffect(scene, Vector3.Zero, new Vector3(1.1f, 2.4f, 1.1f), seed: 31 + i) { Style = FireStyle.Magic };
            _rageFires[i].Node.Visible = false;
        }
    }

    private Emitter Add(ParticleEffect effect, MaterialOptions material)
    {
        var geometry = effect.CreateGeometry(_scene);
        var node = _scene.AddMesh(geometry, _scene.CreateMaterial(material), name: "vfx");
        node.CastShadow = false;
        node.ReceiveShadow = false;
        var e = new Emitter(effect, geometry, node);
        _all.Add(e);
        return e;
    }

    public void HitSparks(Vector3 at, int strength)
    {
        _sparks.Effect.Origin = at;
        _sparks.Effect.Burst(Math.Clamp(12 + strength / 3, 12, 70));
    }

    public void BlockSparks(Vector3 at)
    {
        _blockSparks.Effect.Origin = at;
        _blockSparks.Effect.Burst(18);
    }

    public void Dust(Vector3 at, int amount = 10)
    {
        _dust.Effect.Origin = new Vector3(at.X, 0.05f, at.Z);
        _dust.Effect.Burst(amount);
    }

    /// <summary>Big burst for finishers: crimson for fatalities and brutalities, gold for the legendary finisher.</summary>
    public void FinisherBurst(Vector3 at, bool gold)
    {
        var e = gold ? _goldBurst : _burst;
        e.Effect.Origin = at;
        e.Effect.Burst(260);
    }

    /// <summary>Gold burst for counter hits.</summary>
    public void CounterSparks(Vector3 at)
    {
        _counterSparks.Effect.Origin = at;
        _counterSparks.Effect.Burst(60);
    }

    /// <summary>Expanding shockwave ring (heavy hits, parries, KOs, rage bursts).</summary>
    public void Shockwave(Vector3 at, Vector3 color, float maxScale = 2.4f, float duration = 0.35f)
    {
        ref var r = ref _rings[_nextRing];
        _nextRing = (_nextRing + 1) % _rings.Length;
        r.Active = true;
        r.Age = 0;
        r.Duration = duration;
        r.MaxScale = maxScale;
        r.Color = color;
        r.Node.Position = at;
        r.Node.Visible = true;
    }

    /// <summary>Adrenaline aura: green sparks rising off the fighter while the mode lasts.</summary>
    public void Aura(Vector3 position, float dt)
    {
        _aura.Effect.Origin = position + new Vector3(0, 0.9f, 0);
        _aura.Effect.Burst(Math.Max(1, (int)(dt * 90)));
    }

    /// <summary>Glowing trail behind a projectile.</summary>
    public void Trail(Vector3 position)
    {
        _trail.Effect.Origin = position;
        _trail.Effect.Burst(2);
    }

    public void Confetti(Vector3 at)
    {
        _confetti.Effect.Origin = at;
        _confetti.Effect.Burst(180);
    }

    public void SetRage(int slot, bool active, Vector3 position)
    {
        if (slot >= _rageFires.Length) return;
        var fire = _rageFires[slot];
        fire.Node.Visible = active;
        if (active) fire.Node.Position = position + new Vector3(0, 1.1f, 0);
    }

    public void Update(float dt, Matrix4x4 cameraWorld)
    {
        for (int i = 0; i < _rings.Length; i++)
        {
            ref var r = ref _rings[i];
            if (!r.Active) continue;
            r.Age += dt;
            float t = MathF.Min(1, r.Age / r.Duration);
            float ease = 1 - (1 - t) * (1 - t) * (1 - t);
            float scale = 0.25f + (r.MaxScale - 0.25f) * ease;
            r.Node.Scale = new Vector3(scale, scale, scale);
            r.Material.BaseColor = new Vector4(r.Color * (1 - t) * 2.2f, 1 - t);
            if (t >= 1)
            {
                r.Active = false;
                r.Node.Visible = false;
            }
        }
        _time += dt;
        foreach (var e in _all)
        {
            e.Effect.Update(dt, _time);
            e.Effect.Upload(e.Geometry, cameraWorld);
        }
        foreach (var f in _rageFires) f.Update(dt);
    }
}

/// <summary>Side-on fighting game camera: frames every active fighter, shakes on impact and orbits during finishers.</summary>
public sealed class CameraDirector
{
    private Vector3 _position = new(0, 2.2f, 10);
    private Vector3 _target = new(0, 1.2f, 0);
    private float _shake;
    private float _orbit;
    private readonly Random _random = new(5);

    public Node Camera { get; }

    public CameraDirector(Scene scene)
    {
        Camera = scene.AddCamera(ThreeNet.Camera.Perspective(38f * MathF.PI / 180f, 0.1f, 400f), _position);
        Camera.LookAt(_target);
    }

    public void Shake(float amount) => _shake = MathF.Max(_shake, amount);

    private float _punch;

    /// <summary>A quick push-in towards the action (KO, finisher start).</summary>
    public void Punch(float amount) => _punch = MathF.Max(_punch, amount);

    public enum Mode
    {
        Fight,
        Intro,
        Finisher,
        Victory,
    }

    /// <summary>Frames the span between <paramref name="minX"/> and <paramref name="maxX"/>.</summary>
    public void Update(float dt, float minX, float maxX, float maxY, Mode mode, Vector3 focus, float halfWidth)
    {
        float center = (minX + maxX) / 2;
        float span = maxX - minX;
        Vector3 wantTarget, wantPos;
        switch (mode)
        {
            case Mode.Intro:
                _orbit += dt * 0.35f;
                wantTarget = new Vector3(0, 1.3f, 0);
                wantPos = new Vector3(MathF.Sin(_orbit - 0.6f) * 9f, 2.4f, MathF.Cos(_orbit - 0.6f) * 9f + 1);
                break;
            case Mode.Finisher:
                _orbit += dt * 0.5f;
                wantTarget = focus + new Vector3(0, 1.1f, 0);
                wantPos = focus + new Vector3(MathF.Sin(_orbit) * 4.2f, 1.9f, MathF.Cos(_orbit) * 4.2f + 0.5f);
                break;
            case Mode.Victory:
                wantTarget = focus + new Vector3(0, 1.2f, 0);
                wantPos = focus + new Vector3(1.2f, 1.6f, 4.8f);
                break;
            default:
                _orbit = 0;
                float distance = Math.Clamp(5.5f + span * 0.62f, 6.5f, 14f);
                float limit = MathF.Max(0, halfWidth - distance * 0.35f);
                float cx = Math.Clamp(center, -limit, limit);
                wantTarget = new Vector3(cx, 1.15f + maxY * 0.45f, 0);
                wantPos = new Vector3(cx, 1.9f + maxY * 0.35f + distance * 0.06f, distance);
                break;
        }

        float k = 1f - MathF.Exp(-dt * (mode == Mode.Fight ? 6f : 2.5f));
        _target = Vector3.Lerp(_target, wantTarget, k);
        _position = Vector3.Lerp(_position, wantPos, k);

        var shake = Vector3.Zero;
        if (_shake > 0.001f)
        {
            shake = new Vector3((float)_random.NextDouble() - 0.5f, (float)_random.NextDouble() - 0.5f, 0) * _shake * 0.35f;
            _shake *= MathF.Exp(-dt * 9f);
        }
        var punchOffset = Vector3.Zero;
        if (_punch > 0.001f)
        {
            punchOffset = Vector3.Normalize(_target - _position) * _punch * 2.2f;
            _punch *= MathF.Exp(-dt * 5f);
        }
        Camera.Position = _position + shake + punchOffset;
        Camera.LookAt(_target + shake * 0.5f);
    }
}
