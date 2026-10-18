namespace TarkamArena.Core.Localization;

public enum Language : byte
{
    English,
    Indonesian,
}

/// <summary>A piece of player-facing text in both supported languages.</summary>
public readonly record struct LocalizedText(string En, string Id)
{
    public string Get(Language language) => language == Language.Indonesian ? Id : En;

    public override string ToString() => Get(Loc.Current);

    public static implicit operator LocalizedText((string En, string Id) pair) => new(pair.En, pair.Id);
}
