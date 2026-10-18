using TarkamArena.Core.AI;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;

namespace TarkamArena.Tests;

public class SignatureTests
{
    /// <summary>Feeds the motion for signature <paramref name="sig"/> (player 0 faces right) ending in a Special press.</summary>
    private static void Perform(ref MatchState state, MatchContext context, int sig)
    {
        switch (sig)
        {
            case 0:
                TestMatch.Run(ref state, context, 2, InputButtons.Down);
                TestMatch.Run(ref state, context, 2, InputButtons.Down | InputButtons.Right);
                TestMatch.Run(ref state, context, 1, InputButtons.Right | InputButtons.Special);
                break;
            case 1:
                TestMatch.Run(ref state, context, 2, InputButtons.Down);
                TestMatch.Run(ref state, context, 2, InputButtons.Down | InputButtons.Left);
                TestMatch.Run(ref state, context, 1, InputButtons.Left | InputButtons.Special);
                break;
            default:
                TestMatch.Run(ref state, context, 2, InputButtons.Down);
                TestMatch.Run(ref state, context, 2);
                TestMatch.Run(ref state, context, 1, InputButtons.Down | InputButtons.Special);
                break;
        }
    }

    [Fact]
    public void Every_character_has_three_signatures_with_rising_costs()
    {
        foreach (var c in Roster.All)
        {
            var moves = SignatureCatalog.For(c);
            Assert.Equal(3, moves.Length);
            Assert.True(moves[0].Cost < moves[1].Cost && moves[1].Cost < moves[2].Cost);
            Assert.All(moves, m => Assert.False(string.IsNullOrWhiteSpace(m.Name.ToString())));
        }
    }

    [Fact]
    public void Signature_needs_meter_and_spends_it()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        state.Fighters[0].Jurus = 100;
        Perform(ref state, context, 0);
        Assert.NotEqual(MoveId.Signature, state.Fighters[0].Move);

        TestMatch.Run(ref state, context, 60);
        state.Fighters[0].Jurus = SignatureMove.Costs[0] + 10;
        bool started = false;
        Perform(ref state, context, 0);
        foreach (var e in state.CurrentEvents) started |= e.Kind == MatchEventKind.SignatureStart;
        Assert.True(started);
        Assert.Equal(MoveId.Signature, state.Fighters[0].Move);
        Assert.Equal(10, state.Fighters[0].Jurus);
        Assert.True(state.Fighters[1].Hitstop > 0, "the opponent freezes for the cut-in");
    }

    [Fact]
    public void Each_input_picks_its_own_slot()
    {
        for (int sig = 0; sig < 3; sig++)
        {
            var context = TestMatch.Create();
            var state = TestMatch.StartFight(context);
            state.Fighters[0].Jurus = MatchSimulation.MeterMax;
            Perform(ref state, context, sig);
            Assert.Equal(MoveId.Signature, state.Fighters[0].Move);
            Assert.Equal(sig, state.Fighters[0].SignatureSlot);
        }
    }

    [Fact]
    public void Every_signature_of_every_character_damages_the_opponent()
    {
        foreach (var c in Roster.All)
        {
            for (int sig = 0; sig < 3; sig++)
            {
                var context = TestMatch.Create(GameMode.Versus, c.Id, "preman-pasar");
                var state = TestMatch.StartFight(context);
                TestMatch.PlaceClose(ref state, 2.2f);
                state.Fighters[0].Jurus = MatchSimulation.MeterMax;
                int before = state.Fighters[1].Health;
                Perform(ref state, context, sig);
                TestMatch.Run(ref state, context, 220);
                Assert.True(state.Fighters[1].Health < before, $"{c.Id} signature {sig} ({context.Signatures[0][sig].Kind}) did no damage");
            }
        }
    }

    [Fact]
    public void Signatures_replay_deterministically()
    {
        var context = TestMatch.Create(GameMode.Versus, "pesilat", "preman-pasar");
        MatchState Play()
        {
            var state = TestMatch.StartFight(context);
            state.Fighters[0].Jurus = MatchSimulation.MeterMax;
            state.Fighters[1].Jurus = MatchSimulation.MeterMax;
            Perform(ref state, context, 2);
            TestMatch.Run(ref state, context, 90);
            return state;
        }
        Assert.Equal(Play().Checksum(), Play().Checksum());
    }

    [Fact]
    public void Landing_hits_builds_the_jurus_meter()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.PlaceClose(ref state);
        int before = state.Fighters[0].Jurus;
        TestMatch.Tap(ref state, context, InputButtons.Light);
        TestMatch.Run(ref state, context, 20);
        Assert.True(state.Fighters[0].Jurus > before);
    }

    [Fact]
    public void Cpu_spends_its_meter_on_signatures()
    {
        var context = TestMatch.Create(GameMode.Versus, "kuyang", "pemadam-karhutla");
        var state = TestMatch.StartFight(context);
        var brains = new[] { new CpuBrain(0, CpuDifficulty.Legend, 1), new CpuBrain(1, CpuDifficulty.Legend, 2) };
        var inputs = new InputFrame[2];
        bool used = false;
        for (int f = 0; f < 60 * 120 && !used && !state.IsOver; f++)
        {
            for (int i = 0; i < 2; i++) inputs[i] = brains[i].Think(state, context);
            MatchSimulation.Step(ref state, context, inputs);
            foreach (var e in state.CurrentEvents) used |= e.Kind == MatchEventKind.SignatureStart;
        }
        Assert.True(used);
    }
}
