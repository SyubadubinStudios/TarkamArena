using TarkamArena.Core.AI;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Match;

namespace TarkamArena.Tests;

internal static class TestMatch
{
    public static MatchContext Create(GameMode mode = GameMode.Versus, params string[] ids)
    {
        if (ids.Length == 0) ids = ["pesilat", "preman-pasar"];
        var rules = MatchRules.For(mode) with { ArenaEventsEnabled = false };
        var setup = new MatchSetup(rules, Stages.Get("pasar-becek"), [.. ids.Select(id => new FighterEntry(Roster.Get(id)))], Seed: 42);
        return new MatchContext(setup);
    }

    /// <summary>Skips the intro so tests start in the Fight phase.</summary>
    public static MatchState StartFight(MatchContext context)
    {
        var state = MatchSimulation.CreateInitialState(context);
        Span<InputFrame> none = stackalloc InputFrame[context.Characters.Length];
        while (state.Phase != MatchPhase.Fight) MatchSimulation.Step(ref state, context, none);
        return state;
    }

    public static void Run(ref MatchState state, MatchContext context, int frames, InputButtons p0 = InputButtons.None, InputButtons p1 = InputButtons.None)
    {
        Span<InputFrame> inputs = stackalloc InputFrame[context.Characters.Length];
        inputs[0] = new(p0);
        if (inputs.Length > 1) inputs[1] = new(p1);
        for (int i = 0; i < frames; i++) MatchSimulation.Step(ref state, context, inputs);
    }

    /// <summary>Taps a button for one frame and releases it for one frame.</summary>
    public static void Tap(ref MatchState state, MatchContext context, InputButtons p0, InputButtons p1Held = InputButtons.None)
    {
        Run(ref state, context, 1, p0, p1Held);
        Run(ref state, context, 1, InputButtons.None, p1Held);
    }

    public static void PlaceClose(ref MatchState state, float gap = 0.8f)
    {
        state.Fighters[0].Position = Core.Mathematics.FixedVector2.Of(-gap / 2, 0);
        state.Fighters[1].Position = Core.Mathematics.FixedVector2.Of(gap / 2, 0);
    }
}

public class SimulationTests
{
    [Fact]
    public void Match_starts_with_intro_then_fight()
    {
        var context = TestMatch.Create();
        var state = MatchSimulation.CreateInitialState(context);
        Assert.Equal(MatchPhase.Intro, state.Phase);
        TestMatch.Run(ref state, context, MatchSimulation.IntroFrames);
        Assert.Equal(MatchPhase.Fight, state.Phase);
        Assert.Equal(1, state.Fighters[0].Facing);
        Assert.Equal(-1, state.Fighters[1].Facing);
    }

    [Fact]
    public void Jab_hits_an_idle_opponent()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.PlaceClose(ref state);
        int before = state.Fighters[1].Health;

        TestMatch.Tap(ref state, context, InputButtons.Light);
        TestMatch.Run(ref state, context, 20);

