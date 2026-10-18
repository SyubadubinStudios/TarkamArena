using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Match;

public enum GameMode : byte
{
    Versus,
    TwoVsTwo,
    TagBattle,
    FreeForAll,
    Survival,
    Story,
    Tournament,
    OnlineRanked,
    ClanWar,
    Custom,
    Training,
}

public static class GameModes
{
    public static LocalizedText Name(GameMode mode) => mode switch
    {
        GameMode.Versus => new("1 vs 1", "1 lawan 1"),
        GameMode.TwoVsTwo => new("2 vs 2", "2 lawan 2"),
        GameMode.TagBattle => new("Tag Battle", "Tag Battle"),
        GameMode.FreeForAll => new("Free For All", "Bebas Semua"),
        GameMode.Survival => new("Survival", "Survival"),
        GameMode.Story => new("Story Campaign", "Mode Cerita"),
        GameMode.Tournament => new("Tournament", "Turnamen"),
        GameMode.OnlineRanked => new("Online Ranked", "Ranked Online"),
        GameMode.ClanWar => new("Clan Wars", "Perang Klan"),
        GameMode.Custom => new("Custom Match", "Pertandingan Kustom"),
        _ => new("Training", "Latihan"),
    };

    /// <summary>How many fighter slots a mode uses.</summary>
    public static int SlotCount(GameMode mode, int ffaPlayers = 4) => mode switch
    {
        GameMode.TwoVsTwo or GameMode.TagBattle => 4,
        GameMode.FreeForAll => Math.Clamp(ffaPlayers, 3, 4),
        _ => 2,
    };

    /// <summary>Team of a slot. Free-for-all puts everyone on their own team.</summary>
    public static byte TeamOf(GameMode mode, int slot) => mode switch
    {
        GameMode.TwoVsTwo or GameMode.TagBattle => (byte)(slot / 2),
        GameMode.FreeForAll => (byte)slot,
        _ => (byte)slot,
    };

    /// <summary>Tag battle starts with the second fighter of each team on the bench.</summary>
    public static bool StartsOnField(GameMode mode, int slot) => mode != GameMode.TagBattle || slot % 2 == 0;
}

/// <summary>Rules that stay fixed for a whole match. Part of the match handshake in online play.</summary>
public sealed record MatchRules
{
    public GameMode Mode { get; init; } = GameMode.Versus;
    public int RoundsToWin { get; init; } = 2;
    public int RoundSeconds { get; init; } = 99;
    public int DamagePercent { get; init; } = 100;
    public bool FinishersEnabled { get; init; } = true;
    public bool ArenaEventsEnabled { get; init; } = true;
    public bool CrowdEnabled { get; init; } = true;

    /// <summary>When false the timer never runs out (training).</summary>
    public bool TimerEnabled { get; init; } = true;

    public static MatchRules For(GameMode mode) => mode switch
    {
        GameMode.Training => new() { Mode = mode, TimerEnabled = false, FinishersEnabled = false, ArenaEventsEnabled = false, RoundsToWin = 99 },
        GameMode.Survival => new() { Mode = mode, RoundsToWin = 1 },
        GameMode.FreeForAll => new() { Mode = mode, RoundsToWin = 2, FinishersEnabled = false },
        GameMode.OnlineRanked => new() { Mode = mode, ArenaEventsEnabled = false },
        _ => new() { Mode = mode },
    };
}

public enum MatchPhase : byte
{
    Intro,
    Fight,
    RoundOver,
    FinisherWindow,
    FinisherPerforming,
    MatchOver,
}

public enum FighterStatus : byte
{
    Idle,
    Walk,
    Crouch,
    Airborne,
    Attack,
    Hitstun,
    Blockstun,
    Juggle,
    Knockdown,
    GetUp,
    Dodge,
    Parried,
    Benched,
    KO,
    Dizzy,
    Victory,
    Finishing,
}

public enum MatchEventKind : byte
{
    RoundStart,
    Fight,
    Hit,
    Block,
    Parry,
    CounterHit,
    Launch,
    Knockdown,
    Tech,
    Dodge,
    ProjectileSpawn,
    ProjectileClash,
    RageActivated,
    AdrenalineActivated,
    TagSwitch,
    TeamCombo,
    Combo,
    KO,
    TimeUp,
    RoundWin,
    FinisherPrompt,
    FinisherStart,
    Mercy,
    MatchWin,
    ArenaEvent,
    HazardHit,
    Whiff,
    Jump,
    Land,
}

/// <summary>Something presentation should react to (sound, particles, camera). Value meaning depends on the kind.</summary>
public struct MatchEvent
{
    public MatchEventKind Kind;
    public sbyte Slot;
    public sbyte Target;
    public short Value;
    public FixedVector2 Position;
    public int Frame;
}

/// <summary>A thrown object or energy blast.</summary>
public struct Projectile
{
    public bool Active;
    public sbyte Owner;
    public byte Team;
    public FixedVector2 Position;
    public Fixed VelocityX;
    public short Life;
    public short Damage;
    public short Chip;
    public byte Hitstun;
    public byte Blockstun;
    public FixedVector2 Size;
}

/// <summary>A stage hazard such as a runaway cart or a falling tree.</summary>
public struct Hazard
{
    public bool Active;
    public byte EventIndex;
    public short Warning;
    public short Life;
    public FixedVector2 Position;
    public Fixed VelocityX;
    public FixedVector2 Size;
    public short Damage;
    public bool HitMask0;
    public bool HitMask1;
    public bool HitMask2;
    public bool HitMask3;

    public readonly bool HasHit(int slot) => slot switch { 0 => HitMask0, 1 => HitMask1, 2 => HitMask2, _ => HitMask3 };

    public void MarkHit(int slot)
    {
        switch (slot)
        {
            case 0: HitMask0 = true; break;
            case 1: HitMask1 = true; break;
            case 2: HitMask2 = true; break;
            default: HitMask3 = true; break;
        }
    }
}
