using System.Numerics;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Match;
using ThreeNet;
using ThreeNet.Effects;

namespace TarkamArena.Engine.Rendering;

/// <summary>
/// Draws signature moves ("Jurus Pamungkas"). Every fighter's three signatures get a prebuilt rig
/// (helpers, a charging crowd, a giant weapon, a storm cloud, lift rings, a whirlwind, quake shards
/// or a wave wall) plus a themed particle emitter, so playing one never allocates. The rigs are
/// posed from <see cref="MatchState.Summons"/> every rendered frame.
/// </summary>
public sealed class SignatureRenderer
{
    private const int Slots = 3;

    private sealed class Rig
    {
        public required SignatureMove Move;
        public required Node Root;
        public required Node Body;
        public readonly List<Part> Parts = [];
        public required ParticleEffect Fx;
        public required Geometry FxGeometry;
        public Node? Bolt;
        public Node? Warning;
        public bool UsedThisFrame;
    }

    /// <summary>A child node with a simple procedural animation.</summary>
    private readonly record struct Part(Node Node, PartAnim Anim, Vector3 Position, Vector3 Euler, float Phase);

    private enum PartAnim : byte
    {
        None,
        Bob,
        Flap,
        Spin,
        Orbit,
        Shard,
        Wobble,
        Swirl,
    }

    private readonly MeshCache _cache;
    private readonly Rig[] _rigs;
    private readonly Node[][] _missiles;
    private readonly Scene _scene;
    private float _time;

    public SignatureRenderer(Scene scene, MeshCache cache, MatchContext context, bool lowQuality)
    {
        _scene = scene;
        _cache = cache;
        int fighters = context.Characters.Length;
        _rigs = new Rig[fighters * Slots];
        _missiles = new Node[fighters * Slots][];
        var glow = scene.CreateShader(EffectShaders.Particle);
        var smoke = scene.CreateShader(EffectShaders.Smoke);
        for (int f = 0; f < fighters; f++)
        for (int s = 0; s < Slots; s++)
        {
            var move = context.Signatures[f][s];
            _rigs[f * Slots + s] = BuildRig(move, glow, smoke, lowQuality ? 2 : 1, 100 + f * 7 + s);
            if (move.Kind == SignatureKind.Barrage)
            {
                var pool = new Node[MatchState.MaxProjectiles];
                for (int i = 0; i < pool.Length; i++)
                {
                    pool[i] = scene.CreateNode(name: "signature-missile");
                    BuildProp(move.Theme.Prop, move.Theme, pool[i], 0.55f, null);
                    pool[i].Visible = false;
                }
                _missiles[f * Slots + s] = pool;
            }
        }
    }

    /// <summary>True when projectile <paramref name="index"/> is drawn here instead of the generic orb.</summary>
    public bool DrawsProjectile(in Projectile p) => p.Signature > 0 && _missiles[p.Owner * Slots + p.Signature - 1] is not null;

    public SignatureMove MoveOf(int owner, int slot) => _rigs[owner * Slots + slot].Move;

    public void Update(in MatchState state, float dt, Matrix4x4 cameraWorld, VfxSystem vfx)
    {
        _time += dt;
        foreach (var rig in _rigs)
        {
            rig.UsedThisFrame = false;
            rig.Root.Visible = false;
        }

        for (int i = 0; i < MatchState.MaxSummons; i++)
        {
            ref readonly var s = ref state.Summons[i];
            if (!s.Active || s.Owner < 0) continue;
            var rig = _rigs[s.Owner * Slots + s.Slot];
            if (rig.UsedThisFrame) continue;
            rig.UsedThisFrame = true;
            Pose(rig, state, s, dt);
        }

        // Barrage missiles take the shape of the thrown prop and tumble.
        foreach (var pool in _missiles)
            if (pool is not null)
                foreach (var n in pool) n.Visible = false;
        for (int i = 0; i < MatchState.MaxProjectiles; i++)
        {
            ref readonly var p = ref state.Projectiles[i];
            if (!p.Active || !DrawsProjectile(p)) continue;
            var rig = _rigs[p.Owner * Slots + p.Signature - 1];
            var node = _missiles[p.Owner * Slots + p.Signature - 1][i];
            var at = p.Position.ToVector2();
            node.Visible = true;
            node.Position = new Vector3(at.X, at.Y, 0);
            node.EulerAngles = new Vector3(_time * 9f, _time * 5f, -_time * 14f * MathF.Sign(p.VelocityX.ToFloat()));
            if (dt > 0)
            {
                rig.Fx.Origin = node.Position;
                rig.Fx.Burst(2);
            }
        }

        foreach (var rig in _rigs)
        {
            rig.Fx.Update(dt, _time);
            rig.Fx.Upload(rig.FxGeometry, cameraWorld);
        }
    }

