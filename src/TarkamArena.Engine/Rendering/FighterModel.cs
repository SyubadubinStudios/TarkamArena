using System.Numerics;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;
using TarkamArena.Engine.Assets;
using ThreeNet;

namespace TarkamArena.Engine.Rendering;

/// <summary>The animation a fighter should be showing, derived from its simulation state.</summary>
public enum FighterClip : byte
{
    Idle,
    Walk,
    Crouch,
    Jump,
    Jab,
    Heavy,
    Kick,
    Uppercut,
    Special,
    Grab,
    Hit,
    Block,
    KO,
    Dizzy,
    Victory,
}

public static class FighterClips
{
    /// <summary>Maps simulation state to a clip and the time the clip should take (0 = natural length, looping).</summary>
    public static (FighterClip Clip, float Seconds) Select(in FighterState f, MatchContext context)
    {
        switch (f.Status)
        {
            case FighterStatus.Walk: return (FighterClip.Walk, 0);
            case FighterStatus.Crouch: return (f.Blocking ? FighterClip.Block : FighterClip.Crouch, 0);
            case FighterStatus.Airborne: return (FighterClip.Jump, 0);
            case FighterStatus.Hitstun: case FighterStatus.Juggle: return (FighterClip.Hit, 0.35f);
            case FighterStatus.Blockstun: return (FighterClip.Block, 0);
            case FighterStatus.Knockdown: case FighterStatus.KO: return (FighterClip.KO, 0.6f);
            case FighterStatus.GetUp: return (FighterClip.Idle, 0);
            case FighterStatus.Dodge: return (FighterClip.Jump, 0.33f);
            case FighterStatus.Parried: case FighterStatus.Dizzy: return (FighterClip.Dizzy, 0);
            case FighterStatus.Victory: return (FighterClip.Victory, 0);
            case FighterStatus.Finishing: return (FighterClip.Special, 0);
            case FighterStatus.Attack:
                var move = context.Move(f.Slot, f.Move);
                float seconds = move.TotalFrames / 60f;
                var clip = f.Move switch
                {
                    MoveId.Jab or MoveId.Cross => FighterClip.Jab,
                    MoveId.Hook or MoveId.Heavy or MoveId.AirHeavy or MoveId.SpecialForward => FighterClip.Heavy,
                    MoveId.LowLight or MoveId.AirLight => FighterClip.Kick,
                    MoveId.Launcher or MoveId.SpecialDown or MoveId.TagStrike => FighterClip.Uppercut,
                    MoveId.Grab => FighterClip.Grab,
                    _ => FighterClip.Special,
                };
                return (clip, seconds);
            default:
                return (f.Blocking ? FighterClip.Block : FighterClip.Idle, 0);
        }
    }

    public static bool Loops(FighterClip clip) => clip is FighterClip.Idle or FighterClip.Walk or FighterClip.Dizzy or FighterClip.Victory or FighterClip.Crouch;
}

/// <summary>A fighter on screen: an authored glTF (Rodin + Blender pipeline) or a procedural fallback.</summary>
public abstract class FighterModel
{
    protected FighterModel(Node root, CharacterDefinition character)
    {
        Root = root;
        Character = character;
    }

    public Node Root { get; }
    public CharacterDefinition Character { get; }
    public FighterClip CurrentClip { get; protected set; } = FighterClip.Idle;

    /// <summary>World position of the chest, for effects that attach to the fighter.</summary>
    public Vector3 ChestPosition => Root.Position + new Vector3(0, Character.Body.Height * 0.65f, 0);

    public static FighterModel Create(Scene scene, MeshCache cache, CharacterDefinition character)
    {
        var path = AssetLocator.PathOf(character.ModelPath);
        if (File.Exists(path))
        {
            try
            {
                return new GltfFighterModel(scene, character, path);
            }
            catch (ThreeNetException)
            {
                // Fall back to the procedural body when an asset fails to import.
            }
        }
        return new ProceduralFighterModel(scene, cache, character);
    }

