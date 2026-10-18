using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using TarkamArena.App.Controls;
using TarkamArena.App.Services;
using TarkamArena.Core.Localization;
using TarkamArena.Core.Progression;

namespace TarkamArena.App.Screens;

/// <summary>Achievements and cosmetics: costumes, victory poses, intros, emotes and banners bought with coins.</summary>
public sealed class CollectionScreen : Screen
{
    private static readonly (string Key, UnlockKind? Kind)[] s_tabs =
    [
        ("collection.achievements", null), ("collection.costumes", UnlockKind.Costume), ("collection.poses", UnlockKind.VictoryPose),
        ("collection.intros", UnlockKind.Intro), ("collection.emotes", UnlockKind.Emote), ("collection.banners", UnlockKind.Banner),
    ];

    private readonly StackPanel _tabs = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
    private readonly StackPanel _list = new() { Spacing = 6 };
    private readonly TextBlock _wallet = Ui.Text("", "h3");
    private int _tab;

    public CollectionScreen()
    {
        for (int i = 0; i < s_tabs.Length; i++)
        {
            int index = i;
            _tabs.Children.Add(Ui.Chip(Loc.T(s_tabs[i].Key).ToUpperInvariant(), () => SetTab(index)));
        }
        var root = Ui.Rows("Auto,Auto,Auto,*",
            new SignText(Loc.T("collection.title").ToUpperInvariant(), 52), _wallet, _tabs.With(t => t.Margin = new Thickness(0, 12)),
            new ScrollViewer { Content = _list });
        root.Margin = new Thickness(80, 44, 80, 40);
        Content = root;
        SetTab(0);
    }

    private void SetTab(int index)
    {
        _tab = (index + s_tabs.Length) % s_tabs.Length;
        for (int i = 0; i < _tabs.Children.Count; i++)
        {
            if (i == _tab) _tabs.Children[i].Classes.Add("active");
            else _tabs.Children[i].Classes.Remove("active");
        }
        var p = Game.Profile;
        _wallet.Text = $"{Loc.T("collection.level", p.Level)} · {p.Xp}/{PlayerProfile.XpForLevel(p.Level)} XP · {Loc.T("collection.coins", p.Coins)} · {p.Achievements.Count}/{Achievements.All.Count} ★";
        _list.Children.Clear();
        NavItems.Clear();

        if (s_tabs[_tab].Kind is not { } kind)
        {
            foreach (var a in Achievements.All)
            {
                bool earned = p.Achievements.Contains(a.Id);
                var row = Ui.Columns("40,320,*", Ui.Text(earned ? "★" : "☆", "h2").With(t => t.Foreground = earned ? Ui.Spanduk : Ui.Line),
                    Ui.Text(a.Name.ToString(), "h3").With(t => t.Opacity = earned ? 1 : 0.6), Ui.Text(a.Description.ToString(), "body", "muted"));
                _list.Children.Add(row);
            }
            return;
        }

        foreach (var item in Unlockables.OfKind(kind).Take(kind == UnlockKind.Costume ? 400 : 100))
        {
            bool owned = p.Owns(item.Id);
            string who = item.CharacterId is { } id && Core.Content.Roster.TryGet(id, out var c) ? $"{c.Name} · " : "";
            var label = Ui.Text($"{who}{item.Name}", "h3");
            Control action = owned
                ? Ui.Text("✓", "h3").With(t => t.Foreground = Ui.Daun)
                : Ui.Chip(p.Level < item.RequiredLevel ? $"{Loc.T("collection.level", item.RequiredLevel)} 🔒" : Loc.T("collection.buy", item.Price), () => Buy(item));
            if (action is Button b) Nav(b);
            var swatch = new Border { Width = 26, Height = 26, Background = item.Tint == 0 ? Ui.Line : Ui.Brush(item.Tint) };
            _list.Children.Add(Ui.Columns("40,*,Auto", swatch, label, action));
        }
        NavIndex = 0;
        if (NavItems.Count > 0) FocusCurrent();
    }

    private void Buy(Unlockable item)
    {
        if (Game.Profile.TryBuy(item))
        {
            Game.Audio.Play(Engine.Audio.Sfx.Confetti, 0.8f);
            Window.ToastRewards(ProgressionService.Refresh(Game.Profile));
            Game.Save();
        }
        else Game.Audio.Play(Engine.Audio.Sfx.Block, 0.6f);
        SetTab(_tab);
    }

    public override bool HandleAction(MenuAction action) => action switch
    {
        MenuAction.NextTab or MenuAction.Right => Tab(1),
        MenuAction.PreviousTab or MenuAction.Left => Tab(-1),
        _ => base.HandleAction(action),
    };

    private bool Tab(int d)
    {
        SetTab(_tab + d);
        return true;
    }
}
