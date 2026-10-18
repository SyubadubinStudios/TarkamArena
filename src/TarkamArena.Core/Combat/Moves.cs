using TarkamArena.Core.Content;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Combat;

public enum MoveId : byte
{
    None,
    Jab,
    Cross,
    Hook,
    Heavy,
    LowLight,
    Launcher,
    AirLight,
    AirHeavy,
    Grab,
    SpecialNeutral,
    SpecialForward,
    SpecialDown,
    TagStrike,
    RageBurst,
    Signature,
    Count,
}

[Flags]
public enum MoveFlags : ushort
{
    None = 0,
    /// <summary>Must be crouch-blocked.</summary>
    Low = 1 << 0,
    /// <summary>Must be stand-blocked.</summary>
    Overhead = 1 << 1,
    /// <summary>Cannot be blocked; beaten by being airborne or in hitstun.</summary>
    Unblockable = 1 << 2,
    /// <summary>Launches the victim into a juggle state.</summary>
    Launcher = 1 << 3,
    /// <summary>Spawns a projectile on the first active frame instead of using a hitbox.</summary>
    Projectile = 1 << 4,
    /// <summary>Only usable in the air.</summary>
    Air = 1 << 5,
    /// <summary>Victim is knocked down on hit.</summary>
    KnockDown = 1 << 6,
    /// <summary>Invulnerable during startup (reversal).</summary>
    StartupInvulnerable = 1 << 7,
    /// <summary>Moves the attacker forward during startup and active frames.</summary>
    Lunge = 1 << 8,
    /// <summary>Teleports the attacker behind the target on the first active frame.</summary>
    Teleport = 1 << 9,
    /// <summary>Spikes an airborne victim to the ground.</summary>
    Spike = 1 << 10,
    /// <summary>Applies poison damage over time.</summary>
    Poison = 1 << 11,
    /// <summary>Command grab: grabs on contact.</summary>
    CommandGrab = 1 << 12,
    /// <summary>Spawns the fighter's signature effect on the first active frame.</summary>
    Summon = 1 << 13,
}

/// <summary>Frame data for one move. All times are in 60 Hz frames.</summary>
public readonly record struct MoveData
{
    public MoveId Id { get; init; }
    public byte Startup { get; init; }
    public byte Active { get; init; }
    public byte Recovery { get; init; }
    public short Damage { get; init; }
    public short ChipDamage { get; init; }
    public byte Hitstun { get; init; }
    public byte Blockstun { get; init; }
    public byte Hitstop { get; init; }
    public Fixed Pushback { get; init; }
    public Fixed LaunchVelocity { get; init; }
    public Fixed LungeSpeed { get; init; }
    public FixedVector2 HitboxOffset { get; init; }
    public FixedVector2 HitboxSize { get; init; }
    public MoveFlags Flags { get; init; }
    public short MeterGain { get; init; }

    /// <summary>The move a Light press during the cancel window chains into (target combo).</summary>
    public MoveId ChainsInto { get; init; }

    /// <summary>Projectile horizontal speed in metres per frame.</summary>
    public Fixed ProjectileSpeed { get; init; }

    public int TotalFrames => Startup + Active + Recovery;
    public bool IsNone => Id == MoveId.None;
    public bool Has(MoveFlags flag) => (Flags & flag) != 0;
}

/// <summary>Builds a fighter's frame data from its archetype and stats.</summary>
public static class MoveSetFactory
{
    private static Fixed F(float v) => Fixed.FromFloat(v);
    private static FixedVector2 V(float x, float y) => FixedVector2.Of(x, y);