    /// <summary>Applies the fighter's simulation state. Called once per rendered frame.</summary>
    public void Apply(in FighterState f, MatchContext context, float dt, float time)
    {
        Root.Visible = f.Present && f.OnField;
        if (!f.OnField) return;

        var p = f.Position.ToVector2();
        float yaw = f.Facing >= 0 ? MathF.PI / 2 : -MathF.PI / 2;
        float lean = 0;
        float lift = 0;
        float scaleY = 1f;
        if (f.Status == FighterStatus.Juggle && !f.Grounded)
            lean = -MathF.Min(1.2f, f.JuggleCount * 0.25f + 0.3f);
        if (f.Status == FighterStatus.Crouch) scaleY = 0.8f;
        if (f.Status == FighterStatus.Dodge) lift = MathF.Sin(MathF.Min(1, f.StatusFrame / 20f) * MathF.PI) * 0.15f;

        // A dodge steps into the background lane for a moment, which reads as a sidestep.
        float z = f.Status == FighterStatus.Dodge ? -MathF.Sin(MathF.Min(1, f.StatusFrame / 20f) * MathF.PI) * 0.7f : 0f;

        Root.Position = new Vector3(p.X, p.Y + lift, z);
        Root.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, 0, 0) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, lean);
        Root.Scale = new Vector3(1f, scaleY, 1f);

        var (clip, seconds) = FighterClips.Select(f, context);
        Animate(clip, seconds, f, dt, time);
    }

    protected abstract void Animate(FighterClip clip, float seconds, in FighterState f, float dt, float time);
}

/// <summary>Skinned glTF fighter driven by the clip set the Blender pipeline exports.</summary>
public sealed class GltfFighterModel : FighterModel
{
    private readonly Dictionary<FighterClip, AnimationClip> _clips = [];
    private readonly AnimationMixer _mixer = new();
    private AnimationPlayer? _player;
    private int _lastStatusFrame = -1;
    private FighterStatus _lastStatus;
    private MoveId _lastMove;

    public GltfFighterModel(Scene scene, CharacterDefinition character, string path)
        : base(scene.CreateNode(name: character.Id), character)
    {
        int before = scene.Animations.Count;
        var result = scene.LoadGltf(path, Root);
        result.Root.SetShadowsRecursive(true, true);
        var all = scene.Animations;
        for (int i = before; i < all.Count; i++)
        {
            var name = all[i].Name;
            if (name == "Kick" && !_clips.ContainsKey(FighterClip.Kick)) _clips[FighterClip.Kick] = all[i];
            if (Enum.TryParse<FighterClip>(name, out var c)) _clips[c] = all[i];
        }
        if (_clips.TryGetValue(FighterClip.Idle, out var idle)) _player = idle.Play(loop: true);
    }

    public int ClipCount => _clips.Count;

    protected override void Animate(FighterClip clip, float seconds, in FighterState f, float dt, float time)
    {
        // Crouch has no authored clip; the root squash handles it over the idle pose.
        var source = clip == FighterClip.Crouch ? FighterClip.Block : clip;
        bool restart = f.Status != _lastStatus || f.Move != _lastMove || (f.Status == FighterStatus.Attack && f.StatusFrame < _lastStatusFrame);
        _lastStatus = f.Status;
        _lastMove = f.Move;
        _lastStatusFrame = f.StatusFrame;

        if ((source != CurrentClip || restart) && _clips.TryGetValue(source, out var next))
        {
            bool loop = FighterClips.Loops(clip);
            float speed = seconds > 0 && next.Duration > 0 ? next.Duration / seconds : 1f;
            float fade = clip is FighterClip.Jab or FighterClip.Kick ? 0.04f : 0.09f;
            var player = next.Play(loop, speed, weight: 0f);
            // The old player fades out and is removed once silent, so players never pile up.
            if (_player is not null) _mixer.FadeOut(_player, fade, stop: true);
            _mixer.FadeIn(player, fade);
            _player = player;
            CurrentClip = source;
        }
        _mixer.Update(dt);
    }
}