    private void Pose(Rig rig, in MatchState state, in Summon s, float dt)
    {
        var pos = s.Position.ToVector2();
        var at = new Vector3(pos.X, pos.Y, 0);
        float age = s.Age;
        float facing = s.Facing >= 0 ? 1 : -1;
        float fadeOut = Math.Clamp(s.Life / 10f, 0, 1);
        float fadeIn = Math.Clamp(age / 6f, 0, 1);
        float scale = MathF.Min(fadeIn, fadeOut);
        rig.Root.Visible = true;
        rig.Root.Position = at;
        rig.Root.Rotation = Quaternion.CreateFromYawPitchRoll(facing > 0 ? 0 : MathF.PI, 0, 0);
        rig.Body.Scale = new Vector3(MathF.Max(0.01f, scale));
        rig.Body.Position = Vector3.Zero;
        rig.Body.EulerAngles = Vector3.Zero;
        int burst = dt > 0 ? Math.Max(1, (int)(dt * 70)) : 0;
        var emit = at + new Vector3(0, 0.8f, 0);

        switch (s.Kind)
        {
            case SignatureKind.Summon:
                // Helpers bank into their dive and bob between strikes.
                float dive = Math.Clamp(s.Velocity.Y.ToFloat() * 3f, -0.6f, 0.6f);
                rig.Body.EulerAngles = new Vector3(0, 0, dive);
                rig.Root.Position = at + new Vector3(0, 0.45f, 0.7f);
                emit = rig.Root.Position;
                break;
            case SignatureKind.Crowd:
                emit = at + new Vector3(-0.6f * facing, 0.2f, 0);
                break;
            case SignatureKind.Weapon:
            {
                // The pivot sits at the fighter's hand; the arc goes from overhead to the floor.
                float t = Math.Clamp((age - 1) / 11f, 0, 1);
                float ease = 1 - (1 - t) * (1 - t) * (1 - t);
                rig.Root.Position = new Vector3(pos.X - 1.4f * facing, pos.Y + 1.15f, 0);
                if (rig.Move.Theme.Prop is SignatureProp.Cart)
                {
                    // Carts are rammed, not swung: shoved forward with a hop and a tilt.
                    rig.Root.Position = new Vector3(pos.X - 1.4f * facing, pos.Y, 0);
                    rig.Body.Position = new Vector3(ease * 2.4f, MathF.Sin(t * MathF.PI) * 0.5f, 0);
                    rig.Body.EulerAngles = new Vector3(0, 0, -MathF.Sin(t * MathF.PI) * 0.35f);
                }
                else rig.Body.EulerAngles = new Vector3(0, 0, 0.6f - ease * 2.4f); // ends with the head on the floor
                emit = rig.Root.Position + new Vector3(facing * 1.6f * ease, 1.2f - ease * 2f, 0);
                if (dt > 0 && age is >= 4 and <= 14) burst *= 4;
                break;
            }
            case SignatureKind.Storm:
            {
                rig.Root.Position = new Vector3(pos.X, 0, 0);
                rig.Body.Position = new Vector3(0, 0, 0);
                float sinceStrike = 99, untilStrike = 99;
                foreach (int k in s.Slot == 2 ? s_stormFrames4 : s_stormFrames3)
                {
                    if (age >= k) sinceStrike = MathF.Min(sinceStrike, age - k);
                    else untilStrike = MathF.Min(untilStrike, k - age);
                }
                if (rig.Warning is { } w)
                {
                    w.Visible = untilStrike < 30;
                    float blink = 0.6f + 0.4f * MathF.Sin(_time * 30f);
                    w.Scale = new Vector3(1.6f * blink, 0.02f, 1.6f * blink);
                }
                if (rig.Bolt is { } bolt)
                {
                    bool drop = rig.Move.Theme.Prop is not SignatureProp.Bolt;
                    if (drop)
                    {
                        // Falling props (fireballs, water bombs, gavels, envelopes) drop over the last 10 frames.
                        bolt.Visible = untilStrike <= 10 || sinceStrike < 3;
                        float y = untilStrike <= 10 ? untilStrike / 10f * 6f : 0;
                        bolt.Position = new Vector3(0, y + 0.3f, 0);
                        bolt.EulerAngles = new Vector3(_time * 6, _time * 4, 0);
                    }
                    else
                    {
                        bolt.Visible = sinceStrike < 6 && ((int)sinceStrike % 2 == 0 || sinceStrike < 2);
                        bolt.Scale = new Vector3(1 + (6 - sinceStrike) * 0.15f, 1, 1);
                    }
                }
                emit = new Vector3(pos.X, sinceStrike < 4 ? 0.3f : 6.2f, 0);
                if (sinceStrike < 4) burst *= 6;
                break;
            }
            case SignatureKind.Levitate:
                rig.Root.Rotation = Quaternion.Identity;
                rig.Body.Position = new Vector3(0, 0.9f, 0);
                emit = at + new Vector3(0, 0.9f, 0);
                break;
            case SignatureKind.Tornado:
                emit = at + new Vector3(0, (float)(_random.NextDouble() * 3), 0);
                burst *= 2;
                break;
            case SignatureKind.Barrage:
                // A glowing seal at the thrower's hand while the volley lasts.
                rig.Root.Position = new Vector3(pos.X + 0.6f * facing, pos.Y + 1.2f, 0);
                emit = rig.Root.Position;
                break;
            case SignatureKind.Quake:
            {
                float radius = 0.18f * age;
                rig.Root.Rotation = Quaternion.Identity;
                foreach (var part in rig.Parts)
                {
                    if (part.Anim != PartAnim.Shard) continue;
                    // Shards erupt at the moving front on either side, then sink back.
                    float side = part.Position.X >= 0 ? 1 : -1;
                    float lag = part.Phase;
                    float r = MathF.Max(0, radius - lag);
                    float rise = MathF.Max(0, MathF.Sin(MathF.Min(1, (radius - lag) / 1.4f) * MathF.PI));
                    part.Node.Position = new Vector3(side * r, -0.4f + rise * 0.9f, part.Position.Z);
                    part.Node.Visible = radius > lag;
                }
                if (rig.Warning is { } ring)
                {
                    ring.Scale = new Vector3(radius * 2 + 0.1f, radius * 2 + 0.1f, 1);
                    ring.Visible = true;
                }
                emit = new Vector3(pos.X + (_random.Next(2) == 0 ? radius : -radius), 0.1f, 0);
                burst *= 4;
                break;
            }
            case SignatureKind.Wave:
                rig.Body.Scale = new Vector3(scale, scale * (0.8f + 0.2f * MathF.Sin(_time * 8f)), scale);
                emit = at + new Vector3(0.6f * facing, 2.2f, 0);
                burst *= 3;
                break;
        }

        foreach (var part in rig.Parts) Animate(part);
        if (burst > 0)
        {
            rig.Fx.Origin = emit;
            rig.Fx.Burst(burst);
        }
    }

