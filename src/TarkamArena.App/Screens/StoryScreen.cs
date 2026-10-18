using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Modes;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Screens;

/// <summary>Story campaign: chapters as a route map down the island, the chapter's scene card, and its fight.</summary>
public sealed class StoryScreen : Screen
{
    private readonly Image _backdrop = new() { Stretch = Stretch.UniformToFill, Opacity = 0.4 };
    private readonly Image _hero = new() { Height = 560, Stretch = Stretch.Uniform };
    private readonly Image _villain = new() { Height = 560, Stretch = Stretch.Uniform };
    private readonly SignText _title = new("", 52);
    private readonly TextBlock _eyebrow = Ui.Text("", "eyebrow");
    private readonly TextBlock _intro = Ui.Wrap("", "body").With(t => { t.MaxWidth = 640; t.FontSize = 22; t.LineHeight = 30; });
    private StoryChapter? _selected;

    public StoryScreen()
    {
        var list = new StackPanel { Spacing = 8 };
        int unlocked = Math.Min(StoryCampaign.Chapters.Count, Game.Profile.StoryChapter + 1);
        foreach (var ch in StoryCampaign.Chapters)
        {
            bool open = ch.Number <= unlocked;
            bool done = ch.Number <= Game.Profile.StoryChapter;
            var b = Ui.Banner($"{ch.Number}. {ch.Title.ToString().ToUpperInvariant()}{(done ? "  ✓" : "")}", () => Play(ch), small: true);
            b.IsEnabled = open;
            var c = ch;
            b.GotFocus += (_, _) => Show(c);
            list.Children.Add(b);
            Nav(b);
        }
        NavIndex = Math.Min(unlocked - 1, StoryCampaign.Chapters.Count - 1);

        var prologue = Ui.Wrap(StoryCampaign.Prologue.ToString(), "body", "muted").With(t => t.MaxWidth = 420);
        var left = Ui.V(18, new SignText(Loc.T("story.title").ToUpperInvariant(), 46), prologue, list);
        left.Margin = new Thickness(64, 50, 0, 0);

        var card = Ui.V(12, _eyebrow, _title, _intro, Ui.Text(Loc.Current == Language.Indonesian ? "Tekan Enter untuk bertarung" : "Press Enter to fight", "eyebrow"));
        card.Margin = new Thickness(560, 0, 60, 70);
        card.VerticalAlignment = VerticalAlignment.Bottom;

        var faces = Ui.H(-80, _hero, _villain.With(v => v.RenderTransform = new ScaleTransform(-1, 1)));
        faces.HorizontalAlignment = HorizontalAlignment.Right;
        faces.VerticalAlignment = VerticalAlignment.Top;
        faces.Margin = new Thickness(0, 20, 40, 0);
        faces.Opacity = 0.95;

        var root = new Grid();
        root.Children.Add(_backdrop);
        root.Children.Add(faces);
        root.Children.Add(new Border { Background = Ui.Brush(0x0A1638, 170), Width = 560, HorizontalAlignment = HorizontalAlignment.Left });
        root.Children.Add(left);
        root.Children.Add(card);
        Content = root;
    }

    private void Show(StoryChapter ch)
    {
        _selected = ch;
        var hero = Roster.Get(ch.HeroId);
        var villain = Roster.Get(ch.OpponentId);
        _backdrop.Source = Images.Backdrop(Stages.Get(ch.StageId), 1600);
        _hero.Source = Images.Portrait(hero, 560);
        _villain.Source = Images.Portrait(villain, 560);
        _eyebrow.Text = $"{Loc.T("story.chapter", ch.Number).ToUpperInvariant()} · {Stages.Get(ch.StageId).Name.ToString().ToUpperInvariant()}";
        _title.Text = $"{hero.Name.ToString().ToUpperInvariant()} VS {villain.Name.ToString().ToUpperInvariant()}";
        _intro.Text = ch.Intro.ToString();
    }

    private void Play(StoryChapter ch)
    {
        var plan = MatchPlan.For(GameMode.Story);
        plan.Fighters[0] = Roster.Get(ch.HeroId);
        plan.Fighters[1] = Roster.Get(ch.OpponentId);
        plan.Stage = Stages.Get(ch.StageId);
        plan.Difficulty = ch.Difficulty;
        plan.OpponentsFixed = true;
        plan.StageFixed = true;
        plan.OnFinished = (summary, reward) =>
        {
            if (summary.Won && Game.Profile.StoryChapter < ch.Number)
            {
                Game.Profile.StoryChapter = ch.Number;
                var extra = ProgressionService.Refresh(Game.Profile);
                Window.ToastRewards(extra);
                Game.Save();
            }
            string text = summary.Won ? ch.Outro.ToString() : (Loc.Current == Language.Indonesian ? "Bangkit lagi. Cerita belum selesai." : "Get up. The story is not over.");
            if (summary.Won && ch.Number == StoryCampaign.Chapters.Count) text += "\n\n" + StoryCampaign.Epilogue;
            Window.Replace(new StoryCardScreen(ch, text, summary.Won));
            return true;
        };
        Window.Show(new FightScreen(plan));
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action == MenuAction.Confirm && _selected is not null && NavItems[NavIndex].IsEnabled)
        {
            Play(_selected);
            return true;
        }
        return base.HandleAction(action);
    }
}

/// <summary>The chapter's ending text shown after its fight.</summary>
public sealed class StoryCardScreen : Screen
{
    public StoryCardScreen(StoryChapter ch, string text, bool won)
    {
        var b = Ui.Banner(Loc.T("common.continue").ToUpperInvariant(), () => { Window.Home(); Window.Show(new StoryScreen()); });
        Nav(b);
        var body = Ui.V(24,
            Ui.Text(Loc.T("story.chapter", ch.Number).ToUpperInvariant(), "eyebrow"),
            new SignText(ch.Title.ToString().ToUpperInvariant(), 64, won ? Ui.Spanduk : Ui.Kapur),
            Ui.Wrap(text, "body").With(t => { t.MaxWidth = 900; t.FontSize = 24; t.LineHeight = 34; }),
            b);
        body.Margin = new Thickness(120, 0, 120, 0);
        body.VerticalAlignment = VerticalAlignment.Center;
        var root = new Grid();
        root.Children.Add(Ui.Picture(Images.Backdrop(Stages.Get(ch.StageId), 1600), stretch: Stretch.UniformToFill).With(i => i.Opacity = 0.3));
        root.Children.Add(body);
        Content = root;
    }
}