    public static MoveData[] Build(CharacterDefinition character)
    {
        var moves = new MoveData[(int)MoveId.Count];
        var s = character.Stats;
        var a = character.Archetype;
        float reach = character.Body.Height / 1.72f * (character.Body.Features.HasFlag(BodyFeature.LongArms) ? 1.2f : 1f);

        int Speed(int startup) => Math.Clamp(startup - (s.Speed - 5) / 2, 2, 60);
        short Dmg(int baseDamage) => (short)(baseDamage * (0.75f + s.Power * 0.05f));

        moves[(int)MoveId.Jab] = new()
        {
            Id = MoveId.Jab, Startup = (byte)Speed(5), Active = 3, Recovery = 8, Damage = Dmg(30), ChipDamage = 0,
            Hitstun = 14, Blockstun = 9, Hitstop = 6, Pushback = F(0.06f), HitboxOffset = V(0.65f * reach, 1.35f),
            HitboxSize = V(0.6f * reach, 0.35f), MeterGain = 25, ChainsInto = MoveId.Cross,
        };
        moves[(int)MoveId.Cross] = new()
        {
            Id = MoveId.Cross, Startup = (byte)Speed(7), Active = 3, Recovery = 10, Damage = Dmg(38),
            Hitstun = 16, Blockstun = 11, Hitstop = 7, Pushback = F(0.07f), HitboxOffset = V(0.75f * reach, 1.3f),
            HitboxSize = V(0.7f * reach, 0.4f), MeterGain = 30, ChainsInto = MoveId.Hook,
        };
        moves[(int)MoveId.Hook] = new()
        {
            Id = MoveId.Hook, Startup = (byte)Speed(10), Active = 4, Recovery = 16, Damage = Dmg(60),
            Hitstun = 24, Blockstun = 14, Hitstop = 10, Pushback = F(0.16f), HitboxOffset = V(0.75f * reach, 1.25f),
            HitboxSize = V(0.8f * reach, 0.5f), MeterGain = 45, Flags = MoveFlags.KnockDown,
        };
        moves[(int)MoveId.Heavy] = new()
        {
            Id = MoveId.Heavy, Startup = (byte)Speed(13), Active = 4, Recovery = 20, Damage = Dmg(85), ChipDamage = 8,
            Hitstun = 26, Blockstun = 16, Hitstop = 12, Pushback = F(0.14f), HitboxOffset = V(0.85f * reach, 1.2f),
            HitboxSize = V(0.9f * reach, 0.55f), MeterGain = 55, Flags = MoveFlags.Overhead,
        };
        moves[(int)MoveId.LowLight] = new()
        {
            Id = MoveId.LowLight, Startup = (byte)Speed(6), Active = 3, Recovery = 10, Damage = Dmg(28),
            Hitstun = 14, Blockstun = 9, Hitstop = 6, Pushback = F(0.06f), HitboxOffset = V(0.7f * reach, 0.25f),
            HitboxSize = V(0.75f * reach, 0.3f), MeterGain = 25, Flags = MoveFlags.Low,
        };
        moves[(int)MoveId.Launcher] = new()
        {
            Id = MoveId.Launcher, Startup = (byte)Speed(12), Active = 4, Recovery = 22, Damage = Dmg(70), ChipDamage = 6,
            Hitstun = 30, Blockstun = 15, Hitstop = 10, Pushback = F(0.02f), LaunchVelocity = F(0.30f),
            HitboxOffset = V(0.6f * reach, 1.0f), HitboxSize = V(0.7f * reach, 1.2f), MeterGain = 50, Flags = MoveFlags.Launcher,
        };
        moves[(int)MoveId.AirLight] = new()
        {
            Id = MoveId.AirLight, Startup = (byte)Speed(5), Active = 5, Recovery = 8, Damage = Dmg(32),
            Hitstun = 16, Blockstun = 9, Hitstop = 6, Pushback = F(0.04f), LaunchVelocity = F(0.10f),
            HitboxOffset = V(0.6f * reach, 0.9f), HitboxSize = V(0.7f * reach, 0.6f), MeterGain = 25, Flags = MoveFlags.Air | MoveFlags.Overhead,
        };
        moves[(int)MoveId.AirHeavy] = new()
        {
            Id = MoveId.AirHeavy, Startup = (byte)Speed(9), Active = 5, Recovery = 14, Damage = Dmg(70), ChipDamage = 6,
            Hitstun = 22, Blockstun = 13, Hitstop = 11, Pushback = F(0.10f),
            HitboxOffset = V(0.6f * reach, 0.6f), HitboxSize = V(0.9f * reach, 0.7f), MeterGain = 45,
            Flags = MoveFlags.Air | MoveFlags.Overhead | MoveFlags.Spike,
        };
        moves[(int)MoveId.Grab] = new()
        {
            Id = MoveId.Grab, Startup = 6, Active = 3, Recovery = 24, Damage = Dmg(a == Archetype.Grappler ? 120 : 95),
            Hitstun = 40, Hitstop = 14, Pushback = F(0.25f), HitboxOffset = V(0.5f, 1.0f), HitboxSize = V(0.55f, 1.0f),
            MeterGain = 60, Flags = MoveFlags.Unblockable | MoveFlags.KnockDown,
        };
        moves[(int)MoveId.TagStrike] = new()
        {
            Id = MoveId.TagStrike, Startup = 8, Active = 6, Recovery = 14, Damage = Dmg(55),
            Hitstun = 30, Blockstun = 14, Hitstop = 10, Pushback = F(0.05f), LaunchVelocity = F(0.26f), LungeSpeed = F(0.14f),
            HitboxOffset = V(0.7f, 1.0f), HitboxSize = V(1.0f, 1.2f), MeterGain = 40, Flags = MoveFlags.Launcher | MoveFlags.Lunge,
        };
        // Signature move ("Jurus Pamungkas"): a short invulnerable wind-up, then the effect spawns.
        moves[(int)MoveId.Signature] = new()
        {
            Id = MoveId.Signature, Startup = 16, Active = 2, Recovery = 26,
            Flags = MoveFlags.Summon | MoveFlags.StartupInvulnerable,
        };
        moves[(int)MoveId.RageBurst] = new()
        {
            Id = MoveId.RageBurst, Startup = 4, Active = 8, Recovery = 30, Damage = Dmg(40),
            Hitstun = 34, Hitstop = 16, Pushback = F(0.35f), HitboxOffset = V(0f, 1.0f), HitboxSize = V(3.2f, 2.4f),
            Flags = MoveFlags.Unblockable | MoveFlags.KnockDown | MoveFlags.StartupInvulnerable,
        };

        BuildSpecials(moves, a, reach, Speed, Dmg);
        return moves;
    }

