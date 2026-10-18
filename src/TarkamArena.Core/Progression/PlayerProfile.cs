using TarkamArena.Core.Combat;
using TarkamArena.Core.Creator;
using TarkamArena.Core.Localization;

namespace TarkamArena.Core.Progression;

/// <summary>Everything persisted about a local player. Serialised by TarkamArena.Persistence.</summary>
public sealed class PlayerProfile
{
    public string Name { get; set; } = "Pemain";
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public int Coins { get; set; } = 500;
    public int Rating { get; set; } = Progression.Rating.Initial;
    public Language Language { get; set; } = Language.Indonesian;

    public int MatchesPlayed { get; set; }
    public int Wins { get; set; }
    public int Parries { get; set; }
    public int CounterHits { get; set; }
    public int RageActivations { get; set; }
    public int AdrenalineActivations { get; set; }
    public int TeamCombos { get; set; }
    public int HazardHits { get; set; }
    public int StoryChapter { get; set; }
    public int TournamentWins { get; set; }
    public int SurvivalBest { get; set; }
    public string? ClanName { get; set; }

    public string SelectedBanner { get; set; } = "banner-0";
    public string SelectedPose { get; set; } = "pose-0";
    public string SelectedIntro { get; set; } = "intro-0";

    public HashSet<string> Unlocks { get; } = new(StringComparer.Ordinal);
    public HashSet<string> Achievements { get; } = new(StringComparer.Ordinal);
    public Dictionary<FinisherKind, int> Finishers { get; } = [];
    public Dictionary<string, int> CharacterWins { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> Reputation { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> EquippedCostumes { get; } = new(StringComparer.Ordinal);
    public List<CustomFighterSpec> CustomFighters { get; } = [];

    public static PlayerProfile CreateNew(string name = "Pemain")
    {
        var profile = new PlayerProfile { Name = name };
        foreach (var id in Unlockables.Defaults()) profile.Unlocks.Add(id);
        return profile;
    }

    public int FinisherCount(FinisherKind kind) => Finishers.TryGetValue(kind, out int n) ? n : 0;
    public int TotalFinishers => Finishers.Values.Sum();
    public int WinsWith(string characterId) => CharacterWins.TryGetValue(characterId, out int n) ? n : 0;
    public bool Owns(string unlockId) => Unlocks.Contains(unlockId);
    public bool HasCharacter(string characterId) => Unlocks.Contains(Unlockables.CharacterUnlockId(characterId));

    /// <summary>XP needed to go from <paramref name="level"/> to the next one.</summary>
    public static int XpForLevel(int level) => 400 + (level - 1) * 150;

    public bool TryBuy(Unlockable item)
    {
        if (Owns(item.Id) || Coins < item.Price || Level < item.RequiredLevel) return false;
        Coins -= item.Price;
        Unlocks.Add(item.Id);
        return true;
    }
}
