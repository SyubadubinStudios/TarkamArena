using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace TarkamArena.App.Controls;

/// <summary>Small helpers that keep screen code declarative without a XAML file per screen.</summary>
public static class Ui
{
    public static readonly Color TerpalColor = Color.Parse("#1F3F9E");
    public static readonly Color TerpalDeepColor = Color.Parse("#112459");
    public static readonly Color NightColor = Color.Parse("#0A1638");
    public static readonly Color SpandukColor = Color.Parse("#F6C21B");
    public static readonly Color CabaiColor = Color.Parse("#E0322B");
    public static readonly Color KapurColor = Color.Parse("#F3EEE1");
    public static readonly Color ArangColor = Color.Parse("#14141A");
    public static readonly Color DaunColor = Color.Parse("#2E8B57");

    public static readonly IBrush Terpal = new SolidColorBrush(TerpalColor);
    public static readonly IBrush TerpalDeep = new SolidColorBrush(TerpalDeepColor);
    public static readonly IBrush Night = new SolidColorBrush(NightColor);
    public static readonly IBrush Spanduk = new SolidColorBrush(SpandukColor);
    public static readonly IBrush Cabai = new SolidColorBrush(CabaiColor);
    public static readonly IBrush Kapur = new SolidColorBrush(KapurColor);
    public static readonly IBrush KapurMuted = new SolidColorBrush(Color.FromArgb(0xB3, 0xF3, 0xEE, 0xE1));
    public static readonly IBrush Arang = new SolidColorBrush(ArangColor);
    public static readonly IBrush Daun = new SolidColorBrush(DaunColor);
    public static readonly IBrush Line = new SolidColorBrush(Color.FromArgb(0x33, 0xF3, 0xEE, 0xE1));
    public static readonly IBrush PanelFill = new SolidColorBrush(Color.FromArgb(0xCC, 0x0A, 0x16, 0x38));

    public static FontFamily Display => (FontFamily)Application.Current!.FindResource("Display")!;
    public static FontFamily Body => (FontFamily)Application.Current!.FindResource("Body")!;
    public static FontFamily Mono => (FontFamily)Application.Current!.FindResource("Mono")!;

    public static IBrush Brush(uint rgb, byte alpha = 255) =>
        new SolidColorBrush(Color.FromArgb(alpha, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));

    public static T With<T>(this T control, Action<T> configure) where T : Control
    {
        configure(control);
        return control;
    }

    public static T Classes<T>(this T control, params string[] classes) where T : StyledElement
    {
        foreach (var c in classes) control.Classes.Add(c);
        return control;
    }

    public static TextBlock Text(string text, params string[] classes) => new TextBlock { Text = text }.Classes(classes);

    public static TextBlock Wrap(string text, params string[] classes) =>
        new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }.Classes(classes);

    public static StackPanel V(double spacing, params Control[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Vertical, Spacing = spacing };
        p.Children.AddRange(children);
        return p;
    }

    public static StackPanel H(double spacing, params Control[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing };
        p.Children.AddRange(children);
        return p;
    }

    public static Border Panel(Control child, double padding = 24) =>
        new Border { Child = child, Padding = new Thickness(padding) }.Classes("panel");

    public static Button Banner(string text, Action onClick, bool small = false)
    {
        var b = new Button { Content = text, Focusable = true }.Classes("spanduk");
        if (small) b.Classes.Add("small");
        b.Click += (_, _) => onClick();
        return b;
    }

    public static Button Chip(string text, Action onClick)
    {
        var b = new Button { Content = text }.Classes("chip");
        b.Click += (_, _) => onClick();
        return b;
    }

    public static Image Picture(Bitmap? bitmap, double width = double.NaN, double height = double.NaN, Stretch stretch = Stretch.Uniform) =>
        new() { Source = bitmap, Width = width, Height = height, Stretch = stretch };

    public static void Activate(this Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    public static Grid Columns(string definitions, params Control[] children)
    {
        var g = new Grid { ColumnDefinitions = ColumnDefinitions.Parse(definitions) };
        for (int i = 0; i < children.Length; i++)
        {
            Grid.SetColumn(children[i], i);
            g.Children.Add(children[i]);
        }
        return g;
    }

    public static Grid Rows(string definitions, params Control[] children)
    {
        var g = new Grid { RowDefinitions = RowDefinitions.Parse(definitions) };
        for (int i = 0; i < children.Length; i++)
        {
            Grid.SetRow(children[i], i);
            g.Children.Add(children[i]);
        }
        return g;
    }

    /// <summary>Tiled tarpaulin weave over the night blue, the base texture of every screen.</summary>
    public static IBrush TarpBackground()
    {
        var bmp = Services.Images.Load("ui/terpal-weave.png");
        if (bmp is null) return Night;
        return new ImageBrush(bmp)
        {
            TileMode = TileMode.Tile,
            DestinationRect = new RelativeRect(0, 0, 96, 96, RelativeUnit.Absolute),
            Stretch = Stretch.None,
        };
    }
}

/// <summary>
/// Signature lettering: the text printed twice, the chilli-red ink a few pixels off register, like
/// a screen-printed tournament poster.
/// </summary>
public sealed class SignText : Grid
{
    private readonly TextBlock _shadow;
    private readonly TextBlock _main;

    public SignText(string text, double size, IBrush? ink = null, double offset = 0)
    {
        double o = offset > 0 ? offset : Math.Max(3, size / 14);
        _shadow = new TextBlock { Text = text, FontFamily = Ui.Display, FontSize = size, Foreground = Ui.Cabai, Margin = new Thickness(o, o * 0.8, 0, 0), LineHeight = size * 0.98 };
        _main = new TextBlock { Text = text, FontFamily = Ui.Display, FontSize = size, Foreground = ink ?? Ui.Spanduk, LineHeight = size * 0.98 };
        Children.Add(_shadow);
        Children.Add(_main);
    }

    public string Text
    {
        get => _main.Text ?? "";
        set
        {
            _main.Text = value;
            _shadow.Text = value;
        }
    }

    public TextAlignment TextAlignment
    {
        set
        {
            _main.TextAlignment = value;
            _shadow.TextAlignment = value;
        }
    }
}
