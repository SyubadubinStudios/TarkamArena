using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Creator;
using TarkamArena.Core.Match;
using TarkamArena.Core.Modes;
using TarkamArena.Core.Progression;

namespace TarkamArena.Tests;

public class ProgressionTests
{
    [Fact]
    public void New_profile_owns_the_base_roster_but_not_secrets()
    {
        var p = PlayerProfile.CreateNew();
        Assert.True(p.HasCharacter("pesilat"));
        Assert.False(p.HasCharacter("gatotkaca"));
    }

    [Fact]
    public void Winning_with_a_finisher_gives_rewards_and_achievements()
    {
        var p = PlayerProfile.CreateNew();
        var reward = ProgressionService.Apply(p, new MatchSummary
        {
            Mode = GameMode.Versus, CharacterId = "pesilat", Won = true, MaxCombo = 6, Parries = 1, Finisher = FinisherKind.Fatality, CrowdPeak = 1000,
        });

        Assert.True(reward.Xp > 0);
        Assert.True(reward.Coins > 0);
        Assert.Equal(1, p.Wins);
        Assert.Contains(reward.NewAchievements, a => a.Id == "first-win");
        Assert.Contains(reward.NewAchievements, a => a.Id == "fatality-1");
        Assert.Contains(reward.NewAchievements, a => a.Id == "crowd-max");
        Assert.True(p.Reputation["pesilat"] > 0);
    }

    [Fact]
    public void Winning_a_tournament_unlocks_gatotkaca()
    {
        var p = PlayerProfile.CreateNew();
        p.TournamentWins = 1;
        var reward = ProgressionService.Refresh(p);
        Assert.True(p.HasCharacter("gatotkaca"));
        Assert.Contains(reward.NewUnlocks, u => u.CharacterId == "gatotkaca");
    }

    [Fact]
    public void Levels_up_and_buys_cosmetics()
    {
        var p = PlayerProfile.CreateNew();
        for (int i = 0; i < 10; i++)
            ProgressionService.Apply(p, new MatchSummary { Mode = GameMode.Versus, CharacterId = "pocong", Won = true });
        Assert.True(p.Level > 1);

        var costume = Unlockables.CostumesFor("pocong").First();
        p.Coins = 10_000;
        Assert.True(p.TryBuy(costume));
        Assert.False(p.TryBuy(costume));
    }

    [Fact]
    public void Rating_moves_towards_the_winner()
    {
        var (w, l) = Rating.Update(1000, 1000);
        Assert.Equal(1016, w);
        Assert.Equal(984, l);
        Assert.Equal("Kecamatan", Rating.Tier(1000).Id);
    }

    [Fact]
    public void Tournament_runs_to_a_champion()
    {
        var t = new Tournament(Roster.Get("pesilat"), Roster.All, 5);
        Assert.Equal(8, t.Field.Count);
        while (t.NextPlayerMatch is not null) t.ReportPlayerResult(true);
        Assert.Equal(Roster.Get("pesilat"), t.Champion);
    }

    [Fact]
    public void Tournament_finishes_without_the_player_after_a_loss()
    {
        var t = new Tournament(Roster.Get("pesilat"), Roster.All, 5);
        t.ReportPlayerResult(false);
        Assert.True(t.PlayerEliminated);
        Assert.True(t.IsFinished);
        Assert.NotEqual(Roster.Get("pesilat"), t.Champion);
    }

    [Fact]
    public void Survival_carries_health_and_ramps_up()
    {
        var run = new SurvivalRun(Roster.Get("pesilat"), 1);
        run.ReportResult(true, 50);
        Assert.Equal(2, run.Wave);
        Assert.Equal(70, run.HealthPercent);
        run.ReportResult(false, 0);
        Assert.True(run.IsOver);
    }

    [Fact]
    public void Creator_validates_the_stat_budget()
    {
        var bad = new CustomFighterSpec { Name = "Joko", Power = 9, Speed = 9, Defense = 9, Technique = 9 };
        Assert.Contains("creator.error.points", CharacterCreator.Validate(bad));

        var good = new CustomFighterSpec { Name = "Joko Kendil", Power = 8, Speed = 6, Defense = 5, Technique = 5, Base = CreatorBase.Jawara, Features = BodyFeature.Weapon };
        Assert.Empty(CharacterCreator.Validate(good));
        var def = CharacterCreator.ToDefinition(good);
        Assert.Equal(CharacterCategory.Custom, def.Category);
        Assert.Equal(9, FinisherFactory.Build(def).Length);
    }

    [Fact]
    public void Stats_tracker_reads_match_events()
    {
        var context = TestMatch.Create();
        var state = TestMatch.StartFight(context);
        var tracker = new MatchStatsTracker(0, 0);
        TestMatch.PlaceClose(ref state);
        TestMatch.Run(ref state, context, 1, InputButtons.Light);
        for (int i = 0; i < 30; i++)
        {
            TestMatch.Run(ref state, context, 1);
            tracker.Observe(state, context);
        }
        var summary = tracker.Build(state, context);
        Assert.True(summary.DamageDealt > 0);
    }
}