/// <summary>
/// Fallback fighter built from primitives in the character's colours and silhouette features.
/// Used for DLC and creator fighters, and whenever an authored model is missing.
/// </summary>
public sealed class ProceduralFighterModel : FighterModel
{
    private readonly Node _hips, _spine, _head;
    private readonly Node _shoulderL, _shoulderR, _elbowL, _elbowR;
    private readonly Node _hipL, _hipR, _kneeL, _kneeR;
    private readonly Node? _tail;
    private Pose _pose;

    private struct Pose
    {
        public float ShoulderL, ShoulderR, ArmOutL, ArmOutR, ElbowL, ElbowR, HipL, HipR, KneeL, KneeR, Spine, SpineTwist, Head, Lift;

        public static Pose Lerp(in Pose a, in Pose b, float t) => new()
        {
            ShoulderL = a.ShoulderL + (b.ShoulderL - a.ShoulderL) * t,
            ShoulderR = a.ShoulderR + (b.ShoulderR - a.ShoulderR) * t,
            ArmOutL = a.ArmOutL + (b.ArmOutL - a.ArmOutL) * t,
            ArmOutR = a.ArmOutR + (b.ArmOutR - a.ArmOutR) * t,
            ElbowL = a.ElbowL + (b.ElbowL - a.ElbowL) * t,
            ElbowR = a.ElbowR + (b.ElbowR - a.ElbowR) * t,
            HipL = a.HipL + (b.HipL - a.HipL) * t,
            HipR = a.HipR + (b.HipR - a.HipR) * t,
            KneeL = a.KneeL + (b.KneeL - a.KneeL) * t,
            KneeR = a.KneeR + (b.KneeR - a.KneeR) * t,
            Spine = a.Spine + (b.Spine - a.Spine) * t,
            SpineTwist = a.SpineTwist + (b.SpineTwist - a.SpineTwist) * t,
            Head = a.Head + (b.Head - a.Head) * t,
            Lift = a.Lift + (b.Lift - a.Lift) * t,
        };
    }

