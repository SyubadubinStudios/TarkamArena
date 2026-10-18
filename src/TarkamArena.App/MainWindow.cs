using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Screens;
using TarkamArena.App.Services;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;

namespace TarkamArena.App;

/// <summary>
/// The single game window. It hosts one screen at a time with a back stack, translates keyboard
/// and gamepad input into menu actions, and shows toasts (achievements, unlocks).
/// </summary>
public sealed class MainWindow : Window
{
    private readonly ContentControl _host = new();
    private readonly StackPanel _toasts = new() { Spacing = 8, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Margin = new Thickness(0, 24, 24, 0) };
    private readonly Stack<Screen> _stack = new();
    private readonly DispatcherTimer _padTimer;
    private InputButtons _lastPad;
    private int _padRepeat;
    private readonly Grid _wipe = new() { IsHitTestVisible = false, IsVisible = false, ClipToBounds = true };
    private readonly Border[] _bars;
    private bool _wiping, _dirty;

    public Screen? Current => _stack.Count > 0 ? _stack.Peek() : null;

    public MainWindow()
    {
        Title = "Tarkam Arena";
        Width = 1600;
        Height = 900;
        MinWidth = 1280;
        MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Ui.Night;
        if (Images.Load("ui/icon.png") is { } icon) Icon = new WindowIcon(icon);

        var root = new Grid();
        root.Children.Add(new Border { Background = Ui.TarpBackground() });
        root.Children.Add(_host);
        // Screen transition: three slanted banner strips sweep across, like spanduk being unrolled.
        _bars =
        [
            new Border { Background = Ui.Terpal },
            new Border { Background = Ui.Cabai },
            new Border
            {
                Background = Ui.Spanduk,
                Child = new TextBlock
                {
                    Text = "TARKAM ARENA", FontFamily = Ui.Display, FontSize = 64, Foreground = Ui.Arang, Opacity = 0.9,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                },
            },
        ];
        foreach (var bar in _bars)
        {
            bar.Margin = new Thickness(-400, -100);
            _wipe.Children.Add(bar);
        }
        root.Children.Add(_wipe);
        root.Children.Add(_toasts);
        Content = root;

        if (LaunchOptions.Fullscreen || Game.Settings.Fullscreen) WindowState = WindowState.FullScreen;

        _padTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Input, (_, _) => PollGamepad());
        _padTimer.Start();

        Loc.LanguageChanged += () =>
        {
            foreach (var s in _stack) s.Relocalize();
        };

