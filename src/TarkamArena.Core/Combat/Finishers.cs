using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Combat;

public enum FinisherKind : byte
{
    Fatality,
    Brutality,
    Friendship,
    Mercy,
    Legendary,
}

/// <summary>A finisher a fighter can enter during the "Habisi!" window after the deciding KO.</summary>
public sealed record FinisherDefinition(int Index, FinisherKind Kind, LocalizedText Name, InputToken[] Sequence)
{
    /// <summary>Crowd excitement (0-1000) needed to perform a Legendary finisher.</summary>
    public const int LegendaryCrowdRequirement = 800;

    public string SequenceGlyphs => string.Join(" ", Sequence.Select(InputTokens.ToGlyph));

    public string SequenceCode => new(Sequence.Select(InputTokens.ToChar).ToArray());
}

/// <summary>
/// Creates the nine finishers of a fighter: 3 Fatality, 2 Brutality, 2 Friendship, 1 Mercy, 1 Legendary.
/// Sequences are always four tokens long and come from pools that do not overlap between kinds, so
/// no finisher of a fighter can be the suffix of another.
/// </summary>
public static class FinisherFactory
{
    public static readonly FinisherKind[] Layout =
    [
        FinisherKind.Fatality, FinisherKind.Fatality, FinisherKind.Fatality,
        FinisherKind.Brutality, FinisherKind.Brutality,
        FinisherKind.Friendship, FinisherKind.Friendship,
        FinisherKind.Mercy,
        FinisherKind.Legendary,
    ];

    private static readonly string[] s_fatalityPool = ["DFDH", "BFDH", "FFBH", "DBFH", "UDFH", "BBFH"];
    private static readonly string[] s_brutalityPool = ["FBFL", "DDFL", "BFBL", "UFDL"];
    private static readonly string[] s_friendshipPool = ["FFFS", "BBBS", "DUDS", "UUFS"];
    private static readonly string[] s_mercyPool = ["DDDG", "BBDG"];
    private static readonly string[] s_legendaryPool = ["UUDH", "FDBH", "BUFH"];

    public static FinisherDefinition[] Build(CharacterDefinition character)
    {
        if (character.FinisherNames.Count != Layout.Length)
            throw new InvalidOperationException($"{character.Id} must define exactly {Layout.Length} finisher names.");

        uint rng = DeterministicRandom.Seed(DeterministicRandom.Hash(character.Id));
        var fatalities = Pick(s_fatalityPool, 3, ref rng);
        var brutalities = Pick(s_brutalityPool, 2, ref rng);
        var friendships = Pick(s_friendshipPool, 2, ref rng);
        var mercy = Pick(s_mercyPool, 1, ref rng);
        var legendary = Pick(s_legendaryPool, 1, ref rng);

        string[] codes = [.. fatalities, .. brutalities, .. friendships, .. mercy, .. legendary];
        var result = new FinisherDefinition[Layout.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = new FinisherDefinition(i, Layout[i], character.FinisherNames[i], [.. codes[i].Select(InputTokens.FromChar)]);
        return result;
    }

    private static string[] Pick(string[] pool, int count, ref uint rng)
    {
        var copy = (string[])pool.Clone();
        for (int i = copy.Length - 1; i > 0; i--)
        {
            int j = DeterministicRandom.Range(ref rng, i + 1);
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }
        return copy[..count];
    }

    public static LocalizedText KindName(FinisherKind kind) => kind switch
    {
        FinisherKind.Fatality => new("Fatality", "Fatality"),
        FinisherKind.Brutality => new("Brutality", "Brutality"),
        FinisherKind.Friendship => new("Friendship", "Persahabatan"),
        FinisherKind.Mercy => new("Mercy", "Ampunan"),
        _ => new("Legendary Finisher", "Finisher Legenda"),
    };
}