    private static readonly int[] s_stormFrames3 = [34, 56, 78];
    private static readonly int[] s_stormFrames4 = [34, 56, 78, 100];
    private readonly Random _random = new(77);

    private void Animate(in Part p)
    {
        float t = _time + p.Phase;
        switch (p.Anim)
        {
            case PartAnim.Bob:
                p.Node.Position = p.Position + new Vector3(0, MathF.Abs(MathF.Sin(t * 11f)) * 0.18f, 0);
                p.Node.EulerAngles = p.Euler + new Vector3(0, 0, MathF.Sin(t * 11f) * 0.12f);
                break;
            case PartAnim.Flap:
                p.Node.EulerAngles = p.Euler + new Vector3(MathF.Sin(t * 18f) * 0.8f, 0, 0);
                break;
            case PartAnim.Spin:
                p.Node.EulerAngles = p.Euler + new Vector3(0, t * 9f, 0);
                break;
            case PartAnim.Orbit:
                float a = t * 5f;
                float r = p.Position.X;
                p.Node.Position = new Vector3(MathF.Cos(a) * r, p.Position.Y + MathF.Sin(t * 3f) * 0.2f, MathF.Sin(a) * r);
                p.Node.EulerAngles = p.Euler + new Vector3(t * 4f, t * 6f, 0);
                break;
            case PartAnim.Swirl:
            {
                float h = p.Position.Y / 3f;
                p.Node.Position = p.Position + new Vector3(MathF.Cos(t * 7f) * 0.22f * h, 0, MathF.Sin(t * 7f) * 0.22f * h);
                break;
            }
            case PartAnim.Wobble:
                p.Node.Position = p.Position + new Vector3(0, MathF.Sin(t * 4f) * 0.12f, 0);
                p.Node.EulerAngles = p.Euler + new Vector3(0, MathF.Sin(t * 2f) * 0.3f, MathF.Sin(t * 3f) * 0.08f);
                break;
        }
    }

    // ---------------------------------------------------------------- building

