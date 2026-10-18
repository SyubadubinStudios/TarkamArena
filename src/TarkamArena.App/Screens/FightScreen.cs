using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.AI;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Progression;
using TarkamArena.Core.Settings;
using TarkamArena.Engine.Rendering;
using TarkamArena.Persistence;
using ThreeNet;
using ThreeNet.Avalonia;

namespace TarkamArena.App.Screens;

/// <summary>
/// Runs a match: the deterministic simulation on a fixed 60 Hz step, the ThreeNet view rendering
/// at display rate, and the Avalonia HUD on top. Local players, CPU brains and an online rollback
/// session all feed the same <see cref="MatchSimulation"/>.
/// </summary>
public sealed class FightScreen : Screen
{
    private const double Step = 1.0 / 60.0;

    private readonly MatchPlan _plan;
    private readonly ThreeNetView _view = new() { MaxFramesPerSecond = 60 };
    private readonly Grid _hudLayer = new();
    private readonly VersusOverlay _versus;
    private bool _holdSimulation = true;
    private double _slowMotion;
    private readonly Border _pause;
    private readonly StackPanel _pauseMenu = new() { Spacing = 10 };
    private readonly ContentControl _pauseMoves = new() { MaxWidth = 760 };
    private MatchContext? _context;
    private ArenaPresenter? _presenter;
    private FightHud? _hud;
    private MatchState _state;
    private CpuBrain?[] _brains = [];
    private InputFrame[] _inputs = [];
    private MatchStatsTracker? _tracker;
    private double _accumulator;
    private bool _paused;
    private bool _finished;
    private int _pauseIndex;
    private int _localSlot;

    public override bool WantsRawInput => !_paused;

    /// <summary>Receives the local fighter's remaining health (0..1) when the match ends (survival carries it over).</summary>
    public Action<double>? HealthReporter { get; init; }

    public FightScreen(MatchPlan plan)
    {
        _plan = plan;
        _versus = new VersusOverlay([.. plan.Fighters.Select(f => f ?? Core.Content.Roster.Get("pesilat"))], plan.Mode, plan.Stage ?? Core.Content.Stages.All[0]);

        _pause = new Border
        {
            Background = Ui.Brush(0x0A1638, 0xD8),
            IsVisible = false,
            Child = Ui.V(20, new SignText(Loc.T("common.paused").ToUpperInvariant(), 72), _pauseMenu, _pauseMoves, ControlsHelp())
                .With(s => { s.HorizontalAlignment = HorizontalAlignment.Center; s.VerticalAlignment = VerticalAlignment.Center; }),
        };
        _pauseMenu.Children.Add(Ui.Banner(Loc.T("common.resume").ToUpperInvariant(), () => SetPaused(false)));
        if (plan.Online is null) _pauseMenu.Children.Add(Ui.Banner(Loc.T("common.rematch").ToUpperInvariant(), Rematch));
        _pauseMenu.Children.Add(Ui.Banner(Loc.T("common.mainMenu").ToUpperInvariant(), () => Window.Home()));

        var root = new Grid();
        root.Children.Add(_view);
        root.Children.Add(_hudLayer);
        root.Children.Add(_pause);
        root.Children.Add(_versus);
        Content = root;
        _view.Frame += OnFrame;
        _view.RenderFailed += (_, message) => Window.Toast("ThreeNet", message);
    }

