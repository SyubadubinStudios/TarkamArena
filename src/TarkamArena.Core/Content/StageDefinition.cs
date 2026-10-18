using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Content;

public enum Weather : byte
{
    Clear,
    Cloudy,
    Rain,
    Storm,
    Fog,
    Ash,
    Night,
}

public enum TimeOfDay : byte
{
    Dawn,
    Day,
    Dusk,
    Night,
}

public enum MusicStyle : byte
{
    Gamelan,
    Angklung,
    DegungSunda,
    Talempong,
    Sasando,
    MetalHardcore,
    Rock,
    Edm,
    OrchestralHybrid,
    DangdutKoplo,
    Baleganjur,
    ReogGamelan,
    Punk,
    Horeg,
}

/// <summary>Background NPC roles. Each stage picks a handful.</summary>
public enum NpcKind : byte
{
    Trader,
    Shopper,
    Porter,
    VegetableSeller,
    Driver,
    Conductor,
    Passenger,
    Hawker,
    Ranger,
    Researcher,
    OrangUtan,
    Farmer,
    Buffalo,
    Politician,
    Journalist,
    Commuter,
    Officer,
    Tourist,
    Fisherman,
    Monk,
    Photographer,
    Kid,
    Chicken,
    Goat,
    Musician,
    Dancer,
    SoundCrew,
    Firefighter,
    Villager,
    Warok,
    Pecalang,
    Student,
    Judge,
    OjolDriver,
    Lawyer,
}

/// <summary>What an NPC is doing right now.</summary>
public enum NpcActivity : byte
{
    Walk,
    Trade,
    Chat,
    Cheer,
    Record,
    Flee,
    React,
    Idle,
}

/// <summary>Scripted stage happenings that can interrupt the background or the fight itself.</summary>
public enum ArenaEventKind : byte
{
    LooseChicken,
    PassingCart,
    RagingGoat,
    BusArrival,
    PassengerRush,
    FruitThrow,
    FallingTree,
    Downpour,
    Protest,
    TrainPass,
    BigWave,
    Eruption,
    Fireworks,
    Procession,
    TourBus,
    BassDrop,
    FireSpread,
    WaterBomb,
    WaterCannon,
    TearGas,
    MotorbikeSwarm,
    Blackout,
}

public readonly record struct StagePalette(uint Ground, uint Sky, uint Fog, uint Accent, uint Light);

public readonly record struct ArenaEventDefinition(ArenaEventKind Kind, LocalizedText Name, LocalizedText Description, bool AffectsFight);

public sealed class StageDefinition
{
    public required string Id { get; init; }
    public required LocalizedText Name { get; init; }
    public required string Region { get; init; }
    public required LocalizedText Description { get; init; }
    public required StagePalette Palette { get; init; }
    public required TimeOfDay Time { get; init; }
    public required Weather Weather { get; init; }
    public required MusicStyle Music { get; init; }
    public required IReadOnlyList<NpcKind> Npcs { get; init; }
    public required IReadOnlyList<ArenaEventDefinition> Events { get; init; }

    /// <summary>Half the playable width in metres. Fighters are clamped to [-HalfWidth, HalfWidth].</summary>
    public float HalfWidth { get; init; } = 7.5f;

    public int CrowdDensity { get; init; } = 60;

    /// <summary>Optional authored environment (Blender MCP export).</summary>
    public string ModelPath => $"models/stages/{Id}.glb";

    public string ThumbnailPath => $"generated/stages/{Id}.png";

    public override string ToString() => Name.En;
}
