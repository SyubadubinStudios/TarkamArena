using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Combat;
using TarkamArena.Core.Content;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Match;
using TarkamArena.Core.Modes;

namespace TarkamArena.App.Screens;

/// <summary>
/// Character select: category tabs, a grid of fighter "tickets", and a poster with bio, stats,
/// special moves and finisher inputs for the fighter under the cursor. Slots are picked in order;
/// CPU slots can be picked or randomised.
/// </summary>
public sealed class CharacterSelectScreen : Screen
{
    private const int Columns = 5;

    private static readonly CharacterCategory[] s_tabs =
    [
        CharacterCategory.Profession, CharacterCategory.Mystic, CharacterCategory.Wildlife,
        CharacterCategory.Society, CharacterCategory.Secret, CharacterCategory.Custom,
    ];

    private readonly MatchPlan _plan;
    private readonly Action<CharacterDefinition>? _onPicked;
    private readonly UniformGrid _grid = new() { Columns = Columns };
    private readonly WrapPanel _tabs = new() { Orientation = Orientation.Horizontal, ItemSpacing = 6, LineSpacing = 6 };
    private readonly StackPanel _slots = new() { Spacing = 10 };
    private readonly Image _portrait = new() { Height = 560, Stretch = Stretch.Uniform };
    private readonly SignText _name = new("", 46);
    private readonly TextBlock _title = Ui.Text("", "eyebrow");
    private readonly TextBlock _origin = Ui.Text("", "muted");
    private readonly TextBlock _bio = Ui.Wrap("", "body").With(t => t.MaxWidth = 470);
    private readonly StatBar[] _stats = [new(), new(), new(), new()];
    private readonly TextBlock _specials = Ui.Wrap("", "body");
    private readonly ContentControl _signatures = new();
    private readonly TextBlock _finishers = Ui.Wrap("", "mono").With(t => t.LineHeight = 22);
    private readonly TextBlock _picking = Ui.Text("", "h3");
    private List<CharacterDefinition> _visible = [];
    private int _tab;
    private int _slot;
    private CharacterDefinition? _hover;

    public CharacterSelectScreen(MatchPlan plan, Action<CharacterDefinition>? onPicked = null)
    {
        _plan = plan;
        _onPicked = onPicked;
        Build();
    }

    public static CharacterSelectScreen ForTournament() =>
        new(MatchPlan.For(GameMode.Tournament), c => App.CurrentWindow?.Replace(new TournamentScreen(new Tournament(c, Game.PlayableRoster(), (uint)Environment.TickCount))));

    public static CharacterSelectScreen ForSurvival() =>
        new(MatchPlan.For(GameMode.Survival), c => App.CurrentWindow?.Replace(new SurvivalScreen(new SurvivalRun(c, (uint)Environment.TickCount))));

