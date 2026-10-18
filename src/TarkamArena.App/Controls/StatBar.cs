using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TarkamArena.App.Controls;

/// <summary>A row of ten stamped blocks, like a hand-painted rating on a fight poster.</summary>
public sealed class StatBar : Control
{
    public static readonly StyledProperty<int> ValueProperty = AvaloniaProperty.Register<StatBar, int>(nameof(Value));

    static StatBar() => AffectsRender<StatBar>(ValueProperty);

    public int Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public StatBar()
    {
        Height = 14;
        Width = 220;
    }

    public override void Render(DrawingContext context)
    {
        double gap = 3;
        double w = (Bounds.Width - gap * 9) / 10;
        for (int i = 0; i < 10; i++)
        {
            var rect = new Rect(i * (w + gap), 0, w, Bounds.Height);
            context.FillRectangle(i < Value ? (i >= 8 ? Ui.Cabai : Ui.Spanduk) : Ui.Line, rect);
        }
    }
}

/// <summary>
/// Fight HUD life bar: slanted, with a red "damage trail" that drains after the white bar so the
/// player sees how much a combo took.
/// </summary>
public sealed class LifeBar : Control
{
    private double _value = 1, _trail = 1, _flash;

    public bool Mirrored { get; init; }
    public IBrush Fill { get; set; } = Ui.Spanduk;

    public void Set(double value, double dt)
    {
        value = Math.Clamp(value, 0, 1);
        if (value < _value - 0.0001) _flash = 0.25;
        _value = value;
        if (_trail < _value) _trail = _value;
        else _trail = Math.Max(_value, _trail - dt * 0.35);
        _flash = Math.Max(0, _flash - dt);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width, h = Bounds.Height, slant = h * 0.6;
        StreamGeometry Shape(double from, double to)
        {
            var g = new StreamGeometry();
            using var c = g.Open();
            double x0 = Mirrored ? w - to * w : from * w;
            double x1 = Mirrored ? w - from * w : to * w;
            c.BeginFigure(new Point(x0 + slant, 0), true);
            c.LineTo(new Point(x1, 0));
            c.LineTo(new Point(x1 - slant, h));
            c.LineTo(new Point(x0, h));
            c.EndFigure(true);
            return g;
        }
        context.DrawGeometry(Ui.Brush(0x0A1638, 230), new Pen(Ui.Arang, 3), Shape(0, 1));
        if (_trail > 0) context.DrawGeometry(Ui.Cabai, null, Shape(0, _trail));
        if (_value > 0) context.DrawGeometry(_flash > 0 ? Ui.Kapur : _value < 0.25 ? Ui.Cabai : Fill, null, Shape(0, _value));
        context.DrawGeometry(null, new Pen(Ui.Arang, 3), Shape(0, 1));
    }
}

/// <summary>Thin segmented meter for rage and adrenaline.</summary>
public sealed class SegmentMeter : Control
{
    private double _value;
    public IBrush Fill { get; set; } = Ui.Cabai;
    public bool Mirrored { get; init; }
    public bool Active { get; set; }

    public void Set(double value)
    {
        if (Math.Abs(value - _value) < 0.002 && !Active) return;
        _value = Math.Clamp(value, 0, 1);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        int segments = 5;
        double gap = 4, w = (Bounds.Width - gap * (segments - 1)) / segments, h = Bounds.Height;
        for (int i = 0; i < segments; i++)
        {
            int idx = Mirrored ? segments - 1 - i : i;
            double fill = Math.Clamp(_value * segments - idx, 0, 1);
            var rect = new Rect(i * (w + gap), 0, w, h);
            context.FillRectangle(Ui.Brush(0x0A1638, 200), rect);
            if (fill > 0)
            {
                double fw = w * fill;
                var fr = Mirrored ? new Rect(rect.Right - fw, 0, fw, h) : new Rect(rect.X, 0, fw, h);
                context.FillRectangle(Active ? Ui.Kapur : Fill, fr);
            }
            context.DrawRectangle(new Pen(Ui.Arang, 2), rect);
        }
    }
}

/// <summary>
/// The crowd excitement meter drawn as a sound-system VU meter: green, then banner yellow, then
/// chilli red at the top, with a peak marker that falls slowly.
/// </summary>
public sealed class CrowdMeter : Control
{
    private double _value, _peak;

    public void Set(double value, double dt)
    {
        _value = Math.Clamp(value, 0, 1);
        _peak = Math.Max(_value, _peak - dt * 0.12);
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        const int bars = 16;
        double gap = 3, w = (Bounds.Width - gap * (bars - 1)) / bars, h = Bounds.Height;
        for (int i = 0; i < bars; i++)
        {
            double t = (i + 1) / (double)bars;
            double barH = h * (0.35 + 0.65 * t);
            var rect = new Rect(i * (w + gap), h - barH, w, barH);
            bool lit = _value >= t - 0.5 / bars;
            IBrush color = t > 0.8 ? Ui.Cabai : t > 0.5 ? Ui.Spanduk : Ui.Daun;
            context.FillRectangle(lit ? color : Ui.Brush(0x0A1638, 200), rect);
            if (Math.Abs(_peak - t) < 0.5 / bars) context.FillRectangle(Ui.Kapur, new Rect(rect.X, rect.Y - 4, w, 3));
        }
    }
}
