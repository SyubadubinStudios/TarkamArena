using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;

namespace TarkamArena.Core.Progression;

public sealed record Achievement(string Id, LocalizedText Name, LocalizedText Description, Func<PlayerProfile, MatchSummary?, bool> IsEarned);

/// <summary>
/// Achievements are checked after every match against the updated profile and the match summary.
/// Thirty or more of them unlock Jaka Tarub.
/// </summary>
public static class Achievements
{
    private static LocalizedText T(string en, string id) => new(en, id);

    private static Achievement A(string id, string en, string idn, string descEn, string descId, Func<PlayerProfile, MatchSummary?, bool> rule) =>
        new(id, T(en, idn), T(descEn, descId), rule);

    public static IReadOnlyList<Achievement> All { get; } = Build();

    private static List<Achievement> Build()
    {
        var list = new List<Achievement>
        {
            A("first-win", "First blood", "Kemenangan pertama", "Win your first match.", "Menangkan pertandingan pertamamu.", (p, _) => p.Wins >= 1),
            A("wins-10", "Kampung champion", "Jagoan kampung", "Win 10 matches.", "Menangkan 10 pertandingan.", (p, _) => p.Wins >= 10),
            A("wins-50", "District champion", "Jagoan kecamatan", "Win 50 matches.", "Menangkan 50 pertandingan.", (p, _) => p.Wins >= 50),
            A("wins-200", "National hero", "Pahlawan nasional", "Win 200 matches.", "Menangkan 200 pertandingan.", (p, _) => p.Wins >= 200),
            A("matches-25", "Regular", "Langganan", "Play 25 matches.", "Mainkan 25 pertandingan.", (p, _) => p.MatchesPlayed >= 25),
            A("combo-5", "Rhythm", "Berirama", "Land a 5-hit combo.", "Daratkan kombo 5 pukulan.", (_, m) => m?.MaxCombo >= 5),
            A("combo-10", "Gamelan orchestra", "Orkestra gamelan", "Land a 10-hit combo.", "Daratkan kombo 10 pukulan.", (_, m) => m?.MaxCombo >= 10),
            A("perfect", "Untouchable", "Tak tersentuh", "Win a round without taking damage.", "Menangkan ronde tanpa terkena damage.", (_, m) => m?.PerfectRound == true),
            A("parry-1", "Read like a book", "Terbaca", "Parry an attack.", "Tangkis sebuah serangan.", (p, _) => p.Parries >= 1),
            A("parry-50", "Wall of silat", "Tembok silat", "Parry 50 attacks.", "Tangkis 50 serangan.", (p, _) => p.Parries >= 50),
            A("counter-25", "Timing is everything", "Soal waktu", "Land 25 counter hits.", "Daratkan 25 counter hit.", (p, _) => p.CounterHits >= 25),
            A("rage-1", "Boiling over", "Naik darah", "Activate Rage mode.", "Aktifkan mode amarah.", (p, _) => p.RageActivations >= 1),
            A("adrenaline-1", "Second wind", "Napas kedua", "Trigger Adrenaline mode.", "Picu mode adrenalin.", (p, _) => p.AdrenalineActivations >= 1),
            A("team-combo", "Gotong royong", "Gotong royong", "Land a team combo.", "Daratkan team combo.", (p, _) => p.TeamCombos >= 1),
            A("fatality-1", "Finish them", "Habisi", "Perform a Fatality.", "Lakukan Fatality.", (p, _) => p.FinisherCount(FinisherKind.Fatality) >= 1),
            A("brutality-1", "No mercy", "Tanpa ampun", "Perform a Brutality.", "Lakukan Brutality.", (p, _) => p.FinisherCount(FinisherKind.Brutality) >= 1),
            A("friendship-1", "Friendship is magic", "Persahabatan", "Perform a Friendship.", "Lakukan Friendship.", (p, _) => p.FinisherCount(FinisherKind.Friendship) >= 1),
            A("mercy-1", "Second chance", "Kesempatan kedua", "Show Mercy.", "Berikan ampunan.", (p, _) => p.FinisherCount(FinisherKind.Mercy) >= 1),
            A("legendary-1", "Born in the arena", "Lahir di arena", "Perform a Legendary Finisher.", "Lakukan Finisher Legenda.", (p, _) => p.FinisherCount(FinisherKind.Legendary) >= 1),
            A("finishers-25", "Master of endings", "Ahli penutup", "Perform 25 finishers.", "Lakukan 25 finisher.", (p, _) => p.TotalFinishers >= 25),
            A("crowd-max", "The crowd goes wild", "Penonton menggila", "Fill the crowd meter.", "Penuhi meter sorakan.", (_, m) => m?.CrowdPeak >= 1000),
            A("hazard", "Watch out!", "Awas!", "Get hit by an arena event.", "Terkena event arena.", (p, _) => p.HazardHits >= 1),
            A("story-1", "The flyer", "Selebaran", "Finish chapter 1 of the story.", "Tamatkan bab 1 cerita.", (p, _) => p.StoryChapter >= 1),
            A("story-5", "Halfway south", "Setengah jalan", "Finish chapter 5 of the story.", "Tamatkan bab 5 cerita.", (p, _) => p.StoryChapter >= 5),
            A("story-done", "The one who said no", "Yang berani menolak", "Finish the story.", "Tamatkan mode cerita.", (p, _) => p.StoryChapter >= 9),
            A("tournament-1", "Tarkam Cup", "Piala Tarkam", "Win a tournament.", "Juarai turnamen.", (p, _) => p.TournamentWins >= 1),
            A("tournament-5", "Dynasty", "Dinasti", "Win 5 tournaments.", "Juarai 5 turnamen.", (p, _) => p.TournamentWins >= 5),
            A("survival-5", "Still standing", "Masih berdiri", "Reach survival wave 5.", "Capai gelombang survival ke-5.", (p, _) => p.SurvivalBest >= 5),
            A("survival-10", "Iron will", "Tekad baja", "Reach survival wave 10.", "Capai gelombang survival ke-10.", (p, _) => p.SurvivalBest >= 10),
            A("ffa-win", "Last one standing", "Bertahan terakhir", "Win a free for all.", "Menangkan mode bebas semua.", (_, m) => m is { Won: true, Mode: GameMode.FreeForAll }),
            A("tag-win", "Tag team", "Tim tag", "Win a tag battle.", "Menangkan tag battle.", (_, m) => m is { Won: true, Mode: GameMode.TagBattle }),
            A("twovtwo-win", "Partners in crime", "Partner sejati", "Win a 2 vs 2 match.", "Menangkan pertandingan 2 lawan 2.", (_, m) => m is { Won: true, Mode: GameMode.TwoVsTwo }),
            A("online-win", "Known across the islands", "Terkenal se-Nusantara", "Win an online ranked match.", "Menangkan ranked online.", (_, m) => m is { Won: true, Mode: GameMode.OnlineRanked }),
            A("creator", "Made in Indonesia", "Buatan Indonesia", "Create a custom fighter.", "Buat petarung kustom.", (p, _) => p.CustomFighters.Count >= 1),
            A("level-10", "Rising star", "Bintang baru", "Reach level 10.", "Capai level 10.", (p, _) => p.Level >= 10),
            A("level-25", "Veteran", "Veteran", "Reach level 25.", "Capai level 25.", (p, _) => p.Level >= 25),
            A("collector", "Collector", "Kolektor", "Own 30 cosmetics.", "Miliki 30 kosmetik.", (p, _) => p.Unlocks.Count(u => !u.StartsWith("char-", StringComparison.Ordinal)) >= 30),
            A("reputation", "Crowd favourite", "Idola penonton", "Reach 1000 crowd reputation with one fighter.", "Capai 1000 reputasi penonton dengan satu petarung.", (p, _) => p.Reputation.Values.DefaultIfEmpty(0).Max() >= 1000),
        };

        foreach (var (category, en, idn) in new[]
                 {
                     (CharacterCategory.Profession, "Every trade", "Semua profesi"),
                     (CharacterCategory.Mystic, "Every spirit", "Semua makhluk gaib"),
                     (CharacterCategory.Wildlife, "Every creature", "Semua satwa"),
                     (CharacterCategory.Society, "Every citizen", "Semua warga"),
                 })
        {
            var ids = Roster.InCategory(category).Select(c => c.Id).ToArray();
            string name = category.ToString().ToLowerInvariant();
            list.Add(A($"category-{name}", en, idn,
                "Win a match with every fighter in this category.", "Menang dengan setiap petarung di kategori ini.",
                (p, _) => ids.All(id => p.WinsWith(id) > 0)));
        }
        return list;
    }
}
