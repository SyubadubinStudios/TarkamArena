using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.App.Screens;

/// <summary>
/// Film-style end credits: slow aerial drift across the painted archipelago, wayang shadow puppets
/// at the edges, a gamelan-orchestral score, and the closing line.
/// </summary>
public sealed class CreditsScreen : Screen
{
    private readonly Image _landscapeA = new() { Stretch = Stretch.UniformToFill };
    private readonly Image _landscapeB = new() { Stretch = Stretch.UniformToFill, Opacity = 0 };
    private readonly StackPanel _roll = new() { Spacing = 14, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly TranslateTransform _scroll = new();
    private readonly ScaleTransform _zoomA = new(1.08, 1.08), _zoomB = new(1.08, 1.08);
    private readonly DispatcherTimer _timer;
    private readonly StageDefinition[] _stages = [.. Stages.All];
    private double _time;
    private int _stageIndex;

    public CreditsScreen()
    {
        var fade = new Avalonia.Animation.Transitions { new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromSeconds(1.6) } };
        _landscapeA.Transitions = fade;
        _landscapeB.Transitions = [new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromSeconds(1.6) }];
        _landscapeA.RenderTransform = _zoomA;
        _landscapeB.RenderTransform = _zoomB;
        _landscapeA.Source = Images.Backdrop(_stages[0], 1920);

        void Heading(string text) => _roll.Children.Add(Ui.Text(text.ToUpperInvariant(), "eyebrow").With(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.Margin = new Thickness(0, 30, 0, 0); }));
        void Name(string text, double size = 30) => _roll.Children.Add(new TextBlock { Text = text, FontFamily = Ui.Display, FontSize = size, Foreground = Ui.Kapur, HorizontalAlignment = HorizontalAlignment.Center, TextAlignment = TextAlignment.Center });
        void Body(string text) => _roll.Children.Add(Ui.Wrap(text, "body").With(t => { t.MaxWidth = 760; t.TextAlignment = TextAlignment.Center; t.HorizontalAlignment = HorizontalAlignment.Center; }));

        _roll.Children.Add(new SignText("TARKAM ARENA", 96).With(s => s.HorizontalAlignment = HorizontalAlignment.Center));
        Body(Loc.T("app.tagline"));
        Heading(Loc.T("credits.director"));
        Name("Ariana Mischa Fadhila", 52);
        Heading(Loc.T("credits.studio"));
        Name("Syubadubin Studios", 40);
        Heading(Loc.Current == Language.Indonesian ? "Para petarung" : "The fighters");
        foreach (var group in Roster.All.Where(c => c.Dlc == DlcPack.None).Chunk(3))
            Body(string.Join("   ·   ", group.Select(c => $"{c.Name} — {c.Title}")));
        Heading(Loc.Current == Language.Indonesian ? "Arena" : "Arenas");
        foreach (var group in Stages.All.Chunk(3)) Body(string.Join("   ·   ", group.Select(s => s.Name.ToString())));
        Heading(Loc.T("credits.tech"));
        Body(".NET 10 · Avalonia UI · ThreeNet (Vibe_Graphics) · BepuPhysics · LiteNetLib · SignalR · SQLite · PostgreSQL");
        Heading(Loc.Current == Language.Indonesian ? "Pipeline aset" : "Asset pipeline");
        Body("Concept Art → Rodin MCP → Blender MCP → Optimization → ThreeNet → Build");
        Heading(Loc.T("credits.music"));
        Body(Loc.Current == Language.Indonesian
            ? "Orkestra gamelan hibrida, angklung, degung Sunda, talempong, sasando, baleganjur, reog, dangdut koplo, punk, metal, EDM dan sound horeg — disintesis prosedural di dalam game."
            : "Hybrid gamelan orchestra, angklung, Sundanese degung, talempong, sasando, baleganjur, reog, dangdut koplo, punk, metal, EDM and sound horeg — synthesised procedurally in-game.");
        Heading(Loc.T("credits.thanks"));
        Body(Loc.T("credits.thanks.body"));
        Body("Fonts: Bungee (David Jonathan Ross), Barlow Semi Condensed (Jeremy Tribby), IBM Plex Mono (IBM) — SIL Open Font License.");
        _roll.Children.Add(new Border { Height = 240 });
        _roll.Children.Add(new SignText(Loc.T("app.closing").ToUpperInvariant(), 44, Ui.Kapur).With(s => { s.HorizontalAlignment = HorizontalAlignment.Center; s.MaxWidth = 1100; }));
        _roll.Children.Add(new Border { Height = 600 });
        _roll.RenderTransform = _scroll;

        var veil = new Border { Background = Ui.Brush(0x0A1638, 0xA8) };
        var wayangLeft = Ui.Picture(Images.Load("ui/wayang-ksatria.png", 600), height: 760).With(i => { i.HorizontalAlignment = HorizontalAlignment.Left; i.VerticalAlignment = VerticalAlignment.Bottom; i.Margin = new Thickness(-40, 0, 0, -60); i.Opacity = 0.9; });
        var wayangRight = Ui.Picture(Images.Load("ui/wayang-gunungan.png", 600), height: 820).With(i => { i.HorizontalAlignment = HorizontalAlignment.Right; i.VerticalAlignment = VerticalAlignment.Bottom; i.Margin = new Thickness(0, 0, -60, -80); i.Opacity = 0.9; });

        var root = new Grid { ClipToBounds = true };
        root.Children.Add(_landscapeA);
        root.Children.Add(_landscapeB);
        root.Children.Add(veil);
        root.Children.Add(_roll.With(r => r.VerticalAlignment = VerticalAlignment.Top));
        root.Children.Add(wayangLeft);
        root.Children.Add(wayangRight);
        Content = root;

        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Render, (_, _) => Tick(1 / 60.0));
        PointerPressed += (_, _) => Window.Back();
    }

    private void Tick(double dt)
    {
        _time += dt;
        double height = Bounds.Height;
        _scroll.Y = height - _time * 46;
        if (-_scroll.Y > _roll.Bounds.Height) _time = 0;

        // Every 9 seconds the "drone" cuts to the next island, with a cross-fade and a slow push in.
        double phase = _time % 9;
        double zoom = 1.08 + phase * 0.012;
        _zoomA.ScaleX = _zoomA.ScaleY = zoom;
        _zoomB.ScaleX = _zoomB.ScaleY = zoom;
        if (phase < dt && _time > 1)
        {
            _stageIndex = (_stageIndex + 1) % _stages.Length;
            bool aShown = _landscapeA.Opacity > 0.5;
            var next = aShown ? _landscapeB : _landscapeA;
            var current = aShown ? _landscapeA : _landscapeB;
            next.Source = Images.Backdrop(_stages[_stageIndex], 1920);
            next.Opacity = 1;
            current.Opacity = 0;
        }
    }

    public override void OnEnter()
    {
        _timer.Start();
        Game.Audio.StopMusic();
        _ = Game.Audio.StartMusicAsync(MusicStyle.OrchestralHybrid, 99);
    }

    public override void OnLeave()
    {
        _timer.Stop();
        Game.Audio.StopMusic();
    }

    public override bool HandleAction(MenuAction action)
    {
        if (action is MenuAction.Back or MenuAction.Confirm) Window.Back();
        return true;
    }
}