    public ProceduralFighterModel(Scene scene, MeshCache cache, CharacterDefinition character)
        : base(scene.CreateNode(name: character.Id), character)
    {
        var body = character.Body;
        float h = body.Height;
        float w = body.Width;
        var features = body.Features;
        var primary = cache.Pbr(body.PrimaryColor, 0.75f);
        var secondary = cache.Pbr(body.SecondaryColor, 0.6f);
        var skin = cache.Pbr(body.SkinColor, 0.55f);
        var accent = cache.Pbr(body.AccentColor, 0.4f, 0.2f, features.HasFlag(BodyFeature.Glow) ? 2.5f : 0f);
        bool floating = features.HasFlag(BodyFeature.Floating);
        bool bound = features.HasFlag(BodyFeature.Bound) || features.HasFlag(BodyFeature.Shroud);

        float legLen = h * 0.47f;
        float torsoLen = h * 0.30f;
        float headSize = h * 0.13f * body.HeadScale;

        _hips = scene.CreateNode(Root, "hips");
        _hips.Position = new Vector3(0, legLen, 0);
        cache.Add(cache.Sphere, secondary, Vector3.Zero, new Vector3(0.30f * w, 0.16f * h * 0.6f, 0.2f * w), _hips);

        _spine = scene.CreateNode(_hips, "spine");
        _spine.Position = new Vector3(0, 0.03f * h, 0);
        cache.Add(cache.Capsule, primary, new Vector3(0, torsoLen * 0.5f, 0), new Vector3(0.34f * w, torsoLen * 0.5f, 0.22f * w), _spine);
        if (features.HasFlag(BodyFeature.Apron) || features.HasFlag(BodyFeature.Uniform))
            cache.Add(cache.Box, features.HasFlag(BodyFeature.Apron) ? secondary : accent, new Vector3(0, torsoLen * 0.35f, 0.11f * w), new Vector3(0.3f * w, torsoLen * 0.6f, 0.02f), _spine);

        _head = scene.CreateNode(_spine, "head");
        _head.Position = new Vector3(0, torsoLen + headSize * 0.55f, 0);
        cache.Add(cache.Sphere, features.HasFlag(BodyFeature.Shroud) ? primary : skin, Vector3.Zero, new Vector3(headSize, headSize * 1.1f, headSize), _head);
        AddHeadFeatures(cache, features, headSize, primary, secondary, accent);

        Node Limb(Node parent, Vector3 at, float length, float thickness, Material mat, string name)
        {
            var joint = scene.CreateNode(parent, name);
            joint.Position = at;
            cache.Add(cache.Capsule, mat, new Vector3(0, -length * 0.5f, 0), new Vector3(thickness, length * 0.5f, thickness), joint);
            return joint;
        }

        float armLen = h * (features.HasFlag(BodyFeature.LongArms) ? 0.2f : 0.16f);
        float shoulderY = torsoLen * 0.9f;
        float shoulderX = 0.2f * w;
        var armMat = bound ? primary : skin;
        _shoulderL = Limb(_spine, new Vector3(shoulderX, shoulderY, 0), armLen, 0.075f * w, features.HasFlag(BodyFeature.Fur) ? primary : armMat, "shoulder.L");
        _shoulderR = Limb(_spine, new Vector3(-shoulderX, shoulderY, 0), armLen, 0.075f * w, features.HasFlag(BodyFeature.Fur) ? primary : armMat, "shoulder.R");
        _elbowL = Limb(_shoulderL, new Vector3(0, -armLen, 0), armLen, 0.065f * w, skin, "elbow.L");
        _elbowR = Limb(_shoulderR, new Vector3(0, -armLen, 0), armLen, 0.065f * w, skin, "elbow.R");
        AddFist(_elbowL, 1);
        AddFist(_elbowR, -1);
        void AddFist(Node elbow, int side)
        {
            // A closed palm, four knuckles and a tucked thumb give fallback fighters
            // a readable hand silhouette at the same scale as authored characters.
            var hand = scene.CreateNode(elbow, $"hand.{(side > 0 ? "L" : "R")}");
            hand.Position = new Vector3(0, -armLen, 0);
            cache.Add(cache.Sphere, skin, Vector3.Zero, new Vector3(0.067f * w, 0.085f * w, 0.052f * w), hand);
            for (int finger = 0; finger < 4; finger++)
            {
                float x = (finger - 1.5f) * 0.031f * w;
                cache.Add(cache.Sphere, skin, new Vector3(x, -0.054f * w, 0.018f * w),
                    new Vector3(0.022f * w, 0.036f * w, 0.038f * w), hand);
            }
            cache.Add(cache.Sphere, skin, new Vector3(side * 0.055f * w, -0.004f * w, 0.035f * w),
                new Vector3(0.03f * w, 0.055f * w, 0.03f * w), hand);
        }
        if (features.HasFlag(BodyFeature.Weapon))
            cache.Add(cache.Box, cache.Pbr(0xB0B0B8, 0.3f, 0.8f), new Vector3(0, -armLen - 0.25f, 0.05f), new Vector3(0.04f, 0.5f, 0.08f), _elbowR);

        float thigh = legLen * 0.5f;
        var legMat = floating || bound ? primary : secondary;
        _hipL = Limb(_hips, new Vector3(0.09f * w, 0, 0), thigh, 0.09f * w, legMat, "hip.L");
        _hipR = Limb(_hips, new Vector3(-0.09f * w, 0, 0), thigh, 0.09f * w, legMat, "hip.R");
        _kneeL = Limb(_hipL, new Vector3(0, -thigh, 0), thigh, 0.075f * w, legMat, "knee.L");
        _kneeR = Limb(_hipR, new Vector3(0, -thigh, 0), thigh, 0.075f * w, legMat, "knee.R");
        if (!floating && !bound)
        {
            var shoe = cache.Pbr(Palette.Darken(body.SecondaryColor, 0.6f), 0.5f);
            cache.Add(cache.Box, shoe, new Vector3(0, -thigh, 0.05f), new Vector3(0.1f * w, 0.06f, 0.2f), _kneeL);
            cache.Add(cache.Box, shoe, new Vector3(0, -thigh, 0.05f), new Vector3(0.1f * w, 0.06f, 0.2f), _kneeR);
        }
        if (floating || bound)
        {
            // A robe or shroud hides the legs.
            cache.Add(cache.Cone, primary, new Vector3(0, -legLen * 0.45f, 0), new Vector3(0.45f * w, legLen * 0.95f, 0.38f * w), _hips, new Vector3(MathF.PI, 0, 0));
        }

        if (features.HasFlag(BodyFeature.Tail))
        {
            _tail = scene.CreateNode(_hips, "tail");
            _tail.Position = new Vector3(0, 0, -0.12f * w);
            cache.Add(cache.Capsule, features.HasFlag(BodyFeature.Scales) ? primary : secondary, new Vector3(0, 0, -0.35f * h * 0.5f), new Vector3(0.07f * w, 0.35f * h * 0.5f, 0.07f * w), _tail, new Vector3(MathF.PI / 2, 0, 0));
        }
        if (features.HasFlag(BodyFeature.Wings))
        {
            foreach (int side in (int[])[1, -1])
                cache.Add(cache.Box, accent, new Vector3(side * 0.35f * w, torsoLen * 0.7f, -0.15f), new Vector3(0.6f * w, 0.5f * h * 0.6f, 0.03f), _spine, new Vector3(0, side * 0.5f, side * -0.3f));
        }
        if (features.HasFlag(BodyFeature.Cape))
            cache.Add(cache.Box, accent, new Vector3(0, torsoLen * 0.3f, -0.14f * w), new Vector3(0.42f * w, torsoLen * 1.6f, 0.03f), _spine, new Vector3(0.15f, 0, 0));
        if (features.HasFlag(BodyFeature.Cart))
        {
            // The vendor's cart stays behind him like a backpack-sized prop.
            cache.Add(cache.Box, cache.Pbr(0x8C5A2B, 0.8f), new Vector3(0, torsoLen * 0.5f, -0.3f), new Vector3(0.4f, 0.35f, 0.25f), _spine);
        }
    }