    private void Build()
    {
        for (int i = 0; i < s_tabs.Length; i++)
        {
            int index = i;
            var chip = Ui.Chip(TabName(s_tabs[i]).ToUpperInvariant(), () => SetTab(index));
            _tabs.Children.Add(chip);
        }

        var statsGrid = new Grid { ColumnDefinitions = ColumnDefinitions.Parse("110,*"), RowDefinitions = RowDefinitions.Parse("Auto,Auto,Auto,Auto"), RowSpacing = 8 };
        string[] labels = ["select.stats.power", "select.stats.speed", "select.stats.defense", "select.stats.technique"];
        for (int i = 0; i < 4; i++)
        {
            var l = Ui.Text(Loc.T(labels[i]).ToUpperInvariant(), "eyebrow").With(t => t.FontSize = 13);
            Grid.SetRow(l, i);
            Grid.SetRow(_stats[i], i);
            Grid.SetColumn(_stats[i], 1);
            _stats[i].VerticalAlignment = VerticalAlignment.Center;
            statsGrid.Children.Add(l);
            statsGrid.Children.Add(_stats[i]);
        }

        var details = Ui.V(10,
            _title, _name, _origin, _bio, statsGrid,
            Ui.Text(Loc.T("select.specials").ToUpperInvariant(), "eyebrow"), _specials,
            Ui.Text(Loc.T("moves.signatures").ToUpperInvariant(), "eyebrow").With(t => t.Foreground = Ui.Spanduk), _signatures,
            Ui.Text(Loc.T("select.finishers").ToUpperInvariant(), "eyebrow"), _finishers);
        var poster = new Grid();
        _portrait.HorizontalAlignment = HorizontalAlignment.Right;
        _portrait.VerticalAlignment = VerticalAlignment.Top;
        _portrait.Opacity = 0.3;
        poster.Children.Add(_portrait);
        poster.Children.Add(new ScrollViewer { Content = details, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden });
        var left = Ui.Panel(poster, 22).With(p => p.Width = 500);

        var gridScroll = new ScrollViewer { Content = _grid, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var center = Ui.Rows("Auto,Auto,*", Ui.V(6, new SignText(Loc.T("select.title").ToUpperInvariant(), 44), _picking), _tabs.With(t => t.Margin = new Thickness(0, 12, 0, 10)), gridScroll);
        center.Margin = new Thickness(28, 0, 28, 0);

        var right = Ui.V(14, Ui.Text(Loc.T("mode.opponent").ToUpperInvariant(), "eyebrow"), _slots,
            Ui.Chip(Loc.T("common.random").ToUpperInvariant() + "  [R]", PickRandom),
            Ui.Chip(Loc.T("common.back").ToUpperInvariant() + "  [ESC]", () => Back()));
        right.Width = 220;

        var root = Ui.Columns("Auto,*,Auto", left, center, right);
        root.Margin = new Thickness(40, 36, 40, 30);
        Content = root;

        // Open on the tab of the first unlocked category with fighters.
        SetTab(0);
        RefreshSlots();
    }

    private static string TabName(CharacterCategory c) => c switch
    {
        CharacterCategory.Profession => Loc.T("select.category.profession"),
        CharacterCategory.Mystic => Loc.T("select.category.mystic"),
        CharacterCategory.Wildlife => Loc.T("select.category.wildlife"),
        CharacterCategory.Society => Loc.Current == Language.Indonesian ? "Warga & satire" : "City & satire",
        CharacterCategory.Secret => Loc.T("select.category.secret"),
        _ => Loc.T("select.category.custom"),
    };

    private void SetTab(int index)
    {
        _tab = (index + s_tabs.Length) % s_tabs.Length;
        for (int i = 0; i < _tabs.Children.Count; i++)
        {
            if (i == _tab) _tabs.Children[i].Classes.Add("active");
            else _tabs.Children[i].Classes.Remove("active");
        }

        var category = s_tabs[_tab];
        _visible = [.. Game.PlayableRoster().Where(c => c.Category == category && c.Dlc == DlcPack.None || category == CharacterCategory.Custom && c.Category == CharacterCategory.Custom)];
        if (category == CharacterCategory.Profession)
            _visible.AddRange(Game.PlayableRoster().Where(c => c.Dlc != DlcPack.None && c.Category == CharacterCategory.Profession));
        if (category == CharacterCategory.Mystic)
            _visible.AddRange(Game.PlayableRoster().Where(c => c.Dlc != DlcPack.None && c.Category == CharacterCategory.Mystic));

        _grid.Children.Clear();
        NavItems.Clear();
        foreach (var c in _visible)
        {
            var ticket = Ticket(c);
            _grid.Children.Add(ticket);
            Nav(ticket);
        }
        NavIndex = 0;
        if (NavItems.Count > 0) FocusCurrent();
        else ShowDetails(null);
    }

    private Button Ticket(CharacterDefinition c)
    {
        bool unlocked = Game.IsUnlocked(c);
        var image = Ui.Picture(unlocked ? Images.Portrait(c, 200) : null, 100, 108, Stretch.UniformToFill);
        var name = new TextBlock
        {
            Text = unlocked ? c.Name.ToString().ToUpperInvariant() : "???",
            FontFamily = Ui.Display,
            FontSize = 12,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Foreground = Ui.Arang,
        };
        var label = new Border { Background = unlocked ? Ui.Spanduk : Ui.Line, Padding = new Thickness(4, 3), Child = name };
        var body = Ui.Rows("*,Auto", unlocked ? image : Ui.Text("🔒", "h1").With(t => { t.HorizontalAlignment = HorizontalAlignment.Center; t.VerticalAlignment = VerticalAlignment.Center; }), label);
        body.Width = 108;
        body.Height = 138;
        var b = new Button { Content = body }.Classes("ticket");
        if (!unlocked) b.Classes.Add("locked");
        b.GotFocus += (_, _) => ShowDetails(c);
        b.Click += (_, _) => Pick(c);
        return b;
    }

    private void ShowDetails(CharacterDefinition? c)
    {
        _hover = c;
        if (c is null)
        {
            _name.Text = "";
            _bio.Text = "";
            return;
        }
        bool unlocked = Game.IsUnlocked(c);
        _portrait.Source = unlocked ? Images.Portrait(c, 512) : null;
        _name.Text = unlocked ? c.Name.ToString().ToUpperInvariant() : "???";
        _title.Text = c.Title.ToString().ToUpperInvariant();
        _origin.Text = Loc.T("select.origin", c.Origin);
        _bio.Text = unlocked ? c.Bio.ToString() : $"{Loc.T("common.locked")}: {c.UnlockHint}";
        _stats[0].Value = c.Stats.Power;
        _stats[1].Value = c.Stats.Speed;
        _stats[2].Value = c.Stats.Defense;
        _stats[3].Value = c.Stats.Technique;
        _specials.Text = $"S  ·  {c.Specials.Neutral}\n→ + S  ·  {c.Specials.Forward}\n↓ + S  ·  {c.Specials.Down}";
        _signatures.Content = unlocked ? SignatureList.Build(c) : null;
        var finishers = FinisherFactory.Build(c);
        _finishers.Text = string.Join("\n", finishers.Select(f => $"{f.SequenceGlyphs,-12} {FinisherFactory.KindName(f.Kind)}: {f.Name}"));
    }

    private void RefreshSlots()
    {
        _slots.Children.Clear();
        for (int i = 0; i < _plan.SlotCount; i++)
        {
            var c = _plan.Fighters[i];
            string who = _plan.Controllers[i] switch
            {
                SlotController.Player1 => Loc.T("common.player", 1),
                SlotController.Player2 => Loc.T("common.player", 2),
                _ => Loc.T("common.cpu"),
            };
            var row = Ui.H(10,
                Ui.Picture(c is null ? null : Images.Portrait(c, 96), 56, 56, Stretch.UniformToFill),
                Ui.V(0, Ui.Text(who.ToUpperInvariant(), "eyebrow").With(t => t.FontSize = 12),
                    Ui.Text(c?.Name.ToString() ?? (i == _slot ? Loc.T("select.picking") : "—"), "h3")));
            var frame = new Border { Child = row, Padding = new Thickness(8), BorderThickness = new Thickness(0, 0, 0, 3), BorderBrush = i == _slot ? Ui.Spanduk : Ui.Line, Background = Ui.PanelFill };
            _slots.Children.Add(frame);
        }
        if (_slot < _plan.SlotCount)
        {
            var who = _plan.Controllers[_slot] == SlotController.Cpu ? Loc.T("common.cpu") : Loc.T("common.player", _plan.Controllers[_slot] == SlotController.Player2 ? 2 : 1);
            _picking.Text = $"{who.ToUpperInvariant()} · {Loc.T("select.picking")}";
        }
    }

    private void Pick(CharacterDefinition c)
    {
        if (!Game.IsUnlocked(c))
        {
            Game.Audio.Play(Engine.Audio.Sfx.Block, 0.6f);
            return;
        }
        Game.Audio.Play(Engine.Audio.Sfx.HitHeavy, 0.7f, 0.02f);
        if (_onPicked is not null)
        {
            _onPicked(c);
            return;
        }

        _plan.Fighters[_slot] = c;
        _slot++;
        if (_plan.OpponentsFixed || _slot >= _plan.SlotCount)
        {
            Finish();
            return;
        }
        RefreshSlots();
    }

    private void PickRandom()
    {
        var pool = Game.PlayableRoster().Where(Game.IsUnlocked).ToArray();
        Pick(pool[Random.Shared.Next(pool.Length)]);
    }

    private void Finish()
    {
        _plan.FillRandom(Random.Shared);
        if (_plan.StageFixed) Window.Replace(new FightScreen(_plan));
        else Window.Replace(new StageSelectScreen(_plan));
    }

    public override void OnKey(Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.R)
        {
            PickRandom();
            e.Handled = true;
        }
    }

    public override bool HandleAction(MenuAction action)
    {
        switch (action)
        {
            case MenuAction.Left: Move(-1); return true;
            case MenuAction.Right: Move(1); return true;
            case MenuAction.Up:
                if (NavIndex >= Columns) { NavIndex -= Columns; FocusCurrent(); }
                return true;
            case MenuAction.Down:
                if (NavIndex + Columns < NavItems.Count) { NavIndex += Columns; FocusCurrent(); }
                return true;
            case MenuAction.NextTab: SetTab(_tab + 1); return true;
            case MenuAction.PreviousTab: SetTab(_tab - 1); return true;
            case MenuAction.Confirm:
                if (_hover is not null) Pick(_hover);
                return true;
            case MenuAction.Back:
                if (_slot > 0 && _onPicked is null && !_plan.OpponentsFixed)
                {
                    _slot--;
                    _plan.Fighters[_slot] = null;
                    RefreshSlots();
                    return true;
                }
                return Back();
        }
        return false;
    }
}
