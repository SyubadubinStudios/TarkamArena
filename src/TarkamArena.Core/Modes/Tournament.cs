using TarkamArena.Core.Content;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Modes;

public sealed record BracketMatch(int Round, int Index, CharacterDefinition? Left, CharacterDefinition? Right)
{
    public CharacterDefinition? Winner { get; set; }
    public bool IsPlayerMatch { get; init; }
    public bool IsDecided => Winner is not null;
}

/// <summary>
/// Eight-fighter single-elimination cup. The player is always entrant 0; CPU-vs-CPU matches are
/// resolved from fighter stats with a seeded roll so a given seed always gives the same bracket.
/// </summary>
public sealed class Tournament
{
    public const int Entrants = 8;
    public const int Rounds = 3;

    private readonly List<BracketMatch>[] _rounds = [[], [], []];
    private uint _rng;

    public CharacterDefinition Player { get; }
    public IReadOnlyList<CharacterDefinition> Field { get; }
    public IReadOnlyList<IReadOnlyList<BracketMatch>> Bracket => _rounds;
    public int CurrentRound { get; private set; }
    public bool PlayerEliminated { get; private set; }
    public CharacterDefinition? Champion { get; private set; }
    public bool IsFinished => Champion is not null;

    public Tournament(CharacterDefinition player, IEnumerable<CharacterDefinition> pool, uint seed)
    {
        Player = player;
        _rng = DeterministicRandom.Seed(seed);
        var candidates = pool.Where(c => c.Id != player.Id && !c.IsSecret && c.Dlc == DlcPack.None).ToList();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = DeterministicRandom.Range(ref _rng, i + 1);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }
        if (candidates.Count < Entrants - 1) throw new ArgumentException("Not enough fighters for a tournament.", nameof(pool));

        Field = [player, .. candidates.Take(Entrants - 1)];
        for (int i = 0; i < Entrants / 2; i++)
            _rounds[0].Add(new BracketMatch(0, i, Field[i * 2], Field[i * 2 + 1]) { IsPlayerMatch = i == 0 });
        ResolveCpuMatches();
    }

    /// <summary>The match the player fights next, or null when the cup is over for them.</summary>
    public BracketMatch? NextPlayerMatch =>
        PlayerEliminated || IsFinished ? null : _rounds[CurrentRound].FirstOrDefault(m => m.IsPlayerMatch && !m.IsDecided);

    public void ReportPlayerResult(bool playerWon)
    {
        var match = NextPlayerMatch ?? throw new InvalidOperationException("No player match is pending.");
        match.Winner = playerWon ? Player : (match.Left == Player ? match.Right : match.Left);
        if (!playerWon) PlayerEliminated = true;

        // Finish the rest of the bracket on the CPU's behalf.
        ResolveCpuMatches();
        while (_rounds[CurrentRound].All(m => m.IsDecided))
        {
            if (CurrentRound == Rounds - 1)
            {
                Champion = _rounds[CurrentRound][0].Winner;
                return;
            }
            Advance();
            ResolveCpuMatches();
        }
    }

    private void Advance()
    {
        var finished = _rounds[CurrentRound];
        CurrentRound++;
        for (int i = 0; i < finished.Count / 2; i++)
        {
            var left = finished[i * 2].Winner;
            var right = finished[i * 2 + 1].Winner;
            _rounds[CurrentRound].Add(new BracketMatch(CurrentRound, i, left, right)
            {
                IsPlayerMatch = !PlayerEliminated && (left == Player || right == Player),
            });
        }
    }

    private void ResolveCpuMatches()
    {
        foreach (var m in _rounds[CurrentRound])
        {
            if (m.IsDecided || (m.IsPlayerMatch && !PlayerEliminated)) continue;
            m.Winner = SimulateWinner(m.Left!, m.Right!);
        }
    }

    private CharacterDefinition SimulateWinner(CharacterDefinition a, CharacterDefinition b)
    {
        int scoreA = a.Stats.Total + DeterministicRandom.Range(ref _rng, 12);
        int scoreB = b.Stats.Total + DeterministicRandom.Range(ref _rng, 12);
        return scoreA >= scoreB ? a : b;
    }
}

/// <summary>Endless waves of random opponents. Health carries over between waves.</summary>
public sealed class SurvivalRun
{
    private uint _rng;

    public CharacterDefinition Player { get; }
    public int Wave { get; private set; } = 1;
    public int HealthPercent { get; private set; } = 100;
    public bool IsOver { get; private set; }
    public CharacterDefinition CurrentOpponent { get; private set; }

    public SurvivalRun(CharacterDefinition player, uint seed)
    {
        Player = player;
        _rng = DeterministicRandom.Seed(seed);
        CurrentOpponent = PickOpponent();
    }

    /// <summary>CPU difficulty ramps with the wave number.</summary>
    public AI.CpuDifficulty Difficulty => Wave switch
    {
        < 3 => AI.CpuDifficulty.Easy,
        < 6 => AI.CpuDifficulty.Normal,
        < 10 => AI.CpuDifficulty.Hard,
        _ => AI.CpuDifficulty.Legend,
    };

    public void ReportResult(bool won, int remainingHealthPercent)
    {
        if (IsOver) throw new InvalidOperationException("The run is already over.");
        if (!won)
        {
            IsOver = true;
            return;
        }
        Wave++;
        HealthPercent = Math.Clamp(remainingHealthPercent + 20, 1, 100);
        CurrentOpponent = PickOpponent();
    }

    private CharacterDefinition PickOpponent()
    {
        var pool = Roster.BaseGame.Where(c => c.Id != Player.Id && (!c.IsSecret || Wave >= 8)).ToArray();
        return pool[DeterministicRandom.Range(ref _rng, pool.Length)];
    }
}
