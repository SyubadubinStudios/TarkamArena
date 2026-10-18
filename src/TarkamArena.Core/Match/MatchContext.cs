using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Mathematics;

namespace TarkamArena.Core.Match;

/// <summary>Who fights in which slot.</summary>
public sealed record FighterEntry(CharacterDefinition Character, int StartingHealthPercent = 100);

/// <summary>Everything needed to start a match. Two online peers must agree on it exactly.</summary>
public sealed record MatchSetup(MatchRules Rules, StageDefinition Stage, IReadOnlyList<FighterEntry> Fighters, uint Seed = 1);

/// <summary>
/// Immutable per-match lookup data (frame data, finishers, stage size). Kept out of
/// <see cref="MatchState"/> so snapshots stay small and unmanaged.
/// </summary>
public sealed class MatchContext
{
    public MatchSetup Setup { get; }
    public MatchRules Rules => Setup.Rules;
    public StageDefinition Stage => Setup.Stage;
    public CharacterDefinition[] Characters { get; }
    public MoveData[][] Moves { get; }
    public FinisherDefinition[][] Finishers { get; }
    public Fixed HalfWidth { get; }
    public Fixed[] HurtHeight { get; }
    public Fixed[] HurtWidth { get; }
    public Fixed[] WalkSpeed { get; }

    public MatchContext(MatchSetup setup)
    {
        int count = setup.Fighters.Count;
        if (count < 2 || count > MatchState.MaxFighters)
            throw new ArgumentException($"A match needs 2 to {MatchState.MaxFighters} fighters.", nameof(setup));

        Setup = setup;
        Characters = [.. setup.Fighters.Select(f => f.Character)];
        Moves = [.. Characters.Select(MoveSetFactory.Build)];
        Finishers = [.. Characters.Select(FinisherFactory.Build)];
        HalfWidth = Fixed.FromFloat(setup.Stage.HalfWidth);
        HurtHeight = [.. Characters.Select(c => Fixed.FromFloat(Math.Clamp(c.Body.Height, 0.9f, 2.4f)))];
        HurtWidth = [.. Characters.Select(c => Fixed.FromFloat(Math.Clamp(0.45f * c.Body.Width + 0.15f, 0.45f, 0.9f)))];
        WalkSpeed = [.. Characters.Select(c => Fixed.FromFloat(0.040f + c.Stats.Speed * 0.0035f))];
    }

    public ref readonly MoveData Move(int slot, MoveId id) => ref Moves[slot][(int)id];
}