    private void AddHeadFeatures(MeshCache cache, BodyFeature f, float s, Material primary, Material secondary, Material accent)
    {
        if (f.HasFlag(BodyFeature.Hat))
        {
            cache.Add(cache.Cylinder, secondary, new Vector3(0, s * 0.45f, 0), new Vector3(s * 1.5f, s * 0.08f, s * 1.5f), _head);
            cache.Add(cache.Cylinder, secondary, new Vector3(0, s * 0.62f, 0), new Vector3(s * 0.9f, s * 0.35f, s * 0.9f), _head);
        }
        if (f.HasFlag(BodyFeature.Helmet))
            cache.Add(cache.Sphere, accent, new Vector3(0, s * 0.18f, 0), new Vector3(s * 1.15f, s * 1.0f, s * 1.15f), _head);
        if (f.HasFlag(BodyFeature.Headscarf))
            cache.Add(cache.Sphere, primary, new Vector3(0, s * 0.05f, -s * 0.05f), new Vector3(s * 1.12f, s * 1.18f, s * 1.1f), _head);
        if (f.HasFlag(BodyFeature.LongHair))
            cache.Add(cache.Capsule, cache.Pbr(0x15120F, 0.8f), new Vector3(0, -s * 0.4f, -s * 0.35f), new Vector3(s * 0.55f, s * 0.9f, s * 0.3f), _head);
        if (f.HasFlag(BodyFeature.Horns))
            foreach (int side in (int[])[1, -1])
                cache.Add(cache.Cone, cache.Pbr(0xE8DCC0, 0.5f), new Vector3(side * s * 0.35f, s * 0.55f, 0), new Vector3(s * 0.22f, s * 0.6f, s * 0.22f), _head, new Vector3(0, 0, -side * 0.4f));
        if (f.HasFlag(BodyFeature.Crown))
            cache.Add(cache.Cylinder, cache.Pbr(0xD4AF37, 0.25f, 0.9f), new Vector3(0, s * 0.55f, 0), new Vector3(s * 0.85f, s * 0.3f, s * 0.85f), _head);
        if (f.HasFlag(BodyFeature.Glasses))
            cache.Add(cache.Box, cache.Pbr(0x111111, 0.2f), new Vector3(0, s * 0.08f, s * 0.48f), new Vector3(s * 0.8f, s * 0.12f, s * 0.05f), _head);
        if (f.HasFlag(BodyFeature.Mask))
            cache.Add(cache.Box, accent, new Vector3(0, 0, s * 0.45f), new Vector3(s * 0.85f, s * 0.85f, s * 0.1f), _head);
        if (f.HasFlag(BodyFeature.Snout) || f.HasFlag(BodyFeature.Beak))
            cache.Add(f.HasFlag(BodyFeature.Beak) ? cache.Cone : cache.Sphere, f.HasFlag(BodyFeature.Beak) ? accent : secondary,
                new Vector3(0, -s * 0.1f, s * 0.55f), new Vector3(s * 0.45f, s * 0.5f, s * 0.6f), _head, new Vector3(f.HasFlag(BodyFeature.Beak) ? MathF.PI / 2 : 0, 0, 0));
        if (f.HasFlag(BodyFeature.Glow))
            cache.Add(cache.Sphere, cache.Pbr(0xFFF0A0, 0.2f, 0, 6f), new Vector3(0, s * 0.1f, s * 0.47f), new Vector3(s * 0.5f, s * 0.12f, s * 0.05f), _head);
    }