        Opened += (_, _) =>
        {
            // Small screens (laptops at 125% scaling) get a maximised window instead of an overflowing one.
            if (Screens.ScreenFromWindow(this) is { } screen && WindowState != WindowState.FullScreen)
            {
                var area = screen.WorkingArea.Size.ToSize(screen.Scaling);
                if (area.Width < Width + 40 || area.Height < Height + 60) WindowState = WindowState.Maximized;
            }
            Show(new TitleScreen());
            if (LaunchOptions.CaptureDirectory is { } dir) _ = ScreenshotTour.RunAsync(this, dir);
            else if (LaunchOptions.QuickFight) ScreenshotTour.QuickFight(this);
        };
        Closing += (_, _) => Game.Save();
        Deactivated += (_, _) =>
        {
            Game.Input.ReleaseAll();
            if (Current is FightScreen fight && LaunchOptions.CaptureDirectory is null && LaunchOptions.SnapDirectory is null) fight.AutoPause();
        };
    }

    // ───────────────────────── navigation ─────────────────────────

    public void Show(Screen screen)
    {
        Current?.OnLeave();
        screen.Window = this;
        _stack.Push(screen);
        Present();
    }

    public void Replace(Screen screen)
    {
        if (_stack.Count > 0)
        {
            var old = _stack.Pop();
            old.OnLeave();
        }
        Show(screen);
    }

    public void Back()
    {
        if (_stack.Count <= 1) return;
        var old = _stack.Pop();
        old.OnLeave();
        Present();
    }

    /// <summary>Clears the stack back to the main menu.</summary>
    public void Home()
    {
        while (_stack.Count > 0)
        {
            var s = _stack.Pop();
            s.OnLeave();
        }
        Show(new MainMenuScreen());
    }

    /// <summary>Puts the top of the stack on screen behind a banner wipe. Navigation during a wipe is coalesced.</summary>
    private void Present()
    {
        if (_wiping)
        {
            _dirty = true;
            return;
        }
        _ = PresentAsync();
    }

    private async Task PresentAsync()
    {
        _wiping = true;
        try
        {
            do
            {
                _dirty = false;
                bool animate = _host.Content is not null && !Controls.Anim.Reduced && Bounds.Width > 0;
                if (animate) await SweepAsync(cover: true);
                var screen = Current;
                if (!ReferenceEquals(_host.Content, screen))
                {
                    _host.Content = screen;
                    if (screen is not null) Dispatcher.UIThread.Post(screen.OnEnter, DispatcherPriority.Loaded);
                }
                if (animate) await SweepAsync(cover: false);
            }
            while (_dirty);
        }
        finally
        {
            _wiping = false;
        }
    }

    private async Task SweepAsync(bool cover)
    {
        double w = Bounds.Width + 800;
        _wipe.IsVisible = true;
        var tasks = new List<Task>(3);
        for (int i = 0; i < _bars.Length; i++)
        {
            int order = cover ? i : _bars.Length - 1 - i;
            double from = cover ? -w : 0, to = cover ? 0 : w;
            var a = new Avalonia.Animation.Animation
            {
                Duration = TimeSpan.FromMilliseconds(cover ? 210 : 260),
                Delay = TimeSpan.FromMilliseconds(order * 45),
                Easing = cover ? new Avalonia.Animation.Easings.CubicEaseIn() : new Avalonia.Animation.Easings.CubicEaseOut(),
                FillMode = Avalonia.Animation.FillMode.Both,
            };
            a.Children.Add(new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(0), Setters = { new Avalonia.Styling.Setter(RenderTransformProperty, Avalonia.Media.Transformation.TransformOperations.Parse($"skewX(-14deg) translateX({from}px)")) } });
            a.Children.Add(new Avalonia.Animation.KeyFrame { Cue = new Avalonia.Animation.Cue(1), Setters = { new Avalonia.Styling.Setter(RenderTransformProperty, Avalonia.Media.Transformation.TransformOperations.Parse($"skewX(-14deg) translateX({to}px)")) } });
            tasks.Add(a.RunAsync(_bars[i]));
        }
        if (cover) Game.Audio.Play(Engine.Audio.Sfx.Whoosh, 0.35f, 0.05f);
        await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(700));
        if (!cover) _wipe.IsVisible = false;
    }

    public void ToggleFullscreen()
    {
        WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        Game.Settings.Fullscreen = WindowState == WindowState.FullScreen;
    }

    // ───────────────────────── input ─────────────────────────

    protected override void OnKeyDown(KeyEventArgs e)
    {
        Game.Input.KeyDown(e.Key.ToString());
        if (e.Key == Key.F11 || (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Alt)))
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        var screen = Current;
        if (screen is null) return;
        screen.OnKey(e);
        if (e.Handled || screen.WantsRawInput) return;
        if (FocusManager?.GetFocusedElement() is TextBox && e.Key is not (Key.Escape or Key.Enter)) return;

        MenuAction? action = e.Key switch
        {
            Key.Up or Key.W => MenuAction.Up,
            Key.Down or Key.S => MenuAction.Down,
            Key.Left or Key.A => MenuAction.Left,
            Key.Right or Key.D => MenuAction.Right,
            Key.Enter or Key.Space or Key.J => MenuAction.Confirm,
            Key.Escape or Key.Back or Key.K => MenuAction.Back,
            Key.Q or Key.PageUp => MenuAction.PreviousTab,
            Key.E or Key.PageDown or Key.Tab => MenuAction.NextTab,
            _ => null,
        };
        if (action is { } a && screen.HandleAction(a)) e.Handled = true;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        Game.Input.KeyUp(e.Key.ToString());
        base.OnKeyUp(e);
    }


    private void PollGamepad()
    {
        var screen = Current;
        if (screen is null || screen.WantsRawInput) return;
        Game.Input.Poll();
        var b = Game.Input.Read(0).Buttons & ~InputButtons.None;
        // Only gamepad-driven navigation here; keyboard is handled by key events.
        if (Game.Input.ConnectedGamepads == 0) return;
        var pressed = b & ~_lastPad;
        bool held = b != 0 && b == _lastPad && ++_padRepeat > 22 && _padRepeat % 6 == 0;
        _lastPad = b;
        if (b == 0) _padRepeat = 0;
        var test = held ? b : pressed;
        if (test == 0) return;
        if ((test & InputButtons.Up) != 0) screen.HandleAction(MenuAction.Up);
        else if ((test & InputButtons.Down) != 0) screen.HandleAction(MenuAction.Down);
        else if ((test & InputButtons.Left) != 0) screen.HandleAction(MenuAction.Left);
        else if ((test & InputButtons.Right) != 0) screen.HandleAction(MenuAction.Right);
        else if ((test & InputButtons.Special) != 0) screen.HandleAction(MenuAction.Confirm);
        else if ((test & InputButtons.Grab) != 0) screen.HandleAction(MenuAction.Back);
        else if ((test & InputButtons.Block) != 0) screen.HandleAction(MenuAction.NextTab);
        else if ((test & InputButtons.Dodge) != 0) screen.HandleAction(MenuAction.PreviousTab);
    }

    // ───────────────────────── toasts ─────────────────────────

    public void Toast(string eyebrow, string text, Bitmap? image = null)
    {
        var content = Ui.H(14,
            image is null ? new Border { Width = 6, Background = Ui.Cabai } : Ui.Picture(image, 56, 56),
            Ui.V(2, Ui.Text(eyebrow.ToUpperInvariant(), "eyebrow"), Ui.Text(text, "h3")));
        var toast = new Border
        {
            Child = content,
            Background = Ui.PanelFill,
            BorderBrush = Ui.Spanduk,
            BorderThickness = new Thickness(0, 0, 0, 3),
            Padding = new Thickness(16, 12),
            Opacity = 0,
            Transitions = [new Avalonia.Animation.DoubleTransition { Property = OpacityProperty, Duration = TimeSpan.FromMilliseconds(220) }],
        };
        _toasts.Children.Insert(0, toast);
        Dispatcher.UIThread.Post(() => toast.Opacity = 1, DispatcherPriority.Background);
        DispatcherTimer.RunOnce(() => toast.Opacity = 0, TimeSpan.FromSeconds(4));
        DispatcherTimer.RunOnce(() => _toasts.Children.Remove(toast), TimeSpan.FromSeconds(4.4));
    }

    public void ToastRewards(Core.Progression.RewardSummary reward)
    {
        foreach (var a in reward.NewAchievements) Toast(Loc.T("collection.achievements"), a.Name.ToString());
        foreach (var u in reward.NewUnlocks)
        {
            var c = u.CharacterId is { } id && Roster.TryGet(id, out var ch) ? ch : null;
            Toast(Loc.T("result.unlocked", ""), u.Name.ToString(), c is null ? null : Images.Portrait(c, 128));
        }
    }
}
