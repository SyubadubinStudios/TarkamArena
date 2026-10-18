using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;

namespace TarkamArena.Core.Progression;

/// <summary>What happened in a finished match, from the local player's point of view.</summary>
public sealed record MatchSummary
{
    public required GameMode Mode { get; init; }
    public required string CharacterId { get; init; }
    public required bool Won { get; init; }
    public int MaxCombo { get; init; }
    public int Parries { get; init; }
    public int CounterHits { get; init; }
    public int RageActivations { get; init; }
    public int AdrenalineActivations { get; init; }
    public int TeamCombos { get; init; }
    public int HazardHits { get; init; }
    public int CrowdPeak { get; init; }
    public bool PerfectRound { get; init; }
    public FinisherKind? Finisher { get; init; }
    public int DamageDealt { get; init; }
}

/// <summary>
/// Collects match statistics for one player slot by reading the simulation's event stream.
/// Call <see cref="Observe"/> once per confirmed frame.
/// </summary>
public sealed class MatchStatsTracker
{
    private readonly int _slot;
    private readonly int _team;
    private int _maxCombo, _parries, _counters, _rage, _adrenaline, _teamCombos, _hazards, _crowdPeak, _damage;
    private bool _tookDamageThisRound;
    private bool _perfect;
    private FinisherKind? _finisher;

    public MatchStatsTracker(int slot, int team)
    {
        _slot = slot;
        _team = team;
    }

    public void Observe(in MatchState state, MatchContext context)
    {
        _crowdPeak = Math.Max(_crowdPeak, state.Crowd);
        foreach (ref readonly var e in state.CurrentEvents)
        {
            switch (e.Kind)
            {
                case MatchEventKind.RoundStart:
                    _tookDamageThisRound = false;
                    break;
                case MatchEventKind.Hit or MatchEventKind.CounterHit:
                    if (e.Slot == _slot)
                    {
                        _damage += e.Value;
                        if (e.Kind == MatchEventKind.CounterHit) _counters++;
                        _maxCombo = Math.Max(_maxCombo, state.Fighters[e.Target].ComboHits);
                    }
                    if (e.Target == _slot) _tookDamageThisRound = true;
                    break;
                case MatchEventKind.HazardHit:
                    if (e.Target == _slot) { _hazards++; _tookDamageThisRound = true; }
                    break;
                case MatchEventKind.Parry when e.Slot == _slot: _parries++; break;
                case MatchEventKind.RageActivated when e.Slot == _slot: _rage++; break;
                case MatchEventKind.AdrenalineActivated when e.Slot == _slot: _adrenaline++; break;
                case MatchEventKind.TeamCombo when state.Fighters[e.Slot].Team == _team: _teamCombos++; break;
                case MatchEventKind.RoundWin:
                    if (e.Value == _team && !_tookDamageThisRound) _perfect = true;
                    break;
                case MatchEventKind.FinisherStart when e.Slot == _slot:
                    _finisher = context.Finishers[_slot][e.Value].Kind;
                    break;
                case MatchEventKind.Mercy when e.Slot == _slot:
                    _finisher = FinisherKind.Mercy;
                    break;
            }
        }
    }

    public MatchSummary Build(in MatchState state, MatchContext context) => new()
    {
        Mode = context.Rules.Mode,
        CharacterId = context.Characters[_slot].Id,
        Won = state.MatchWinnerTeam == _team,
        MaxCombo = _maxCombo,
        Parries = _parries,
        CounterHits = _counters,
        RageActivations = _rage,
        AdrenalineActivations = _adrenaline,
        TeamCombos = _teamCombos,
        HazardHits = _hazards,
        CrowdPeak = _crowdPeak,
        PerfectRound = _perfect,
        Finisher = _finisher,
        DamageDealt = _damage,
    };
}

public sealed record RewardSummary(
    int Xp,
    int Coins,
    int ReputationGained,
    IReadOnlyList<int> LevelsReached,
    IReadOnlyList<Achievement> NewAchievements,
    IReadOnlyList<Unlockable> NewUnlocks);

