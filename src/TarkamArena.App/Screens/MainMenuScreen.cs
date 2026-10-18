using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Screens;

/// <summary>
/// Main menu: a stack of spanduk strips on the left, a rotating poster of a fighter on the right,
/// the player's standing in the corner.
/// </summary>
public sealed class MainMenuScreen : Screen
{
    private readonly TextBlock _hint = Ui.Wrap("", "body").With(t => t.MaxWidth = 420);
    private readonly Image _poster = new() { Stretch = Stretch.Uniform, Height = 720 };
    private readonly SignText _posterName = new("", 46);
    private readonly TextBlock _posterTitle = Ui.Text("", "eyebrow");
    private readonly DispatcherTimer _rotate;
    private readonly List<(string Label, string Hint, Action Go)> _items = [];
    private int _posterIndex;
    private SignText _logo = null!;
    private Border _stripe = null!;
    private Image _backdrop = null!;
    private StackPanel _caption = null!;
    private CharacterDefinition[] _posterPool = [];

    public MainMenuScreen()
    {
        Build();
        _rotate = new DispatcherTimer(TimeSpan.FromSeconds(4.5), DispatcherPriority.Background, (_, _) => NextPoster());
    }

    private void Build()
    {
        NavItems.Clear();
        _items.Clear();
        _items.Add((Loc.T("menu.fight"), Loc.T("menu.fight.hint"), () => Window.Show(new ModeSelectScreen())));
        _items.Add((Loc.T("menu.story"), Loc.T("menu.story.hint"), () => Window.Show(new StoryScreen())));
        _items.Add((Loc.T("menu.tournament"), Loc.T("menu.tournament.hint"), () => Window.Show(CharacterSelectScreen.ForTournament())));
        _items.Add((Loc.T("menu.survival"), Loc.T("menu.survival.hint"), () => Window.Show(CharacterSelectScreen.ForSurvival())));
        _items.Add((Loc.T("menu.online"), Loc.T("menu.online.hint"), () => Window.Show(new OnlineScreen())));
        _items.Add((Loc.T("menu.training"), Loc.T("menu.training.hint"), () => Window.Show(new CharacterSelectScreen(MatchPlan.For(GameMode.Training)))));
        _items.Add((Loc.T("menu.creator"), Loc.T("menu.creator.hint"), () => Window.Show(new CreatorScreen())));
        _items.Add((Loc.T("menu.collection"), Loc.T("menu.collection.hint"), () => Window.Show(new CollectionScreen())));
        _items.Add((Loc.T("menu.settings"), "", () => Window.Show(new SettingsScreen())));
        _items.Add((Loc.T("menu.credits"), "", () => Window.Show(new CreditsScreen())));
        _items.Add((Loc.T("menu.quit"), "", () => Window.Close()));

        var menu = new StackPanel { Spacing = 6 };
        foreach (var (label, _, go) in _items)
        {
            var b = Ui.Banner(label.ToUpperInvariant(), go);
            menu.Children.Add(b);
            Nav(b);
        }

        _logo = new SignText("TARKAM ARENA", 54);
        var logo = Ui.V(0, _logo);
        var left = Ui.V(20, logo, menu, _hint);
        left.Margin = new Thickness(80, 40, 0, 0);

        var profile = Game.Profile;
        var chip = Ui.Panel(Ui.V(4,
            Ui.Text(profile.Name.ToUpperInvariant(), "h3"),
            Ui.Text($"{Loc.T("collection.level", profile.Level)}  ·  {Loc.T("collection.coins", profile.Coins)}", "mono"),
            Ui.Text($"{Rating.Tier(profile.Rating)}  ·  {profile.Rating}", "mono", "muted")), 16);
        chip.HorizontalAlignment = HorizontalAlignment.Right;
        chip.VerticalAlignment = VerticalAlignment.Top;
        chip.Margin = new Thickness(0, 40, 48, 0);

        var posterCaption = Ui.V(4, _posterTitle, _posterName);
        posterCaption.HorizontalAlignment = HorizontalAlignment.Right;
        posterCaption.VerticalAlignment = VerticalAlignment.Bottom;
        posterCaption.Margin = new Thickness(0, 0, 70, 70);
        _poster.HorizontalAlignment = HorizontalAlignment.Right;
        _poster.VerticalAlignment = VerticalAlignment.Bottom;
        _poster.Margin = new Thickness(0, 0, 160, 0);

        // A tall spanduk hanging behind the poster: the one bold block of colour on the screen.
        var stripe = new Border
        {
            Background = Ui.Spanduk,
            Width = 380,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 220, 0),
            RenderTransform = new SkewTransform(-9, 0),
            Opacity = 0.92,
            Child = new TextBlock
            {
                Text = "TARKAM",
                FontFamily = Ui.Display,
                FontSize = 150,
                Foreground = Ui.Arang,
                Opacity = 0.08,
                RenderTransform = new RotateTransform(-90),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };

        var footer = Footer().With(f =>
        {
            f.Margin = new Thickness(0, 0, 48, 24);
            f.HorizontalAlignment = HorizontalAlignment.Right;
            f.VerticalAlignment = VerticalAlignment.Bottom;
        });

        _stripe = stripe;
        _caption = posterCaption;
        // Painted night-tournament key art far behind everything, dimmed, drifting slowly.
        _backdrop = Ui.Picture(Images.Load("ui/keyart.png", 1600), stretch: Stretch.UniformToFill).With(i => i.Opacity = 0.16);
        var spotlight = new Border
        {
            Width = 900,
            Height = 900,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, -60, -260),
            Background = new RadialGradientBrush
            {
                GradientStops = { new GradientStop(Color.FromArgb(0x70, 0xF6, 0xC2, 0x1B), 0), new GradientStop(Color.FromArgb(0x00, 0xF6, 0xC2, 0x1B), 1) },
            },
            IsHitTestVisible = false,
        };
        var mendung = Ui.Picture(Images.Load("ui/mega-mendung.png", 1600), stretch: Stretch.UniformToFill)
            .With(i => { i.Height = 54; i.VerticalAlignment = VerticalAlignment.Top; i.Opacity = 0.35; i.IsHitTestVisible = false; });