    private static float Deg(float d) => d * MathF.PI / 180f;

    private static Pose Target(FighterClip clip, in FighterState f, float time)
    {
        // Angles in degrees; negative shoulder pitch swings the arm forward, negative hip pitch lifts the leg forward.
        float breathe = MathF.Sin(time * 3.2f);
        var guard = new Pose
        {
            ShoulderL = -55, ShoulderR = -40, ArmOutL = 12, ArmOutR = 12, ElbowL = -95, ElbowR = -100,
            HipL = -12, HipR = 10, KneeL = 18, KneeR = 10, Spine = 6 + breathe * 2, Head = -4, Lift = -0.02f + breathe * 0.01f,
        };
        float t = f.StatusFrame / 60f;
        switch (clip)
        {
            case FighterClip.Walk:
                float s = MathF.Sin(time * 9f);
                guard.HipL = s * 28; guard.HipR = -s * 28;
                guard.KneeL = MathF.Max(0, -s) * 35 + 8; guard.KneeR = MathF.Max(0, s) * 35 + 8;
                guard.Lift = MathF.Abs(s) * 0.03f;
                return guard;
            case FighterClip.Crouch:
                guard.HipL = -60; guard.HipR = -50; guard.KneeL = 100; guard.KneeR = 95; guard.Lift = -0.25f; guard.Spine = 20;
                return guard;
            case FighterClip.Jump:
                guard.HipL = -70; guard.HipR = -55; guard.KneeL = 110; guard.KneeR = 95;
                return guard;
            case FighterClip.Jab:
                float jab = MathF.Sin(MathF.Min(1, t * 6f) * MathF.PI);
                guard.ShoulderR = -40 - 50 * jab; guard.ElbowR = -100 + 95 * jab; guard.SpineTwist = -18 * jab;
                return guard;
            case FighterClip.Heavy:
                float hv = MathF.Sin(MathF.Min(1, t * 3.4f) * MathF.PI);
                guard.ShoulderL = -55 - 40 * hv; guard.ElbowL = -95 + 90 * hv; guard.SpineTwist = 30 * hv; guard.Spine = 6 + 14 * hv;
                return guard;
            case FighterClip.Kick:
                float k = MathF.Sin(MathF.Min(1, t * 4.5f) * MathF.PI);
                guard.HipR = 10 - 100 * k; guard.KneeR = 10 * (1 - k); guard.Spine = 6 - 14 * k;
                return guard;
            case FighterClip.Uppercut:
                float u = MathF.Sin(MathF.Min(1, t * 3.5f) * MathF.PI);
                guard.ShoulderR = -40 - 130 * u; guard.ElbowR = -100 + 60 * u; guard.Spine = -10 * u; guard.Lift = 0.08f * u;
                return guard;
            case FighterClip.Special:
                float sp = MathF.Min(1, t * 4f);
                guard.ShoulderL = -55 - 35 * sp; guard.ShoulderR = -40 - 50 * sp; guard.ElbowL = -95 + 95 * sp; guard.ElbowR = -100 + 100 * sp;
                return guard;
            case FighterClip.Grab:
                float g = MathF.Min(1, t * 5f);
                guard.ShoulderL = -80 * g - 20; guard.ShoulderR = -80 * g - 20; guard.ElbowL = -30; guard.ElbowR = -30;
                return guard;
            case FighterClip.Hit:
                guard.Spine = -22; guard.Head = -20; guard.ShoulderL = 20; guard.ShoulderR = 25; guard.ArmOutL = 30; guard.ArmOutR = 30;
                return guard;
            case FighterClip.Block:
                guard.ShoulderL = -95; guard.ShoulderR = -95; guard.ElbowL = -120; guard.ElbowR = -120; guard.Spine = 14; guard.Lift = -0.04f;
                return guard;
            case FighterClip.KO:
                return new Pose { ShoulderL = -160, ShoulderR = -160, ArmOutL = 30, ArmOutR = 30, Spine = -80, Head = -10, HipL = -80, HipR = -70, KneeL = 20, KneeR = 10, Lift = -0.75f };
            case FighterClip.Dizzy:
                float sway = MathF.Sin(time * 2.2f);
                guard.ShoulderL = 10; guard.ShoulderR = 10; guard.ElbowL = -10; guard.ElbowR = -10; guard.Spine = 18; guard.SpineTwist = sway * 15; guard.Head = 20 + sway * 10;
                return guard;
            case FighterClip.Victory:
                float bounce = MathF.Abs(MathF.Sin(time * 4f));
                return new Pose { ShoulderL = -170, ShoulderR = -170, ArmOutL = -15, ArmOutR = -15, ElbowL = -20, ElbowR = -20, Head = -15, Lift = bounce * 0.08f };
            default:
                return guard;
        }
    }