/// <summary>Turns a match summary into XP, coins, reputation, achievements and unlocks.</summary>
public static class ProgressionService
{
    public static RewardSummary Apply(PlayerProfile profile, MatchSummary match)
    {
        profile.MatchesPlayed++;
        profile.Parries += match.Parries;
        profile.CounterHits += match.CounterHits;
        profile.RageActivations += match.RageActivations;
        profile.AdrenalineActivations += match.AdrenalineActivations;
        profile.TeamCombos += match.TeamCombos;
        profile.HazardHits += match.HazardHits;
        if (match.Won)
        {
            profile.Wins++;
            profile.CharacterWins[match.CharacterId] = profile.WinsWith(match.CharacterId) + 1;
        }
        if (match.Finisher is { } kind) profile.Finishers[kind] = profile.FinisherCount(kind) + 1;

        int xp = 60 + (match.Won ? 90 : 0) + match.MaxCombo * 6 + match.Parries * 10 + (match.PerfectRound ? 80 : 0) + (match.Finisher is null ? 0 : 60);
        int coins = 25 + (match.Won ? 75 : 10) + (match.Finisher is null ? 0 : 40);
        int reputation = match.CrowdPeak / 20 + (match.Finisher is null ? 0 : 25);
        profile.Reputation[match.CharacterId] = (profile.Reputation.TryGetValue(match.CharacterId, out int r) ? r : 0) + reputation;

        var levels = AddXp(profile, xp);
        profile.Coins += coins;
        return Finish(profile, match, xp, coins, reputation, levels);
    }

    /// <summary>Re-checks achievements and secret unlocks without a match (after story, tournament, creator…).</summary>
    public static RewardSummary Refresh(PlayerProfile profile) => Finish(profile, null, 0, 0, 0, []);

    private static List<int> AddXp(PlayerProfile profile, int xp)
    {
        var levels = new List<int>();
        profile.Xp += xp;
        while (profile.Xp >= PlayerProfile.XpForLevel(profile.Level))
        {
            profile.Xp -= PlayerProfile.XpForLevel(profile.Level);
            profile.Level++;
            profile.Coins += 100;
            levels.Add(profile.Level);
        }
        return levels;
    }

    private static RewardSummary Finish(PlayerProfile profile, MatchSummary? match, int xp, int coins, int reputation, List<int> levels)
    {
        var newAchievements = new List<Achievement>();
        foreach (var a in Achievements.All)
        {
            if (profile.Achievements.Contains(a.Id) || !a.IsEarned(profile, match)) continue;
            profile.Achievements.Add(a.Id);
            newAchievements.Add(a);
        }

        var newUnlocks = new List<Unlockable>();
        foreach (var c in Roster.All.Where(c => c.IsSecret))
        {
            string id = Unlockables.CharacterUnlockId(c.Id);
            if (profile.Owns(id) || !SecretUnlocked(profile, c.Id)) continue;
            profile.Unlocks.Add(id);
            newUnlocks.Add(Unlockables.Find(id)!);
        }
        return new RewardSummary(xp, coins, reputation, levels, newAchievements, newUnlocks);
    }

    /// <summary>Conditions shown in <see cref="CharacterDefinition.UnlockHint"/>.</summary>
    public static bool SecretUnlocked(PlayerProfile p, string characterId) => characterId switch
    {
        "nyi-roro-kidul" => p.StoryChapter >= 9,
        "gatotkaca" => p.TournamentWins >= 1,
        "si-pitung" => p.SurvivalBest >= 10,
        "sangkuriang" => p.TotalFinishers >= 25,
        "jaka-tarub" => p.Achievements.Count >= 30,
        _ => false,
    };
}

/// <summary>Elo rating for ranked play, with Indonesian administrative tiers as ranks.</summary>
public static class Rating
{
    public const int Initial = 1000;
    public const int K = 32;

    public static double Expected(int rating, int opponent) => 1.0 / (1.0 + Math.Pow(10, (opponent - rating) / 400.0));

    public static (int Winner, int Loser) Update(int winner, int loser)
    {
        double e = Expected(winner, loser);
        int delta = (int)Math.Round(K * (1 - e));
        delta = Math.Max(1, delta);
        return (winner + delta, Math.Max(100, loser - delta));
    }

    public static LocalizedText Tier(int rating) => rating switch
    {
        < 900 => new("Kampung", "Kampung"),
        < 1100 => new("District", "Kecamatan"),
        < 1300 => new("Regency", "Kabupaten"),
        < 1500 => new("Province", "Provinsi"),
        < 1750 => new("National", "Nasional"),
        _ => new("Legend", "Legenda"),
    };
}
