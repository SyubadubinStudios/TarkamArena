using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Content;

public enum CharacterCategory : byte
{
    Profession,
    Mystic,
    Wildlife,
    Secret,
    Custom,
    /// <summary>City life and satire: officials, buzzers, students, drivers, performers.</summary>
    Society,
}

/// <summary>Broad fighting style. It shapes frame data, reach and which specials a fighter gets.</summary>
public enum Archetype : byte
{
    /// <summary>Balanced all-rounder.</summary>
    Brawler,
    /// <summary>Fast pokes and long combos, low damage per hit.</summary>
    Striker,
    /// <summary>Slow, heavy, strong grabs.</summary>
    Grappler,
    /// <summary>Floaty, ranged specials, teleports.</summary>
    Mystic,
    /// <summary>Animal fighter: low stance, lunges, high mobility.</summary>
    Beast,
    /// <summary>Boss-tier legend with strong everything.</summary>
    Legend,
}

/// <summary>Silhouette feature the procedural model builder adds on top of the base body.</summary>
[Flags]
public enum BodyFeature : uint
{
    None = 0,
    Hat = 1 << 0,
    Helmet = 1 << 1,
    LongHair = 1 << 2,
    Shroud = 1 << 3,
    Tail = 1 << 4,
    Wings = 1 << 5,
    Horns = 1 << 6,
    Floating = 1 << 7,
    Bound = 1 << 8,
    Crown = 1 << 9,
    Cape = 1 << 10,
    Snout = 1 << 11,
    Uniform = 1 << 12,
    Apron = 1 << 13,
    Headscarf = 1 << 14,
    Beak = 1 << 15,
    Fur = 1 << 16,
    Glasses = 1 << 17,
    Cart = 1 << 18,
    Scales = 1 << 19,
    Glow = 1 << 20,
    LongArms = 1 << 21,
    Quadruped = 1 << 22,
    Mask = 1 << 23,
    Weapon = 1 << 24,
}

public enum DlcPack : byte
{
    None,
    HorrorNusantara,
    JawaraNusantara,
    FutureIndonesia,
}

/// <summary>Attribute ratings on a 1-10 scale.</summary>
public readonly record struct CharacterStats(byte Power, byte Speed, byte Defense, byte Technique)
{
    public int Total => Power + Speed + Defense + Technique;
}

/// <summary>Body proportions and colours for the procedural model (used when no authored glTF exists).</summary>
public readonly record struct BodyShape(
    float Height,
    float Width,
    float HeadScale,
    uint PrimaryColor,
    uint SecondaryColor,
    uint SkinColor,
    BodyFeature Features,
    uint AccentColor = 0xF6C21B);

/// <summary>The three special moves every fighter owns.</summary>
public readonly record struct SpecialMoveNames(LocalizedText Neutral, LocalizedText Forward, LocalizedText Down);

/// <summary>
/// Static, immutable description of one playable fighter. Simulation-relevant numbers are turned
/// into frame data by <see cref="Combat.MoveSetFactory"/>.
/// </summary>
public sealed class CharacterDefinition
{
    public required string Id { get; init; }
    public required LocalizedText Name { get; init; }
    public required LocalizedText Title { get; init; }
    public required CharacterCategory Category { get; init; }
    public required Archetype Archetype { get; init; }
    public required string Origin { get; init; }
    public required LocalizedText Bio { get; init; }
    public required CharacterStats Stats { get; init; }
    public required BodyShape Body { get; init; }
    public required SpecialMoveNames Specials { get; init; }

    /// <summary>Exactly nine names in the order: 3 fatality, 2 brutality, 2 friendship, 1 mercy, 1 legendary.</summary>
    public required IReadOnlyList<LocalizedText> FinisherNames { get; init; }

    public LocalizedText Catchphrase { get; init; }
    public bool IsSecret { get; init; }
    public LocalizedText UnlockHint { get; init; }
    public DlcPack Dlc { get; init; }

    /// <summary>Base health before the defense modifier is applied.</summary>
    public int BaseHealth => 1000 + (Stats.Defense - 5) * 40 + (Archetype == Archetype.Legend ? 100 : 0);

    /// <summary>Optional authored model, relative to the assets folder (Rodin MCP / Blender MCP export).</summary>
    public string ModelPath => $"models/characters/{Id}.glb";

    public string PortraitPath => $"generated/portraits/{Id}.png";

    public override string ToString() => Name.En;
}
