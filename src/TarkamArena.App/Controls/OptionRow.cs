using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace TarkamArena.App.Controls;

/// <summary>
/// A labelled value that cycles with Left/Right (or a click), the way fighting-game option menus
/// work with a pad: "CPU difficulty  ‹ Hard ›".
/// </summary>
public sealed class OptionRow : Button
{
    private readonly TextBlock _value;
    private readonly Func<int, string> _format;
    private readonly int _count;
    private readonly Action<int> _changed;

    public int Index { get; private set; }

    public OptionRow(string label, int count, int initial, Func<int, string> format, Action<int> changed)
    {
        _count = count;
        _format = format;
        _changed = changed;
        Index = Math.Clamp(initial, 0, count - 1);
        _value = new TextBlock { FontFamily = Ui.Display, FontSize = 20, Foreground = Ui.Spanduk, VerticalAlignment = VerticalAlignment.Center };
        Content = Ui.Columns("*,Auto",
            new TextBlock { Text = label, FontSize = 19, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
            Ui.H(10, Ui.Text("‹", "h3", "muted"), _value, Ui.Text("›", "h3", "muted")));
        Classes.Add("chip");
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        MinWidth = 460;
        Padding = new Thickness(18, 10);
        Click += (_, _) => Step(1);
        Refresh();
    }

    public void Step(int delta)
    {
        Index = (Index + delta + _count) % _count;
        Refresh();
        _changed(Index);
    }

    private void Refresh() => _value.Text = _format(Index);
}
