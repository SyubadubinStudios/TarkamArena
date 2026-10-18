using Avalonia.Controls;
using Avalonia.Input;
using TarkamArena.App.Controls;

namespace TarkamArena.App.Screens;

public enum MenuAction
{
    Up,
    Down,
    Left,
    Right,
    Confirm,
    Back,
    NextTab,
    PreviousTab,
}

/// <summary>
/// Base class for every screen. Screens get high-level menu actions (from keyboard or gamepad)
/// and by default walk a list of focusable items with Up/Down and activate with Confirm.
/// </summary>
public abstract class Screen : UserControl
{
    protected List<Control> NavItems { get; } = [];
    protected int NavIndex { get; set; }

    public MainWindow Window { get; internal set; } = null!;

    /// <summary>When true the shell does not handle Escape for this screen.</summary>
    public virtual bool CapturesBack => false;

    /// <summary>When true the shell does not translate keys to menu actions (the fight screen reads raw input).</summary>
    public virtual bool WantsRawInput => false;

    public virtual void OnEnter() => FocusCurrent();

    public virtual void OnLeave()
    {
    }

    /// <summary>Called when the language changes; screens rebuild their text.</summary>
    public virtual void Relocalize()
    {
    }

    public virtual void OnKey(KeyEventArgs e)
    {
    }

    public virtual bool HandleAction(MenuAction action)
    {
        if (NavItems.Count == 0) return action == MenuAction.Back && Back();
        switch (action)
        {
            case MenuAction.Up:
            case MenuAction.Left:
                Move(-1);
                return true;
            case MenuAction.Down:
            case MenuAction.Right:
                Move(1);
                return true;
            case MenuAction.Confirm:
                if (NavItems[NavIndex] is Button b) b.Activate();
                return true;
            case MenuAction.Back:
                return Back();
        }
        return false;
    }

    protected virtual bool Back()
    {
        Window.Back();
        return true;
    }

    protected void Move(int delta)
    {
        if (NavItems.Count == 0) return;
        int start = NavIndex;
        do
        {
            NavIndex = (NavIndex + delta + NavItems.Count) % NavItems.Count;
        } while (!NavItems[NavIndex].IsEnabled && NavIndex != start);
        FocusCurrent();
        Services.Game.Audio.Play(Engine.Audio.Sfx.Tag, 0.25f, 0.02f);
    }

    protected void FocusCurrent()
    {
        if (NavItems.Count == 0) return;
        NavIndex = Math.Clamp(NavIndex, 0, NavItems.Count - 1);
        NavItems[NavIndex].Focus(NavigationMethod.Directional);
        OnNavChanged(NavIndex);
    }

    protected virtual void OnNavChanged(int index)
    {
    }

    /// <summary>Registers buttons for keyboard/gamepad navigation and keeps the index in sync with the mouse.</summary>
    protected void Nav(params Control[] items)
    {
        foreach (var item in items)
        {
            int index = NavItems.Count;
            NavItems.Add(item);
            item.GotFocus += (_, _) =>
            {
                if (NavIndex == index) return;
                NavIndex = index;
                OnNavChanged(index);
            };
            item.PointerEntered += (_, _) =>
            {
                if (item.IsEnabled) item.Focus(NavigationMethod.Directional);
            };
        }
    }

    protected static Control Footer()
    {
        var credit = Ui.Text(Core.Localization.Loc.T("app.credit"), "muted").With(t => t.FontSize = 15);
        return credit;
    }
}