    private Rig BuildRig(SignatureMove move, Shader glow, Shader smoke, int quality, int seed)
    {
        var theme = move.Theme;
        var root = _scene.CreateNode(name: "signature");
        var body = _scene.CreateNode(root, name: "signature-body");
        var (fx, geometry) = BuildEmitter(theme, glow, smoke, quality, seed);
        var rig = new Rig { Move = move, Root = root, Body = body, Fx = fx, FxGeometry = geometry };
        root.Visible = false;

        switch (move.Kind)
        {
            case SignatureKind.Summon:
            {
                var lead = _scene.CreateNode(body);
                BuildProp(theme.Prop, theme, lead, 1.5f, rig);
                rig.Parts.Add(new Part(lead, PartAnim.Wobble, Vector3.Zero, Vector3.Zero, 0));
                int companions = move.Slot == 2 ? 3 : move.Slot == 1 ? 2 : 0;
                for (int i = 0; i < companions; i++)
                {
                    var c = _scene.CreateNode(body);
                    BuildProp(theme.Prop, theme, c, 0.5f, rig);
                    rig.Parts.Add(new Part(c, PartAnim.Orbit, new Vector3(0.9f, 0.1f * i, 0), Vector3.Zero, i * 2.1f));
                }
                break;
            }
            case SignatureKind.Crowd:
            {
                bool vehicle = theme.Prop is SignatureProp.Truck;
                int count = vehicle ? 1 : theme.Prop is SignatureProp.Bike or SignatureProp.Cart ? 3 : move.Slot == 2 ? 7 : 5;
                for (int i = 0; i < count; i++)
                {
                    var n = _scene.CreateNode(body);
                    float x = vehicle ? 0 : -(i % 3) * 0.55f + 0.3f;
                    float z = vehicle ? 0 : (i - count / 2f) * 0.45f;
                    var p = new Vector3(x, 0, z);
                    n.Position = p;
                    BuildProp(theme.Prop, theme, n, vehicle ? 1.4f : 1f, rig, variant: i);
                    rig.Parts.Add(new Part(n, vehicle ? PartAnim.None : PartAnim.Bob, p, Vector3.Zero, i * 0.37f));
                }
                break;
            }
            case SignatureKind.Weapon:
            {
                var n = _scene.CreateNode(body);
                BuildProp(theme.Prop, theme, n, theme.Prop is SignatureProp.Cart ? 1.3f : 2.2f, rig);
                break;
            }
            case SignatureKind.Storm:
            {
                // A low, dark cloud over the target with a glowing core.
                var cloud = _scene.CreateNode(body);
                cloud.Position = new Vector3(0, 6.6f, -0.3f);
                var dark = _cache.Pbr(Palette.Mix(0x2A2A3A, theme.Primary, 0.25f), 0.95f);
                for (int i = 0; i < 6; i++)
                {
                    float x = (i - 2.5f) * 0.6f;
                    _cache.Add(_cache.LowSphere, dark, new Vector3(x, MathF.Sin(i * 1.7f) * 0.2f, MathF.Cos(i * 2.3f) * 0.3f), new Vector3(1.4f, 0.7f, 1.1f), cloud);
                }
                _cache.Add(_cache.LowSphere, _cache.Pbr(theme.Primary, 0.5f, 0, 3f), new Vector3(0, -0.25f, 0), new Vector3(1.4f, 0.35f, 0.8f), cloud);
                rig.Parts.Add(new Part(cloud, PartAnim.Wobble, cloud.Position, Vector3.Zero, 0));

                rig.Warning = _cache.Add(_cache.Cylinder, _cache.Unlit(theme.Primary, 0.45f), new Vector3(0, 0.03f, 0), new Vector3(1.6f, 0.02f, 1.6f), body);
                var bolt = _scene.CreateNode(body);
                if (theme.Prop is SignatureProp.Bolt)
                {
                    // A jagged bolt from the cloud to the ground.
                    var mat = _cache.Pbr(theme.Primary, 0.2f, 0, 9f);
                    float y = 6.4f, x = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        float nx = i == 5 ? 0 : MathF.Sin(i * 2.9f) * 0.35f;
                        float ny = y - 1.07f;
                        var mid = new Vector3((x + nx) / 2, (y + ny) / 2, 0);
                        float len = MathF.Sqrt((nx - x) * (nx - x) + 1.07f * 1.07f);
                        _cache.Add(_cache.Box, mat, mid, new Vector3(0.14f, len, 0.14f), bolt, new Vector3(0, 0, MathF.Atan2(x - nx, 1.07f)));
                        x = nx;
                        y = ny;
                    }
                    _cache.Add(_cache.Box, _cache.Pbr(theme.Secondary, 0.2f, 0, 6f), new Vector3(0, 3.2f, 0), new Vector3(0.05f, 6.4f, 0.05f), bolt);
                }
                else BuildProp(theme.Prop, theme, bolt, 1.1f, rig);
                bolt.Visible = false;
                rig.Bolt = bolt;
                break;
            }
            case SignatureKind.Levitate:
            {
                // Spinning rings around the victim, and the theme prop circling them.
                for (int i = 0; i < 3; i++)
                {
                    var ring = _cache.Add(_cache.Torus, _cache.Pbr(i == 1 ? theme.Secondary : theme.Primary, 0.3f, 0, 5f), new Vector3(0, -0.6f + i * 0.6f, 0), new Vector3(1.6f - i * 0.25f, 1.6f - i * 0.25f, 0.6f), body, new Vector3(MathF.PI / 2, 0, 0));
                    rig.Parts.Add(new Part(ring, PartAnim.Spin, ring.Position, ring.EulerAngles, i * 1.3f));
                }
                for (int i = 0; i < 3; i++)
                {
                    var n = _scene.CreateNode(body);
                    BuildProp(theme.Prop, theme, n, 0.45f, rig);
                    rig.Parts.Add(new Part(n, PartAnim.Orbit, new Vector3(1.2f, -0.2f + i * 0.4f, 0), Vector3.Zero, i * 2.09f));
                }
                break;
            }
            case SignatureKind.Tornado:
            {
                // A twisting funnel: thin wind bands that widen with height and swirl out of phase.
                var mat = _cache.Unlit(theme.Primary, 0.32f);
                var core = _cache.Unlit(Palette.Lighten(theme.Secondary, 0.4f), 0.22f);
                for (int i = 0; i < 11; i++)
                {
                    float r = 0.3f + i * 0.13f;
                    var ring = _cache.Add(_cache.Torus, i % 2 == 0 ? mat : core, new Vector3(0, 0.15f + i * 0.31f, 0), new Vector3(r * 2, r * 2, 0.4f), body, new Vector3(MathF.PI / 2, 0, 0));
                    rig.Parts.Add(new Part(ring, PartAnim.Swirl, ring.Position, ring.EulerAngles, i * 0.55f));
                }
                for (int i = 0; i < 5; i++)
                {
                    var n = _scene.CreateNode(body);
                    BuildProp(theme.Prop is SignatureProp.Water or SignatureProp.Bolt ? SignatureProp.Leaf : theme.Prop, theme, n, 0.32f, rig);
                    rig.Parts.Add(new Part(n, PartAnim.Orbit, new Vector3(0.7f + i * 0.18f, 0.5f + i * 0.55f, 0), Vector3.Zero, i * 1.25f));
                }
                break;
            }
            case SignatureKind.Barrage:
            {
                var seal = _cache.Add(_cache.Torus, _cache.Pbr(theme.Primary, 0.3f, 0, 6f), Vector3.Zero, new Vector3(0.7f, 0.7f, 0.4f), body, new Vector3(0, MathF.PI / 2, 0));
                rig.Parts.Add(new Part(seal, PartAnim.Spin, Vector3.Zero, seal.EulerAngles, 0));
                break;
            }
            case SignatureKind.Quake:
            {
                var rock = _cache.Pbr(Palette.Mix(0x6A5A4A, theme.Secondary, 0.3f), 0.95f);
                var hot = _cache.Pbr(theme.Primary, 0.6f, 0, 3f);
                for (int i = 0; i < 12; i++)
                {
                    float side = i % 2 == 0 ? 1 : -1;
                    float z = (i / 2 % 3 - 1) * 0.5f;
                    var shard = _cache.Add(_cache.Cone, i % 4 == 0 ? hot : rock, new Vector3(side, 0, z), new Vector3(0.5f, 1.2f, 0.5f), body, new Vector3(0, i * 0.7f, side * 0.25f));
                    rig.Parts.Add(new Part(shard, PartAnim.Shard, new Vector3(side, 0, z), shard.EulerAngles, i / 2 * 0.35f));
                }
                // A hairline ring so the tube stays thin however far the front spreads.
                var thin = _cache.Get("signature-thin-ring", () => _scene.CreateTorusGeometry(0.5f, 0.006f, 6, 72));
                rig.Warning = _cache.Add(thin, _cache.Pbr(theme.Primary, 0.4f, 0, 6f), new Vector3(0, 0.05f, 0), Vector3.One, body, new Vector3(MathF.PI / 2, 0, 0));
                break;
            }
            case SignatureKind.Wave:
            {
                if (theme.Prop is SignatureProp.Sound)
                {
                    // Concentric sound rings travelling with the front.
                    for (int i = 0; i < 4; i++)
                    {
                        var ring = _cache.Add(_cache.Torus, _cache.Pbr(i % 2 == 0 ? theme.Primary : theme.Secondary, 0.3f, 0, 6f), new Vector3(-i * 0.35f, 1.3f, 0), new Vector3(1.2f + i * 0.5f, 1.2f + i * 0.5f, 1), body, new Vector3(0, MathF.PI / 2, 0));
                        rig.Parts.Add(new Part(ring, PartAnim.Wobble, ring.Position, ring.EulerAngles, i * 0.5f));
                    }
                }
                else
                {
                    // A curling wall as wide as the arena is deep: a low swell, a leaning face and a foam crest.
                    var wall = _cache.Unlit(theme.Primary, 0.55f);
                    var deep = _cache.Unlit(Palette.Darken(theme.Primary, 0.35f), 0.6f);
                    var crest = _cache.Pbr(theme.Secondary, 0.3f, 0, 2f);
                    _cache.Add(_cache.Sphere, deep, new Vector3(-0.9f, 0.35f, 0), new Vector3(2.6f, 1.0f, 3.6f), body);
                    _cache.Add(_cache.Sphere, wall, new Vector3(-0.2f, 1.2f, 0), new Vector3(1.3f, 2.6f, 3.6f), body, new Vector3(0, 0, -0.4f));
                    for (int i = 0; i < 7; i++)
                    {
                        var c = _cache.Add(_cache.LowSphere, crest, new Vector3(0.5f, 2.3f - MathF.Abs(i - 3) * 0.08f, (i - 3) * 0.48f), new Vector3(0.6f, 0.4f, 0.6f), body);
                        rig.Parts.Add(new Part(c, PartAnim.Wobble, c.Position, Vector3.Zero, i * 0.8f));
                    }
                    if (theme.Prop is not SignatureProp.Water)
                        for (int i = 0; i < 3; i++)
                        {
                            var n = _scene.CreateNode(body);
                            n.Position = new Vector3(0.3f, 0.8f + i * 0.6f, (i - 1) * 0.8f);
                            BuildProp(theme.Prop, theme, n, 0.5f, rig);
                            rig.Parts.Add(new Part(n, PartAnim.Wobble, n.Position, Vector3.Zero, i));
                        }
                }
                break;
            }
        }
        root.SetShadowsRecursive(false, false);
        return rig;
    }

    private (ParticleEffect, Geometry) BuildEmitter(SignatureTheme theme, Shader glow, Shader smoke, int quality, int seed)
    {
        var a = Palette.Hex3(theme.Primary);
        var b = Palette.Hex3(theme.Secondary);
        var fx = theme.Flavor switch
        {
            SignatureFlavor.Fire => new ParticleEffect(260 / quality, seed) { Lifetime = 0.7f, Size = 0.16f, Growth = 0.6f, Shape = EmitterShape.Sphere, EmitterRadius = 0.35f, Rise = 2.4f, Spread = 0.8f, Gravity = new Vector3(0, 2, 0), Turbulence = 2.5f },
            SignatureFlavor.Water => new ParticleEffect(260 / quality, seed) { Lifetime = 0.8f, Size = 0.09f, Growth = 0.3f, Shape = EmitterShape.Sphere, EmitterRadius = 0.4f, Radial = true, Spread = 4f, Gravity = new Vector3(0, -9, 0), StretchBySpeed = 0.04f },
            SignatureFlavor.Leaves or SignatureFlavor.Feathers or SignatureFlavor.Paper => new ParticleEffect(220 / quality, seed) { Lifetime = 1.6f, Size = 0.12f, Growth = 0.1f, Shape = EmitterShape.Sphere, EmitterRadius = 0.5f, Radial = true, Spread = 2.5f, Gravity = new Vector3(0, -1.2f, 0), Drag = 0.7f, Turbulence = 3f, SpinRate = 6f },
            SignatureFlavor.Smoke or SignatureFlavor.Dust => new ParticleEffect(160 / quality, seed) { Lifetime = 1.4f, Size = 0.4f, Growth = 2.2f, Shape = EmitterShape.Sphere, EmitterRadius = 0.4f, Rise = 0.8f, Spread = 1.2f, Drag = 0.8f, Turbulence = 1f },
            SignatureFlavor.Ghostly => new ParticleEffect(200 / quality, seed) { Lifetime = 1.3f, Size = 0.18f, Growth = 1.2f, Shape = EmitterShape.Sphere, EmitterRadius = 0.4f, Rise = 0.6f, Spread = 0.5f, Turbulence = 1.8f },
            SignatureFlavor.Electric => new ParticleEffect(260 / quality, seed) { Lifetime = 0.25f, Size = 0.06f, Growth = 0.1f, Shape = EmitterShape.Sphere, EmitterRadius = 0.3f, Radial = true, Spread = 9f, Drag = 0.4f, StretchBySpeed = 0.08f },
            SignatureFlavor.Notes => new ParticleEffect(160 / quality, seed) { Lifetime = 1.2f, Size = 0.13f, Growth = 0.2f, Shape = EmitterShape.Sphere, EmitterRadius = 0.5f, Rise = 1.6f, Spread = 1f, Turbulence = 1.2f, SpinRate = 3f },
            SignatureFlavor.Gold => new ParticleEffect(240 / quality, seed) { Lifetime = 1.0f, Size = 0.1f, Growth = 0.3f, Shape = EmitterShape.Sphere, EmitterRadius = 0.4f, Radial = true, Spread = 3f, Gravity = new Vector3(0, -3, 0), Turbulence = 1f, SpinRate = 5f },
            _ => new ParticleEffect(240 / quality, seed) { Lifetime = 0.45f, Size = 0.08f, Growth = 0.2f, Shape = EmitterShape.Sphere, EmitterRadius = 0.25f, Radial = true, Spread = 5f, Gravity = new Vector3(0, -6, 0), Drag = 0.5f, StretchBySpeed = 0.04f },
        };
        fx.Rate = 0;
        fx.Variation = 0.5f;
        var material = theme.Flavor is SignatureFlavor.Smoke or SignatureFlavor.Dust
            ? ParticleEffect.SmokeMaterial(smoke, a, 0.22f)
            : ParticleEffect.GlowMaterial(glow, a, b, theme.Flavor is SignatureFlavor.Ghostly ? 2.2f : 4.5f);
        var geometry = fx.CreateGeometry(_scene);
        var node = _scene.AddMesh(geometry, _scene.CreateMaterial(material), name: "signature-fx");
        node.CastShadow = false;
        node.ReceiveShadow = false;
        return (fx, geometry);
    }

    /// <summary>
    /// Builds a prop from primitives under <paramref name="parent"/>, about <paramref name="size"/>
    /// metres tall, facing +X (the presentation yaws the rig to the owner's facing).
    /// </summary>
    private void BuildProp(SignatureProp prop, SignatureTheme theme, Node parent, float size, Rig? rig, int variant = 0)
    {
        var c = _cache;
        var main = c.Pbr(theme.Primary, 0.6f);
        var accent = c.Pbr(theme.Secondary, 0.5f);
        var glowMain = c.Pbr(theme.Primary, 0.3f, 0, 4f);
        var glowAccent = c.Pbr(theme.Secondary, 0.3f, 0, 3f);
        var skin = c.Pbr(variant % 3 == 0 ? 0xC68A5Au : variant % 3 == 1 ? 0xA8724Au : 0xD8A070u, 0.8f);
        var dark = c.Pbr(0x1A1A1A, 0.8f);
        float k = size;
        Vector3 V(float x, float y, float z) => new(x * k, y * k, z * k);

        switch (prop)
        {
            case SignatureProp.Orb:
                c.Add(c.Sphere, glowMain, Vector3.Zero, V(0.6f, 0.6f, 0.6f), parent);
                c.Add(c.Sphere, c.Unlit(theme.Secondary, 0.35f), Vector3.Zero, V(0.95f, 0.95f, 0.95f), parent);
                break;
            case SignatureProp.Ghost:
                c.Add(c.Sphere, c.Pbr(0xF2EEE4, 0.9f, 0, 0.6f), V(0, 0.25f, 0), V(0.6f, 0.62f, 0.6f), parent);
                c.Add(c.Cone, c.Unlit(0xF2EEE4, 0.7f), V(0, -0.25f, 0), V(0.62f, 0.9f, 0.62f), parent, new Vector3(MathF.PI, 0, 0));
                c.Add(c.LowSphere, dark, V(0.25f, 0.32f, 0.12f), V(0.1f, 0.13f, 0.1f), parent);
                c.Add(c.LowSphere, dark, V(0.25f, 0.32f, -0.12f), V(0.1f, 0.13f, 0.1f), parent);
                c.Add(c.Torus, glowMain, V(0, 0.65f, 0), V(0.4f, 0.4f, 0.4f), parent, new Vector3(MathF.PI / 2, 0, 0));
                break;
            case SignatureProp.Head:
                // A flying head trailing glowing entrails (kuyang) or a floating mask.
                c.Add(c.Sphere, skin, Vector3.Zero, V(0.5f, 0.55f, 0.5f), parent);
                c.Add(c.Sphere, dark, V(-0.06f, 0.1f, 0), V(0.54f, 0.5f, 0.54f), parent);
                c.Add(c.LowSphere, glowMain, V(0.22f, 0.04f, 0.1f), V(0.08f, 0.08f, 0.08f), parent);
                c.Add(c.LowSphere, glowMain, V(0.22f, 0.04f, -0.1f), V(0.08f, 0.08f, 0.08f), parent);
                for (int i = 0; i < 3; i++)
                    c.Add(c.Cylinder, glowAccent, V(-0.05f + i * 0.04f, -0.5f, (i - 1) * 0.1f), V(0.06f, 0.6f, 0.06f), parent, new Vector3(0, 0, 0.2f - i * 0.2f));
                break;
            case SignatureProp.Person:
            {
                // A protester / bodyguard / supporter: shirt in the theme colour, a placard or flag.
                var shirt = variant % 2 == 0 ? main : accent;
                c.Add(c.Capsule, shirt, V(0, 0.85f, 0), V(0.42f, 0.55f, 0.3f), parent);
                c.Add(c.Capsule, dark, V(0, 0.32f, 0), V(0.34f, 0.45f, 0.26f), parent);
                c.Add(c.Sphere, skin, V(0, 1.45f, 0), V(0.3f, 0.32f, 0.3f), parent);
                c.Add(c.Cylinder, variant % 3 == 2 ? main : dark, V(0, 1.6f, 0), V(0.32f, 0.06f, 0.32f), parent);
                c.Add(c.Cylinder, c.Pbr(0x8C6A3A, 0.8f), V(0.12f, 1.55f, 0.18f), V(0.04f, 1.2f, 0.04f), parent);
                c.Add(c.Box, variant % 2 == 0 ? c.Pbr(0xF2EEE4, 0.8f) : glowAccent, V(0.12f, 2.1f, 0.18f), V(0.05f, 0.4f, 0.65f), parent);
                break;
            }
            case SignatureProp.Beast:
                c.Add(c.Capsule, main, V(0, 0.6f, 0), V(0.5f, 0.45f, 0.4f), parent, new Vector3(0, 0, MathF.PI / 2));
                c.Add(c.Sphere, main, V(0.55f, 0.8f, 0), V(0.42f, 0.4f, 0.38f), parent);
                c.Add(c.LowSphere, glowAccent, V(0.72f, 0.88f, 0.1f), V(0.07f, 0.07f, 0.07f), parent);
                c.Add(c.LowSphere, glowAccent, V(0.72f, 0.88f, -0.1f), V(0.07f, 0.07f, 0.07f), parent);
                c.Add(c.Cone, accent, V(0.5f, 1.05f, 0.12f), V(0.08f, 0.3f, 0.08f), parent, new Vector3(0.4f, 0, -0.3f));
                c.Add(c.Cone, accent, V(0.5f, 1.05f, -0.12f), V(0.08f, 0.3f, 0.08f), parent, new Vector3(-0.4f, 0, -0.3f));
                foreach (var (lx, lz) in new[] { (0.3f, 0.15f), (0.3f, -0.15f), (-0.3f, 0.15f), (-0.3f, -0.15f) })
                    c.Add(c.Cylinder, main, V(lx, 0.2f, lz), V(0.1f, 0.4f, 0.1f), parent);
                break;
            case SignatureProp.Bird:
            {
                c.Add(c.Sphere, main, Vector3.Zero, V(0.5f, 0.35f, 0.32f), parent);
                c.Add(c.Sphere, main, V(0.28f, 0.12f, 0), V(0.24f, 0.24f, 0.24f), parent);
                c.Add(c.Cone, c.Pbr(0xE0A020, 0.5f), V(0.44f, 0.1f, 0), V(0.08f, 0.2f, 0.08f), parent, new Vector3(0, 0, -MathF.PI / 2));
                foreach (float side in new[] { 1f, -1f })
                {
                    var wing = c.Add(c.Box, accent, V(0, 0.05f, side * 0.35f), V(0.35f, 0.04f, 0.6f), parent);
                    rig?.Parts.Add(new Part(wing, PartAnim.Flap, wing.Position, new Vector3(side * 0.2f, 0, 0), side > 0 ? 0 : MathF.PI / 18));
                }
                break;
            }
            case SignatureProp.Bike:
                // An ojol motorbike with a rider in the green jacket.
                foreach (float x in new[] { 0.45f, -0.45f })
                    c.Add(c.Torus, dark, V(x, 0.3f, 0), V(0.6f, 0.6f, 0.9f), parent);
                c.Add(c.Box, main, V(0, 0.55f, 0), V(1.0f, 0.3f, 0.3f), parent);
                c.Add(c.Capsule, main, V(-0.05f, 1.0f, 0), V(0.38f, 0.45f, 0.3f), parent);
                c.Add(c.Sphere, accent, V(0, 1.45f, 0), V(0.32f, 0.32f, 0.32f), parent);
                c.Add(c.Sphere, glowAccent, V(0.55f, 0.7f, 0), V(0.12f, 0.12f, 0.12f), parent);
                break;
            case SignatureProp.Truck:
                // A sound-system truck: cab, a speaker wall, flashing lights.
                c.Add(c.Box, main, V(0.9f, 0.8f, 0), V(0.8f, 1.0f, 1.3f), parent);
                c.Add(c.Box, dark, V(-0.3f, 0.6f, 0), V(1.8f, 0.4f, 1.4f), parent);
                for (int y = 0; y < 3; y++)
                for (int z = 0; z < 3; z++)
                {
                    c.Add(c.Box, c.Pbr(0x101010, 0.7f), V(-0.3f, 1.05f + y * 0.45f, (z - 1) * 0.45f), V(0.4f, 0.42f, 0.42f), parent);
                    c.Add(c.Cylinder, glowAccent, V(-0.08f, 1.05f + y * 0.45f, (z - 1) * 0.45f), V(0.3f, 0.04f, 0.3f), parent, new Vector3(0, 0, MathF.PI / 2));
                }
                foreach (float x in new[] { 0.9f, -0.8f })
                foreach (float z in new[] { 0.7f, -0.7f })
                    c.Add(c.Cylinder, dark, V(x, 0.3f, z), V(0.55f, 0.15f, 0.55f), parent, new Vector3(MathF.PI / 2, 0, 0));
                c.Add(c.Box, glowMain, V(-0.3f, 2.45f, 0), V(1.6f, 0.08f, 1.4f), parent);
                break;
            case SignatureProp.Cart:
                c.Add(c.Box, main, V(0, 0.7f, 0), V(1.1f, 0.6f, 0.6f), parent);
                c.Add(c.Box, c.Unlit(0xBFE6F5, 0.6f), V(0, 1.15f, 0), V(1.0f, 0.3f, 0.55f), parent);
                c.Add(c.Cone, accent, V(0, 1.85f, 0), V(1.6f, 0.4f, 1.2f), parent);
                c.Add(c.Cylinder, dark, V(0, 1.5f, 0), V(0.04f, 0.7f, 0.04f), parent);
                foreach (float z in new[] { 0.32f, -0.32f })
                    c.Add(c.Torus, dark, V(0.2f, 0.25f, z), V(0.5f, 0.5f, 0.8f), parent);
                break;
            case SignatureProp.Pole:
                // A long pole with a heavy head: the pacul / mic stand / flagpole family.
                c.Add(c.Cylinder, c.Pbr(0x7A5230, 0.8f), V(0, 0.6f, 0), V(0.08f, 1.3f, 0.08f), parent);
                c.Add(c.Box, glowMain, V(0.18f, 1.25f, 0), V(0.45f, 0.12f, 0.35f), parent, new Vector3(0, 0, -0.2f));
                c.Add(c.Box, accent, V(0, 1.22f, 0), V(0.12f, 0.16f, 0.12f), parent);
                break;
            case SignatureProp.Blade:
                c.Add(c.Cylinder, c.Pbr(0x3A2A1A, 0.8f), V(0, 0.15f, 0), V(0.08f, 0.35f, 0.08f), parent);
                c.Add(c.Box, accent, V(0, 0.34f, 0), V(0.25f, 0.05f, 0.1f), parent);
                c.Add(c.Box, c.Pbr(theme.Primary, 0.15f, 0.9f, 1.5f), V(0.03f, 0.95f, 0), V(0.16f, 1.2f, 0.03f), parent, new Vector3(0, 0, -0.05f));
                break;
            case SignatureProp.Hoe:
                // The pacul: a long wooden handle and a broad steel blade set at a right angle.
                c.Add(c.Cylinder, c.Pbr(0x8C6A3A, 0.8f), V(0, 0.7f, 0), V(0.07f, 1.45f, 0.07f), parent);
                c.Add(c.Box, c.Pbr(theme.Primary, 0.25f, 0.6f, 1.4f), V(0.3f, 1.22f, 0), V(0.66f, 0.07f, 0.56f), parent, new Vector3(0, 0, -1.25f));
                c.Add(c.Box, glowAccent, V(0.4f, 0.93f, 0), V(0.06f, 0.06f, 0.58f), parent, new Vector3(0, 0, -1.25f));
                c.Add(c.Cylinder, dark, V(0, 1.4f, 0), V(0.11f, 0.12f, 0.11f), parent);
                break;
            case SignatureProp.Paddle:
                c.Add(c.Cylinder, c.Pbr(0x8C6A3A, 0.8f), V(0, 0.45f, 0), V(0.07f, 0.9f, 0.07f), parent);
                c.Add(c.Box, main, V(0, 1.15f, 0), V(0.38f, 0.6f, 0.06f), parent);
                c.Add(c.Box, glowAccent, V(0, 1.15f, 0.035f), V(0.25f, 0.4f, 0.01f), parent);
                break;
            case SignatureProp.Slab:
                // Briefcases, gavels, giant books and rulers: a heavy block on a short handle.
                c.Add(c.Cylinder, dark, V(0, 0.4f, 0), V(0.08f, 0.8f, 0.08f), parent);
                c.Add(c.Box, main, V(0, 1.0f, 0), V(0.65f, 0.45f, 0.3f), parent);
                c.Add(c.Box, glowAccent, V(0, 1.0f, 0.16f), V(0.5f, 0.06f, 0.02f), parent);
                break;
            case SignatureProp.Hand:
                c.Add(c.Box, glowMain, V(0, 0.5f, 0), V(0.55f, 0.6f, 0.2f), parent);
                for (int i = 0; i < 4; i++)
                    c.Add(c.Capsule, glowMain, V(-0.2f + i * 0.13f, 0.95f, 0), V(0.1f, 0.25f, 0.1f), parent);
                c.Add(c.Capsule, glowMain, V(0.33f, 0.6f, 0), V(0.1f, 0.2f, 0.1f), parent, new Vector3(0, 0, -0.8f));
                c.Add(c.Torus, glowAccent, V(0, 0.2f, 0), V(0.6f, 0.6f, 0.6f), parent, new Vector3(MathF.PI / 2, 0, 0));
                break;
            case SignatureProp.Card:
                // Envelopes, money, flyers, phones: a flat glowing rectangle.
                c.Add(c.Box, main, Vector3.Zero, V(0.5f, 0.3f, 0.04f), parent);
                c.Add(c.Box, glowAccent, V(0, 0, 0.025f), V(0.3f, 0.12f, 0.01f), parent);
                break;
            case SignatureProp.Ball:
                c.Add(c.Sphere, main, Vector3.Zero, V(0.45f, 0.45f, 0.45f), parent);
                c.Add(c.Torus, accent, Vector3.Zero, V(0.46f, 0.46f, 0.3f), parent);
                break;
            case SignatureProp.Bolt:
                c.Add(c.Box, glowMain, Vector3.Zero, V(0.12f, 0.6f, 0.12f), parent, new Vector3(0, 0, 0.5f));
                c.Add(c.Box, glowAccent, V(0.05f, 0.35f, 0), V(0.1f, 0.4f, 0.1f), parent, new Vector3(0, 0, -0.5f));
                break;
            case SignatureProp.Fireball:
                c.Add(c.Sphere, c.Pbr(0xFFD040, 0.3f, 0, 8f), Vector3.Zero, V(0.4f, 0.4f, 0.4f), parent);
                c.Add(c.Sphere, c.Unlit(theme.Primary, 0.5f), Vector3.Zero, V(0.65f, 0.6f, 0.6f), parent);
                c.Add(c.Cone, c.Unlit(theme.Secondary, 0.4f), V(-0.35f, 0, 0), V(0.4f, 0.8f, 0.4f), parent, new Vector3(0, 0, MathF.PI / 2));
                break;
            case SignatureProp.Water:
                c.Add(c.Sphere, c.Unlit(theme.Primary, 0.65f), Vector3.Zero, V(0.6f, 0.7f, 0.6f), parent);
                c.Add(c.Sphere, c.Pbr(0xFFFFFF, 0.1f, 0, 2f), V(0.12f, 0.15f, 0.12f), V(0.15f, 0.15f, 0.15f), parent);
                break;
            case SignatureProp.Sound:
                for (int i = 0; i < 3; i++)
                    c.Add(c.Torus, i == 1 ? glowAccent : glowMain, V(i * 0.12f, 0, 0), V(0.3f + i * 0.2f, 0.3f + i * 0.2f, 0.4f), parent, new Vector3(0, MathF.PI / 2, 0));
                break;
            case SignatureProp.Leaf:
                c.Add(c.LowSphere, main, Vector3.Zero, V(0.5f, 0.06f, 0.25f), parent);
                c.Add(c.Box, accent, Vector3.Zero, V(0.5f, 0.02f, 0.02f), parent);
                break;
            case SignatureProp.Arrow:
                c.Add(c.Cylinder, c.Pbr(0x8C6A3A, 0.8f), Vector3.Zero, V(0.04f, 0.8f, 0.04f), parent, new Vector3(0, 0, MathF.PI / 2));
                c.Add(c.Cone, glowMain, V(0.45f, 0, 0), V(0.12f, 0.2f, 0.12f), parent, new Vector3(0, 0, -MathF.PI / 2));
                c.Add(c.Box, accent, V(-0.35f, 0, 0), V(0.15f, 0.12f, 0.02f), parent);
                break;
        }
    }
}