    private static Control ControlsHelp()
    {
        var keys = Game.Settings.KeyBindings[0];
        string Key(InputButtons b) => keys.FirstOrDefault(kv => kv.Value == b).Key ?? "-";
        var text = string.Join("   ", new[]
        {
            $"{Key(InputButtons.Left)}/{Key(InputButtons.Right)} {Loc.T("controls.move")}",
            $"{Key(InputButtons.Up)} {Loc.T("controls.jump")}",
            $"{Key(InputButtons.Down)} {Loc.T("controls.crouch")}",
            $"{Key(InputButtons.Light)} {Loc.T("controls.light")}",
            $"{Key(InputButtons.Heavy)} {Loc.T("controls.heavy")}",
            $"{Key(InputButtons.Grab)} {Loc.T("controls.grab")}",
            $"{Key(InputButtons.Block)} {Loc.T("controls.block")}",
            $"{Key(InputButtons.Special)} {Loc.T("controls.special")}",
            $"{Key(InputButtons.Dodge)} {Loc.T("controls.dodge")}",
            $"{Key(InputButtons.Tag)} {Loc.T("controls.tag")}",
            $"{Key(InputButtons.Rage)} {Loc.T("controls.rage")}",
        });
        return Ui.Wrap(text, "mono", "muted").With(t => { t.MaxWidth = 900; t.TextAlignment = Avalonia.Media.TextAlignment.Center; });
    }

    public override void OnEnter()
    {
        if (_presenter is not null) return;
        Game.Audio.StopMusic();
        _ = StartAsync();
    }

    /// <summary>VS card in, arena built behind it, a short beat, then the card wipes away and the fight begins.</summary>
    private async Task StartAsync()
    {
        try
        {
            await StartCoreAsync();
        }
        catch (Exception e)
        {
            CrashLog.Write(e);
            Window.Toast("Error", e.Message);
            _versus.IsVisible = false;
            _holdSimulation = false;
        }
    }


    private async Task StartCoreAsync()
    {
        LoadTrace.Mark("fight: start");
        await _versus.PlayAsync();
        LoadTrace.Mark("fight: versus card shown");
        _versus.SetProgress(0.3);
        await Task.Delay(120);
        Load();
        LoadTrace.Mark("fight: scene built");
        _versus.SetProgress(0.85);
        await Task.Delay(500);
        // Start the 3D view only as the card starts to leave: rendering a view that is fully
        // covered stalls ThreeNetView's GPU readback on some drivers.
        StartRendering();
        LoadTrace.Mark("fight: rendering started");
        await _versus.DismissAsync();
        LoadTrace.Mark("fight: versus card dismissed");
        // The opening "Round 1" event is part of the initial state; present it now that the card is gone.
        _presenter?.OnSimulated(_state);
        _holdSimulation = false;
    }

    private void Load()
    {
        var setup = _plan.ToSetup();
        _context = new MatchContext(setup);
        _state = _plan.Online is { } session ? session.State : MatchSimulation.CreateInitialState(_context);

        var quality = Game.Settings.Quality;
        _presenter = new ArenaPresenter(_context, new PresenterOptions(Weather: Game.Settings.DynamicWeather, LowQuality: quality == GraphicsQuality.Low))
        {
            Audio = Game.Audio,
        };
        _view.RendererOptions = RendererFor(quality);
        _view.RenderScale = quality switch { GraphicsQuality.Low => 0.6, GraphicsQuality.Medium => 0.8, _ => 1.0 };
        _view.Scene = _presenter.Scene;
        _view.Camera = _presenter.Camera;

        int n = _context.Characters.Length;
        _inputs = new InputFrame[n];
        _brains = new CpuBrain?[n];
        for (int i = 0; i < n; i++)
            if (_plan.Controllers[i] == SlotController.Cpu && _plan.Mode != GameMode.Training)
                _brains[i] = new CpuBrain(i, _plan.Difficulty, setup.Seed + (uint)i * 31);

        _localSlot = _plan.Online?.LocalSlot ?? Array.IndexOf(_plan.Controllers, SlotController.Player1);
        if (_localSlot < 0) _localSlot = 0;
        _tracker = new MatchStatsTracker(_localSlot, GameModes.TeamOf(_context.Rules.Mode, _localSlot));

        _hud = new FightHud(_context, _localSlot);
        _pauseMoves.Content = Ui.Panel(Ui.V(10,
            Ui.Text($"{Loc.T("moves.signatures").ToUpperInvariant()} · {_context.Characters[_localSlot].Name}", "eyebrow").With(t => t.Foreground = Ui.Spanduk),
            SignatureList.Build(_context.Characters[_localSlot], compact: true)), 16);
        _hudLayer.Children.Clear();
        _hudLayer.Children.Add(_hud);
        _presenter.EventRaised += e =>
        {
            _hud.OnEvent(e, _state);
            if (e.Kind == MatchEventKind.SignatureStart) SignatureShown?.Invoke();
            // The match-deciding blow plays in slow motion (offline only: online peers must stay in step).
            if (e.Kind == MatchEventKind.KO && _plan.Online is null) _slowMotion = 1.2;
        };

        _ = Game.Audio.StartMusicAsync(_context.Stage.Music, (int)Core.Mathematics.DeterministicRandom.Hash(_context.Stage.Id));
    }

