using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;

namespace TarkamArena.App.Screens;

/// <summary>
/// The pre-fight VERSUS card that doubles as the loading screen: both sides slide in over the
/// painted clash (Rodin art), the VS stamp lands on a brush stroke, the arena banner rises with a
/// tip, and a spanduk progress bar fills while the 3D arena is built.
/// </summary>
public sealed class VersusOverlay : Grid
{
    private static readonly string[] s_tips = Enumerable.Range(0, 12).Select(i => $"tip.{i}").ToArray();

    private readonly Image _splash;
    private readonly List<Control> _left = [];
    private readonly List<Control> _right = [];
    private readonly SignText _vs = new("VS", 230);
    private readonly Image _vsBrush;
    private readonly Border _banner;
    private readonly Border _progressFill = new() { Background = Ui.Spanduk, HorizontalAlignment = HorizontalAlignment.Left, Width = 0 };
    private readonly Border _progressTrack;
    private double _progress;

    public VersusOverlay(IReadOnlyList<CharacterDefinition> fighters, GameMode mode, StageDefinition stage)
    {
        ClipToBounds = true;
        Background = Ui.Night;
        _splash = Ui.Picture(Images.Load("ui/vs-splash.jpg", 1920), stretch: Stretch.UniformToFill);
        Children.Add(_splash);
        Children.Add(new Border { Background = Ui.Brush(0x0A1638, 0x60) });
        Children.Add(new ParticleField(ParticleStyle.SpeedLines, 60) { Opacity = 0.5 });
        Children.Add(new ParticleField(ParticleStyle.Embers, 70));
        Children.Add(Ui.Picture(Images.Load("ui/mega-mendung.png", 1600), stretch: Stretch.UniformToFill).With(i => { i.Height = 56; i.VerticalAlignment = VerticalAlignment.Top; i.Opacity = 0.6; }));

        // Fighters: team 0 on the left, everyone else on the right (free-for-all splits the field).
        var leftPanel = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(30, 0, 0, 120) };
        var rightPanel = new Grid { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 30, 120) };
        int leftCount = 0, rightCount = 0;
        for (int i = 0; i < fighters.Count; i++)
        {
            bool left = mode == GameMode.FreeForAll ? i < (fighters.Count + 1) / 2 : GameModes.TeamOf(mode, i) == 0;
            int index = left ? leftCount++ : rightCount++;
            var c = fighters[i];
            double height = index == 0 ? 640 : 520;
            var portrait = Ui.Picture(Images.Portrait(c, 640), height: height).With(p =>
            {
                p.VerticalAlignment = VerticalAlignment.Bottom;
                p.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                p.Margin = left ? new Thickness(index * 230, 0, 0, 40) : new Thickness(0, 0, index * 230, 40);
                if (!left) p.RenderTransform = new ScaleTransform(-1, 1);
                p.ZIndex = 10 - index;
                p.Tag = index == 0;
            });
            _portraits.Add(portrait);
            var name = new SignText(c.Name.ToString().ToUpperInvariant(), index == 0 ? 54 : 34, left ? Ui.Spanduk : Ui.Kapur);
            var title = Ui.Text(c.Title.ToString().ToUpperInvariant(), "eyebrow").With(t => t.Foreground = left ? Ui.Kapur : Ui.Spanduk);
            var caption = Ui.V(2, title, name).With(v =>
            {
                v.VerticalAlignment = VerticalAlignment.Bottom;
                v.HorizontalAlignment = left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                v.Margin = left ? new Thickness(40 + index * 230, 0, 0, index == 0 ? 0 : 0) : new Thickness(0, 0, 40 + index * 230, 0);
                v.ZIndex = 20;
                if (index > 0) v.IsVisible = false;
            });
            var panel = left ? leftPanel : rightPanel;
            panel.Children.Add(portrait);
            panel.Children.Add(caption);
            (left ? _left : _right).Add(portrait);
            (left ? _left : _right).Add(caption);
        }
        Children.Add(leftPanel);
        Children.Add(rightPanel);

        _vsBrush = Ui.Picture(Images.Load("ui/brush-stroke.png", 1200), 760, 190, Stretch.Fill).With(b =>
        {
            b.HorizontalAlignment = HorizontalAlignment.Center;
            b.VerticalAlignment = VerticalAlignment.Center;
            b.Margin = new Thickness(0, 0, 0, 120);
            b.RenderTransform = new RotateTransform(-8);
        });
        Children.Add(_vsBrush);
        _vs.HorizontalAlignment = HorizontalAlignment.Center;
        _vs.VerticalAlignment = VerticalAlignment.Center;
        _vs.Margin = new Thickness(0, 0, 0, 140);
        Children.Add(_vs);

        var tipKey = s_tips[Random.Shared.Next(s_tips.Length)];
        _progressTrack = new Border { Height = 10, Background = Ui.Brush(0x0A1638, 0xC0), BorderBrush = Ui.Arang, BorderThickness = new Thickness(2), Child = _progressFill, Width = 520 };
        _banner = new Border
        {
            Background = Ui.Spanduk,
            BorderBrush = Ui.Arang,
            BorderThickness = new Thickness(0, 4, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            Padding = new Thickness(60, 14, 60, 16),
            Child = Ui.Columns("Auto,*,Auto",
                Ui.V(0,
                    Ui.Text(stage.Region.ToUpperInvariant(), "eyebrow").With(t => t.Foreground = Ui.Cabai),
                    new TextBlock { Text = stage.Name.ToString().ToUpperInvariant(), FontFamily = Ui.Display, FontSize = 34, Foreground = Ui.Arang }),
                Ui.Wrap($"{Loc.T("loading.tip")} · {Loc.T(tipKey)}", "body").With(t => { t.Foreground = Ui.Arang; t.Margin = new Thickness(40, 4, 40, 0); t.VerticalAlignment = VerticalAlignment.Center; t.MaxWidth = 780; }),
                Ui.V(6, Ui.Text(Loc.T("common.loading").ToUpperInvariant(), "eyebrow").With(t => t.Foreground = Ui.Arang), _progressTrack)
                    .With(v => v.VerticalAlignment = VerticalAlignment.Center)),
        };
        Children.Add(_banner);

        // Portraits scale with the window so heads never leave the frame on short screens.
        SizeChanged += (_, e) =>
        {
            double available = Math.Max(300, e.NewSize.Height - 230);
            foreach (var p in _portraits) p.Height = Math.Min(p.Tag is true ? 640 : 520, available * (p.Tag is true ? 1 : 0.82));
        };
    }

    private readonly List<Image> _portraits = [];

    /// <summary>Plays the arrival sequence. Returns when the card is fully on screen.</summary>
    public async Task PlayAsync()
    {
        Anim.Drift(_splash, 18, 1.1, 30);
        int k = 0;
        foreach (var c in _left) _ = Anim.SlideIn(c, -700, 60 + k++ * 70, 520);
        k = 0;
        foreach (var c in _right) _ = Anim.SlideIn(c, 700, 60 + k++ * 70, 520);
        _ = Anim.SlideIn(_banner, 0, 200, 420, "", 160);
        _ = Anim.SlideIn(_vsBrush, -900, 520, 260);
        Avalonia.Threading.DispatcherTimer.RunOnce(() => Game.Audio.Play(Engine.Audio.Sfx.HitHeavy, 1f, 0), TimeSpan.FromMilliseconds(700));
        Avalonia.Threading.DispatcherTimer.RunOnce(() => Game.Audio.Play(Engine.Audio.Sfx.Whoosh, 0.7f, 0), TimeSpan.FromMilliseconds(80));
        // Animations pause while the window is minimised; never let that stall the match start.
        await Task.WhenAny(Anim.Stamp(_vs, 600, 460, 3.0, -12), Task.Delay(1300));
    }

    public void SetProgress(double value)
    {
        _progress = Math.Clamp(value, 0, 1);
        _progressFill.Width = _progressTrack.Width * _progress - 4;
    }

    /// <summary>Fades and zooms the card away to reveal the arena.</summary>
    public async Task DismissAsync()
    {
        SetProgress(1);
        Opacity = 1;
        var a = new Avalonia.Animation.Animation { Duration = TimeSpan.FromMilliseconds(420), Easing = new Avalonia.Animation.Easings.CubicEaseIn(), FillMode = Avalonia.Animation.FillMode.Forward };
        a.Children.Add(new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(0), Setters = { new Avalonia.Styling.Setter(OpacityProperty, 1d), new Avalonia.Styling.Setter(RenderTransformProperty, Avalonia.Media.Transformation.TransformOperations.Parse("scale(1)")) } });
        a.Children.Add(new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(1), Setters = { new Avalonia.Styling.Setter(OpacityProperty, 0d), new Avalonia.Styling.Setter(RenderTransformProperty, Avalonia.Media.Transformation.TransformOperations.Parse("scale(1.15)")) } });
        await Task.WhenAny(a.RunAsync(this), Task.Delay(700));
        IsVisible = false;
    }
}
