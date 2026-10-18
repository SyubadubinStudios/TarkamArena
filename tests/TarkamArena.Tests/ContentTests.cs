using TarkamArena.Core.Arena;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Modes;

namespace TarkamArena.Tests;

public class ContentTests
{
    [Fact]
    public void Roster_matches_the_design_document()
    {
        Assert.Equal(9, Roster.InCategory(CharacterCategory.Profession).Count());
        Assert.Equal(12, Roster.InCategory(CharacterCategory.Mystic).Count());
        Assert.Equal(11, Roster.InCategory(CharacterCategory.Society).Count());
        Assert.Equal(5, Roster.InCategory(CharacterCategory.Wildlife).Count());
        Assert.Equal(5, Roster.InCategory(CharacterCategory.Secret).Count());
        Assert.Equal(9, Roster.All.Count(c => c.Dlc != DlcPack.None));
        Assert.Equal(Roster.All.Count, Roster.All.Select(c => c.Id).Distinct().Count());
    }

    [Fact]
    public void Every_fighter_has_nine_finishers_without_sequence_conflicts()
    {
        foreach (var c in Roster.All)
        {
            var finishers = FinisherFactory.Build(c);
            Assert.Equal(3, finishers.Count(f => f.Kind == FinisherKind.Fatality));
            Assert.Equal(2, finishers.Count(f => f.Kind == FinisherKind.Brutality));
            Assert.Equal(2, finishers.Count(f => f.Kind == FinisherKind.Friendship));
            Assert.Single(finishers, f => f.Kind == FinisherKind.Mercy);
            Assert.Single(finishers, f => f.Kind == FinisherKind.Legendary);

            var codes = finishers.Select(f => f.SequenceCode).ToArray();
            Assert.Equal(codes.Length, codes.Distinct().Count());
            foreach (var a in codes)
                foreach (var b in codes)
                    if (a != b) Assert.False(b.EndsWith(a, StringComparison.Ordinal), $"{c.Id}: {a} is a suffix of {b}");
        }
    }

    [Fact]
    public void Finisher_sequences_are_stable_per_fighter()
    {
        var c = Roster.Get("pesilat");
        Assert.Equal(FinisherFactory.Build(c).Select(f => f.SequenceCode), FinisherFactory.Build(c).Select(f => f.SequenceCode));
    }

    [Fact]
    public void Every_fighter_has_bilingual_text()
    {
        foreach (var c in Roster.All)
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Name.En));
            Assert.False(string.IsNullOrWhiteSpace(c.Title.Id));
            Assert.False(string.IsNullOrWhiteSpace(c.Bio.En));
            Assert.False(string.IsNullOrWhiteSpace(c.Bio.Id));
            Assert.All(c.FinisherNames, n => Assert.False(string.IsNullOrWhiteSpace(n.En) || string.IsNullOrWhiteSpace(n.Id)));
            if (c.IsSecret) Assert.False(string.IsNullOrWhiteSpace(c.UnlockHint.Id));
        }
    }

    [Fact]
    public void Movesets_have_sane_frame_data()
    {
        foreach (var c in Roster.All)
        {
            var moves = MoveSetFactory.Build(c);
            Assert.Equal((int)MoveId.Count, moves.Length);
            for (int i = 1; i < moves.Length; i++)
            {
                Assert.Equal((MoveId)i, moves[i].Id);
                Assert.True(moves[i].Startup >= 1);
                Assert.True(moves[i].Active >= 1);
            }
        }
    }

    [Fact]
    public void Stages_match_the_design_document()
    {
        Assert.Equal(18, Stages.All.Count);
        Assert.All(Stages.All, s => Assert.NotEmpty(s.Npcs));
        Assert.All(Stages.All, s => Assert.NotEmpty(s.Events));
        var market = Stages.Get("pasar-becek");
        Assert.Contains(market.Events, e => e.Kind == ArenaEventKind.LooseChicken);
        Assert.Contains(market.Events, e => e.Kind == ArenaEventKind.RagingGoat);
    }

    [Fact]
    public void Story_chapters_reference_real_content()
    {
        foreach (var ch in StoryCampaign.Chapters)
        {
            Assert.True(Roster.TryGet(ch.HeroId, out _), ch.HeroId);
            Assert.True(Roster.TryGet(ch.OpponentId, out _), ch.OpponentId);
            Assert.True(Stages.IndexOf(ch.StageId) >= 0, ch.StageId);
        }
    }

    [Fact]
    public void Ui_strings_exist_in_both_languages()
    {
        foreach (var (key, text) in Loc.Table)
        {
            Assert.False(string.IsNullOrWhiteSpace(text.En), key);
            Assert.False(string.IsNullOrWhiteSpace(text.Id), key);
        }
        Assert.Equal("Dibuat oleh Ariana Mischa Fadhila dari Syubadubin Studios", Loc.Table["app.credit"].Id);
    }

    [Fact]
    public void Npcs_react_to_fighters_they_know()
    {
        Assert.Equal(NpcReaction.Fear, NpcDirector.Recognise(NpcKind.Trader, "preman-pasar"));
        Assert.Equal(NpcReaction.Cheer, NpcDirector.Recognise(NpcKind.Shopper, "abang-bakso"));
        Assert.Equal(NpcReaction.Fear, NpcDirector.Recognise(NpcKind.Tourist, "kuntilanak"));

        var director = new NpcDirector(Stages.Get("pasar-becek"), 40);
        director.Update(0.5f, 0.8f, [(System.Numerics.Vector3.Zero, "preman-pasar")], []);
        Assert.Equal(40, director.Agents.Length);
    }
}