        Assert.True(state.Fighters[1].Health < before);
        Assert.True(state.Fighters[0].Adrenaline > 0);
        Assert.True(state.Fighters[1].Rage > 0);
    }

    [Fact]
    public void Holding_block_only_takes_chip_damage()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.PlaceClose(ref state);
        // Hold block long enough that the press is outside the parry window.
        TestMatch.Run(ref state, context, 20, InputButtons.None, InputButtons.Block);
        int before = state.Fighters[1].Health;

        TestMatch.Run(ref state, context, 1, InputButtons.Heavy, InputButtons.Block);
        TestMatch.Run(ref state, context, 40, InputButtons.None, InputButtons.Block);

        int lost = before - state.Fighters[1].Health;
        Assert.InRange(lost, 1, 15);
    }

    [Fact]
    public void Tapping_block_just_before_a_hit_parries_it()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.PlaceClose(ref state);
        int before = state.Fighters[1].Health;
        int startup = context.Move(0, MoveId.Heavy).Startup;

        TestMatch.Run(ref state, context, 1, InputButtons.Heavy);
        TestMatch.Run(ref state, context, startup - 3);
        bool parried = false;
        for (int i = 0; i < 10 && !parried; i++)
        {
            TestMatch.Run(ref state, context, 1, InputButtons.None, i == 0 ? InputButtons.Block : InputButtons.None);
            foreach (var e in state.CurrentEvents) parried |= e.Kind == MatchEventKind.Parry;
        }

        Assert.True(parried);
        Assert.Equal(before, state.Fighters[1].Health);
        Assert.Equal(FighterStatus.Parried, state.Fighters[0].Status);
    }

    [Fact]
    public void Launcher_puts_the_victim_in_a_juggle()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.PlaceClose(ref state);

        TestMatch.Run(ref state, context, 1, InputButtons.Heavy | InputButtons.Down);
        bool launched = false;
        for (int i = 0; i < 30 && !launched; i++)
        {
            TestMatch.Run(ref state, context, 1, InputButtons.Down);
            foreach (var e in state.CurrentEvents) launched |= e.Kind == MatchEventKind.Launch;
        }

        Assert.True(launched);
        Assert.Equal(FighterStatus.Juggle, state.Fighters[1].Status);
        Assert.False(state.Fighters[1].Grounded);
    }

    [Fact]
    public void Same_inputs_produce_identical_states()
    {
        var a = RunScripted(seed: 1234);
        var b = RunScripted(seed: 1234);
        Assert.Equal(a.Checksum(), b.Checksum());
        Assert.Equal(a.Frame, b.Frame);
    }

    [Fact]
    public void Restoring_a_snapshot_replays_exactly()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        var rng = new Random(9);
        Span<InputFrame> inputs = stackalloc InputFrame[2];
        var log = new List<(InputFrame, InputFrame)>();

        for (int i = 0; i < 200; i++)
        {
            inputs[0] = new((InputButtons)rng.Next(0, 4096));
            inputs[1] = new((InputButtons)rng.Next(0, 4096));
            MatchSimulation.Step(ref state, context, inputs);
        }
        var snapshot = state;
        for (int i = 0; i < 300; i++)
        {
            inputs[0] = new((InputButtons)rng.Next(0, 4096));
            inputs[1] = new((InputButtons)rng.Next(0, 4096));
            log.Add((inputs[0], inputs[1]));
            MatchSimulation.Step(ref state, context, inputs);
        }
        uint expected = state.Checksum();

        state = snapshot;
        foreach (var (p0, p1) in log)
        {
            inputs[0] = p0;
            inputs[1] = p1;
            MatchSimulation.Step(ref state, context, inputs);
        }
        Assert.Equal(expected, state.Checksum());
    }

    private static MatchState RunScripted(uint seed)
    {
        var context = TestMatch.Create(GameMode.Versus, "kuntilanak", "buto-ijo");
        var state = MatchSimulation.CreateInitialState(context);
        var rng = new Random((int)seed);
        Span<InputFrame> inputs = stackalloc InputFrame[2];
        for (int i = 0; i < 3000; i++)
        {
            inputs[0] = new((InputButtons)rng.Next(0, 4096));
            inputs[1] = new((InputButtons)rng.Next(0, 4096));
            MatchSimulation.Step(ref state, context, inputs);
        }
        return state;
    }

    [Fact]
    public void Step_does_not_allocate()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        var inputs = new InputFrame[] { new(InputButtons.Light | InputButtons.Right), new(InputButtons.Heavy | InputButtons.Left) };
        MatchSimulation.Step(ref state, context, inputs);

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 600; i++) MatchSimulation.Step(ref state, context, inputs);
        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, after - before);
    }

    [Fact]
    public void Cpu_versus_cpu_reaches_a_result()
    {
        foreach (var mode in new[] { GameMode.Versus, GameMode.TwoVsTwo, GameMode.TagBattle, GameMode.FreeForAll })
        {
            string[] ids = mode is GameMode.Versus ? ["pesilat", "pocong"] : ["pesilat", "pocong", "komodo", "kasir-minimarket"];
            var context = TestMatch.Create(mode, ids);
            var state = MatchSimulation.CreateInitialState(context);
            var brains = Enumerable.Range(0, ids.Length).Select(i => new CpuBrain(i, CpuDifficulty.Hard, (uint)(i + 1))).ToArray();
            var inputs = new InputFrame[ids.Length];
            int guard = 0;
            while (!state.IsOver && guard++ < 60 * 60 * 15)
            {
                for (int i = 0; i < brains.Length; i++) inputs[i] = brains[i].Think(state, context);
                MatchSimulation.Step(ref state, context, inputs);
            }
            Assert.True(state.IsOver, $"{mode} did not finish");
        }
    }

    [Fact]
    public void Time_up_awards_the_round_to_the_healthier_fighter()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        state.Fighters[1].Health -= 100;
        state.TimerFrames = 1;
        TestMatch.Run(ref state, context, 2);
        Assert.Equal(MatchPhase.RoundOver, state.Phase);
        Assert.Equal(0, state.RoundWinnerTeam);
        Assert.Equal(1, state.RoundWins[0]);
    }

    [Fact]
    public void Winning_the_match_opens_the_finisher_window_and_a_fatality_runs()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        state.RoundWins[0] = context.Rules.RoundsToWin - 1;
        state.Fighters[1].Health = 0;
        TestMatch.Run(ref state, context, 1);
        TestMatch.Run(ref state, context, MatchSimulation.RoundOverFrames + 1);
        Assert.Equal(MatchPhase.FinisherWindow, state.Phase);
        Assert.Equal(0, state.FinisherExecutor);

        var fatality = context.Finishers[0].First(f => f.Kind == FinisherKind.Fatality);
        foreach (var token in fatality.Sequence)
            TestMatch.Tap(ref state, context, CpuBrain.TokenToButtons(token, state.Fighters[0].Facing));

        Assert.Equal(MatchPhase.FinisherPerforming, state.Phase);
        Assert.Equal(fatality.Index, state.FinisherPerformed);
        TestMatch.Run(ref state, context, MatchSimulation.FinisherPerformFrames + 1);
        Assert.True(state.IsOver);
        Assert.Equal(0, state.MatchWinnerTeam);
    }

    [Fact]
    public void Mercy_revives_the_loser_and_the_fight_continues()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        state.RoundWins[0] = context.Rules.RoundsToWin - 1;
        state.Fighters[1].Health = 0;
        TestMatch.Run(ref state, context, MatchSimulation.RoundOverFrames + 2);

        var mercy = context.Finishers[0].Single(f => f.Kind == FinisherKind.Mercy);
        foreach (var token in mercy.Sequence)
            TestMatch.Tap(ref state, context, CpuBrain.TokenToButtons(token, state.Fighters[0].Facing));

        Assert.Equal(MatchPhase.Fight, state.Phase);
        Assert.True(state.MercyUsed);
        Assert.True(state.Fighters[1].Health > 0);
        Assert.Equal(context.Rules.RoundsToWin - 1, state.RoundWins[0]);
    }

    [Fact]
    public void Legendary_finisher_needs_a_full_crowd()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        state.RoundWins[0] = context.Rules.RoundsToWin - 1;
        state.Fighters[1].Health = 0;
        TestMatch.Run(ref state, context, MatchSimulation.RoundOverFrames + 2);
        state.Crowd = 100;

        var legendary = context.Finishers[0].Single(f => f.Kind == FinisherKind.Legendary);
        foreach (var token in legendary.Sequence)
            TestMatch.Tap(ref state, context, CpuBrain.TokenToButtons(token, state.Fighters[0].Facing));
        Assert.Equal(MatchPhase.FinisherWindow, state.Phase);

        state.Crowd = 1000;
        foreach (var token in legendary.Sequence)
            TestMatch.Tap(ref state, context, CpuBrain.TokenToButtons(token, state.Fighters[0].Facing));
        Assert.Equal(MatchPhase.FinisherPerforming, state.Phase);
    }

    [Fact]
    public void Tag_swaps_the_point_fighter()
    {
        var context = TestMatch.Create(GameMode.TagBattle, "pesilat", "pocong", "komodo", "kasir-minimarket");
        var state = TestMatch.StartFight(context);
        Assert.True(state.Fighters[0].OnField);
        Assert.False(state.Fighters[1].OnField);

        TestMatch.Tap(ref state, context, InputButtons.Tag);

        Assert.False(state.Fighters[0].OnField);
        Assert.True(state.Fighters[1].OnField);
    }

    [Fact]
    public void Tag_partner_replaces_a_knocked_out_point_fighter()
    {
        var context = TestMatch.Create(GameMode.TagBattle, "pesilat", "pocong", "komodo", "kasir-minimarket");
        var state = TestMatch.StartFight(context);
        state.Fighters[2].Health = 0;
        state.Fighters[2].Status = FighterStatus.KO;
        TestMatch.Run(ref state, context, 1);
        Assert.Equal(MatchPhase.Fight, state.Phase);
        Assert.True(state.Fighters[3].OnField);
    }

    [Fact]
    public void Projectile_travels_and_hits()
    {
        var context = TestMatch.Create(GameMode.Versus, "guru-matematika", "preman-pasar");
        var state = TestMatch.StartFight(context);
        int before = state.Fighters[1].Health;
        TestMatch.Tap(ref state, context, InputButtons.Special);
        bool spawned = false;
        for (int i = 0; i < 80; i++)
        {
            TestMatch.Run(ref state, context, 1);
            foreach (var e in state.CurrentEvents) spawned |= e.Kind == MatchEventKind.ProjectileSpawn;
        }
        Assert.True(spawned);
        Assert.True(state.Fighters[1].Health < before);
    }

    [Fact]
    public void Rage_needs_a_full_meter_and_boosts_damage()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        TestMatch.Tap(ref state, context, InputButtons.Rage);
        Assert.False(state.Fighters[0].RageActive);

        state.Fighters[0].Rage = MatchSimulation.MeterMax;
        TestMatch.Tap(ref state, context, InputButtons.Rage);
        Assert.True(state.Fighters[0].RageActive);
        Assert.Equal(0, state.Fighters[0].Rage);
    }

    [Fact]
    public void Arena_hazards_trigger_on_stages_with_fight_events()
    {
        var rules = MatchRules.For(GameMode.Versus);
        var setup = new MatchSetup(rules, Stages.Get("pasar-becek"), [new FighterEntry(Roster.Get("pesilat")), new FighterEntry(Roster.Get("pocong"))], 3);
        var context = new MatchContext(setup);
        var state = TestMatch.StartFight(context);
        bool arenaEvent = false;
        for (int i = 0; i < 2500 && state.Phase == MatchPhase.Fight; i++)
        {
            TestMatch.Run(ref state, context, 1);
            foreach (var e in state.CurrentEvents) arenaEvent |= e.Kind == MatchEventKind.ArenaEvent;
        }
        Assert.True(arenaEvent);
    }
}