        var root = new Grid { ClipToBounds = true };
        root.Children.Add(_backdrop);
        root.Children.Add(mendung);
        root.Children.Add(spotlight);
        root.Children.Add(stripe);
        root.Children.Add(_poster);
        root.Children.Add(posterCaption);
        root.Children.Add(left);
        root.Children.Add(chip);
        root.Children.Add(footer);
        root.Children.Add(new ParticleField(ParticleStyle.Embers, 60) { Opacity = 0.8 });
        Content = root;

        _posterPool = [.. Roster.All.Where(c => Game.IsUnlocked(c) && Images.Portrait(c) is not null)];
        if (_posterPool.Length > 0) _posterIndex = Random.Shared.Next(_posterPool.Length);
        ShowPoster();
    }

    private void ShowPoster()
    {
        if (_posterPool.Length == 0) return;
        var c = _posterPool[_posterIndex % _posterPool.Length];
        _poster.Source = Images.Portrait(c, 720);
        _posterName.Text = c.Name.ToString().ToUpperInvariant();
        _posterTitle.Text = c.Title.ToString().ToUpperInvariant();
    }

    private void NextPoster()
    {
        _posterIndex++;
        ShowPoster();
        // Each new fighter slides onto the banner from the right with its name stamped below.
        _ = Anim.SlideIn(_poster, 160, 0, 520);
        _ = Anim.Stamp(_caption, 220, 380, 1.6, -4);
    }

    protected override void OnNavChanged(int index)
    {
        _hint.Text = _items[index].Hint;
        _ = Anim.FadeIn(_hint, 0, 220);
    }

    protected override void AnimateEntrance()
    {
        base.AnimateEntrance();
        Anim.Drift(_backdrop, 40, 1.1, 50);
        _ = Anim.Stamp(_logo, 0, 420, 1.8, -5);
        _ = Anim.SlideIn(_stripe, 500, 60, 520, "skewX(-9deg)", -80);
        _ = Anim.SlideIn(_poster, 260, 220, 620);
        _ = Anim.Stamp(_caption, 560, 400, 1.6, -4);
    }

    public override void OnEnter()
    {
        base.OnEnter();
        _rotate.Start();
        Game.Audio.StopMusic();
        _ = Game.Audio.StartMusicAsync(MusicStyle.Gamelan, 21);
    }

    public override void OnLeave() => _rotate.Stop();

    public override void Relocalize()
    {
        int index = NavIndex;
        Build();
        NavIndex = index;
    }

    protected override bool Back() => true;
}