    private DispatcherTimer? _frameTimer;

    /// <summary>
    /// Drives the 3D view from a Background-priority timer instead of the view's own render loop.
    /// When a frame takes longer than the display interval (integrated GPUs, large windows), the
    /// built-in loop queues frames back to back above input priority and the window stops
    /// responding; at Background priority input, timers and animations always run first.
    /// </summary>
    private void StartRendering()
    {
        _view.IsRendering = false;
        _frameTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(15), DispatcherPriority.Background, (_, _) =>
        {
            if (_presenter is not null) _view.RenderFrame();
        });
        _frameTimer.Start();
    }

    private static RendererOptions RendererFor(GraphicsQuality q)
    {
        var o = OffscreenCapture.Options(1280, 720, q >= GraphicsQuality.High);
        o.VSync = true;
        o.MsaaSamples = q >= GraphicsQuality.High ? 4 : 1;
        o.ShadowMapSize = q switch { GraphicsQuality.Low => 1024, GraphicsQuality.Medium => 1536, GraphicsQuality.High => 2048, _ => 4096 };
        o.ShadowSoftness = q >= GraphicsQuality.High ? 2 : 1;
        o.Ssao = q >= GraphicsQuality.High;
        o.Ssr = q == GraphicsQuality.Ultra;
        o.Bloom = q >= GraphicsQuality.Medium;
        o.MotionBlur = Game.Settings.MotionBlur && q >= GraphicsQuality.Medium;
        o.MotionBlurStrength = 0.35f;
        o.Fxaa = q < GraphicsQuality.High;
        o.Smaa = q >= GraphicsQuality.High;
        return o;
    }

    private int _framesSeen;

    private void OnFrame(object? sender, FrameEventArgs e)
    {
        if (_presenter is null || _context is null) return;
        if (_framesSeen++ < 3) LoadTrace.Mark($"fight: frame {_framesSeen}");
        if (!_holdSimulation && _framesSeen > 3) _ready.TrySetResult();
        double dt = Math.Min(e.DeltaSeconds, 0.25);
        if (_slowMotion > 0)
        {
            _slowMotion -= dt;
            dt *= 0.3;
        }
        if (!_paused && !_finished && !_holdSimulation)
        {
            _accumulator += dt;
            int steps = 0;
            while (_accumulator >= Step && steps++ < 5)
            {
                Simulate();
                _accumulator -= Step;
            }
        }
        if (_presenter is null) return;
        _presenter.Render(_state, (float)(_paused ? 0 : dt));
        _hud?.Update(_state, dt);
    }

    private void Simulate()
    {
        var context = _context!;
        Game.Input.Poll();
        if (_plan.Online is { } session)
        {
            if (!session.Tick(Game.Input.Read(0))) return;
            _state = session.State;
            if (session.Stats.Desynced) Window.Toast("Online", "Desync detected");
        }
        else
        {
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = _plan.Controllers[i] switch
                {
                    SlotController.Player1 => Game.Input.Read(0),
                    SlotController.Player2 => Game.Input.Read(1),
                    _ => _brains[i]?.Think(_state, context) ?? InputFrame.Neutral,
                };
            }
            if (LaunchOptions.SignatureDemo || FillJurus)
                for (int i = 0; i < _state.FighterCount; i++) _state.Fighters[i].Jurus = MatchSimulation.MeterMax;
            MatchSimulation.Step(ref _state, context, _inputs);
        }
        _presenter!.OnSimulated(_state);
        _tracker!.Observe(_state, context);

        if (_state.IsOver && _state.PhaseFrame > 170) Finish();
    }

    private void Finish()
    {
        if (_finished) return;
        _finished = true;
        var context = _context!;
        var summary = _tracker!.Build(_state, context);
        ref readonly var me = ref _state.Fighters[_localSlot];
        HealthReporter?.Invoke(me.MaxHealth > 0 ? me.Health / (double)me.MaxHealth : 0);
        var reward = ProgressionService.Apply(Game.Profile, summary);
        string opponent = context.Characters.Where((_, i) => GameModes.TeamOf(context.Rules.Mode, i) != GameModes.TeamOf(context.Rules.Mode, _localSlot)).Select(c => c.Id).FirstOrDefault() ?? "";
        Game.Database.RecordMatch(new MatchRecord(DateTimeOffset.UtcNow, context.Rules.Mode.ToString(), summary.CharacterId, opponent, summary.Won, summary.MaxCombo, summary.Finisher?.ToString()));
        Game.Save();
        Window.ToastRewards(reward);

        // Navigate after the current frame so the scene is not disposed while it is rendering.
        var fighter = context.Characters[_localSlot];
        Dispatcher.UIThread.Post(() =>
        {
            if (_plan.OnFinished?.Invoke(summary, reward) == true) return;
            Window.Replace(new ResultScreen(_plan, summary, reward, fighter));
        });
    }

    private void Rematch()
    {
        _plan.Seed++;
        Window.Replace(new FightScreen(_plan));
    }

    public override void OnKey(KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape || e.Key == Avalonia.Input.Key.P)
        {
            SetPaused(!_paused);
            e.Handled = true;
        }
    }

    /// <summary>Called when the window loses focus: offline matches pause so nobody loses a round while alt-tabbed.</summary>
    public void AutoPause()
    {
        if (!_paused && !_finished && !_holdSimulation && _plan.Online is null) SetPaused(true);
    }

    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once the loading card is gone and the arena has rendered a few frames.</summary>
    public Task Ready => _ready.Task;

    /// <summary>Screenshot tour: every fighter's Jurus meter stays full so the CPUs throw signatures.</summary>
    public bool FillJurus { get; set; }

    /// <summary>Raised when a signature cut-in starts (used by the screenshot tour).</summary>
    public event Action? SignatureShown;

    /// <summary>Screenshot tour: opens the pause menu (with the move list) as a player would.</summary>
    public void PauseForCapture() => SetPaused(true);

    private void SetPaused(bool paused)
    {
        if (_plan.Online is not null && paused) return; // Online matches cannot pause.
        _paused = paused;
        _pause.IsVisible = paused;
        if (paused)
        {
            NavItems.Clear();
            foreach (var c in _pauseMenu.Children) Nav(c);
            NavIndex = _pauseIndex = 0;
            FocusCurrent();
        }
        else
        {
            Game.Input.ReleaseAll();
        }
    }

    public override bool HandleAction(MenuAction action)
    {
        if (!_paused) return false;
        if (action == MenuAction.Back)
        {
            SetPaused(false);
            return true;
        }
        _ = _pauseIndex;
        return base.HandleAction(action);
    }

    public override void OnLeave()
    {
        _frameTimer?.Stop();
        _view.IsRendering = false;
        _view.Frame -= OnFrame;
        _view.Scene = null!;
        _presenter?.Dispose();
        _presenter = null;
        _plan.OnlineTransport?.Dispose();
        Game.Audio.StopMusic();
    }
}