    private static void BuildSpecials(MoveData[] moves, Archetype a, float reach, Func<int, int> speed, Func<int, short> dmg)
    {
        // Neutral special: a projectile for most, a command grab for grapplers, a flurry for beasts.
        moves[(int)MoveId.SpecialNeutral] = a switch
        {
            Archetype.Grappler => new()
            {
                Id = MoveId.SpecialNeutral, Startup = 9, Active = 4, Recovery = 30, Damage = dmg(150),
                Hitstun = 45, Hitstop = 18, Pushback = F(0.3f), HitboxOffset = V(0.6f, 1.0f), HitboxSize = V(0.75f, 1.2f),
                MeterGain = 80, Flags = MoveFlags.Unblockable | MoveFlags.CommandGrab | MoveFlags.KnockDown,
            },
            Archetype.Beast => new()
            {
                Id = MoveId.SpecialNeutral, Startup = (byte)speed(8), Active = 10, Recovery = 16, Damage = dmg(75), ChipDamage = 10,
                Hitstun = 24, Blockstun = 14, Hitstop = 8, Pushback = F(0.1f), LungeSpeed = F(0.05f),
                HitboxOffset = V(0.7f * reach, 1.0f), HitboxSize = V(0.9f * reach, 0.9f), MeterGain = 60,
                Flags = MoveFlags.Lunge | (a == Archetype.Beast ? MoveFlags.Poison : MoveFlags.None),
            },
            _ => new()
            {
                Id = MoveId.SpecialNeutral, Startup = (byte)speed(14), Active = 1, Recovery = 22, Damage = dmg(55), ChipDamage = 8,
                Hitstun = 20, Blockstun = 14, Hitstop = 8, Pushback = F(0.1f), HitboxOffset = V(0.7f, 1.2f), HitboxSize = V(0.5f, 0.5f),
                MeterGain = 50, Flags = MoveFlags.Projectile, ProjectileSpeed = a == Archetype.Mystic ? F(0.11f) : F(0.15f),
            },
        };

        // Forward special: a lunge, or a teleport for mystics.
        moves[(int)MoveId.SpecialForward] = a == Archetype.Mystic
            ? new()
            {
                Id = MoveId.SpecialForward, Startup = 16, Active = 4, Recovery = 18, Damage = dmg(60),
                Hitstun = 22, Blockstun = 12, Hitstop = 9, Pushback = F(0.12f), HitboxOffset = V(0.6f, 1.2f), HitboxSize = V(0.9f, 1.2f),
                MeterGain = 50, Flags = MoveFlags.Teleport | MoveFlags.Overhead,
            }
            : new()
            {
                Id = MoveId.SpecialForward, Startup = (byte)speed(11), Active = 8, Recovery = 20, Damage = dmg(80), ChipDamage = 10,
                Hitstun = 26, Blockstun = 16, Hitstop = 11, Pushback = F(0.18f), LungeSpeed = F(a == Archetype.Grappler ? 0.16f : 0.22f),
                HitboxOffset = V(0.7f * reach, 1.1f), HitboxSize = V(0.9f * reach, 1.0f), MeterGain = 55,
                Flags = MoveFlags.Lunge | MoveFlags.KnockDown,
            };

        // Down special: a reversal uppercut for most, a low shockwave for mystics and grapplers.
        moves[(int)MoveId.SpecialDown] = a is Archetype.Mystic or Archetype.Grappler
            ? new()
            {
                Id = MoveId.SpecialDown, Startup = (byte)speed(15), Active = 6, Recovery = 24, Damage = dmg(70), ChipDamage = 10,
                Hitstun = 30, Blockstun = 18, Hitstop = 12, Pushback = F(0.2f), HitboxOffset = V(1.1f * reach, 0.2f), HitboxSize = V(2.0f * reach, 0.4f),
                MeterGain = 55, Flags = MoveFlags.Low | MoveFlags.KnockDown,
            }
            : new()
            {
                Id = MoveId.SpecialDown, Startup = 5, Active = 8, Recovery = 30, Damage = dmg(90), ChipDamage = 10,
                Hitstun = 34, Blockstun = 20, Hitstop = 12, Pushback = F(0.04f), LaunchVelocity = F(0.36f),
                HitboxOffset = V(0.5f * reach, 1.6f), HitboxSize = V(0.8f * reach, 1.8f), MeterGain = 60,
                Flags = MoveFlags.Launcher | MoveFlags.StartupInvulnerable,
            };
    }
}