    protected override void Animate(FighterClip clip, float seconds, in FighterState f, float dt, float time)
    {
        CurrentClip = clip;
        var target = Target(clip, f, time);
        float blend = 1f - MathF.Exp(-dt * (clip is FighterClip.Jab or FighterClip.Kick or FighterClip.Hit ? 40f : 16f));
        _pose = Pose.Lerp(_pose, target, blend);

        float h = Character.Body.Height;
        _hips.Position = new Vector3(0, h * 0.47f + _pose.Lift * h * 0.6f, 0);
        _spine.EulerAngles = new Vector3(Deg(_pose.Spine), Deg(_pose.SpineTwist), 0);
        _head.EulerAngles = new Vector3(Deg(_pose.Head), 0, 0);
        _shoulderL.EulerAngles = new Vector3(Deg(_pose.ShoulderL), 0, Deg(_pose.ArmOutL));
        _shoulderR.EulerAngles = new Vector3(Deg(_pose.ShoulderR), 0, -Deg(_pose.ArmOutR));
        _elbowL.EulerAngles = new Vector3(Deg(_pose.ElbowL), 0, 0);
        _elbowR.EulerAngles = new Vector3(Deg(_pose.ElbowR), 0, 0);
        _hipL.EulerAngles = new Vector3(Deg(_pose.HipL), 0, 0);
        _hipR.EulerAngles = new Vector3(Deg(_pose.HipR), 0, 0);
        _kneeL.EulerAngles = new Vector3(Deg(_pose.KneeL), 0, 0);
        _kneeR.EulerAngles = new Vector3(Deg(_pose.KneeR), 0, 0);
        if (_tail is not null) _tail.EulerAngles = new Vector3(Deg(-20 + MathF.Sin(time * 3f) * 10), Deg(MathF.Sin(time * 2f) * 25), 0);
    }
}
