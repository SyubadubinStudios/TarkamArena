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

/// <summary>
/// The Jurus meter that pays for signature moves: a violet-to-gold bar with a diamond at each
/// signature cost. A diamond lights up (and the bar shimmers) once that signature is affordable.
/// </summary>
public sealed class JurusMeter : Control
{
    private static readonly IBrush s_track = Ui.Brush(0x0A1638, 220);
    private static readonly IBrush s_fill = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = { new GradientStop(Color.FromRgb(0x7A, 0x3C, 0xFF), 0), new GradientStop(Color.FromRgb(0xFF, 0x4F, 0xC8), 0.55), new GradientStop(Color.FromRgb(0xFF, 0xD1, 0x3A), 1) },
    };
    private static readonly IBrush s_shine = Ui.Brush(0xFFFFFF, 90);
    private static readonly IBrush s_lit = Ui.Spanduk;
    private static readonly IBrush s_unlit = Ui.Brush(0x2A3460);
    private static readonly Pen s_outline = new(Ui.Arang, 2);
    private static readonly Pen s_tick = new(Ui.Brush(0xF3EEE1, 120), 1);

    private double _value, _shimmer;
    private int _costsMax = 1000;
    private readonly int[] _costs = [250, 400, 700];

    public bool Mirrored { get; init; }

    /// <summary>Number of signatures currently affordable (0-3).</summary>
    public int Level { get; private set; }

    public void Configure(ReadOnlySpan<int> costs, int max)
    {
        costs.CopyTo(_costs);
        _costsMax = max;
    }

    public void Set(int jurus, double dt)
    {
        double value = Math.Clamp(jurus / (double)_costsMax, 0, 1);
        int level = 0;
        foreach (int c in _costs) if (jurus >= c) level++;
        Level = level;
        _shimmer = (_shimmer + dt * 0.8) % 1.0;
        if (Math.Abs(value - _value) < 0.001 && level == 0) return;
        _value = value;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        double w = Bounds.Width, h = Bounds.Height, bar = h * 0.62, top = (h - bar) / 2;
        var track = new Rect(0, top, w, bar);
        context.FillRectangle(s_track, track);
        double fw = w * _value;
        if (fw > 0)
        {
            var fill = Mirrored ? new Rect(w - fw, top, fw, bar) : new Rect(0, top, fw, bar);
            context.FillRectangle(s_fill, fill);
            if (Level > 0)
            {
                // A highlight sweeps along the filled part while a signature is ready.
                double sx = fw * _shimmer;
                double x = Mirrored ? w - sx - 18 : sx;
                context.FillRectangle(s_shine, new Rect(Math.Clamp(x, fill.X, Math.Max(fill.X, fill.Right - 18)), top, 18, bar));
            }
        }
        context.DrawRectangle(s_outline, track);

        for (int i = 0; i < _costs.Length; i++)
        {
            double f = _costs[i] / (double)_costsMax;
            double x = Mirrored ? w - w * f : w * f;
            context.DrawLine(s_tick, new Point(x, top), new Point(x, top + bar));
            double d = h * 0.5;
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(new Point(x, h / 2 - d), true);
                c.LineTo(new Point(x + d, h / 2));
                c.LineTo(new Point(x, h / 2 + d));
                c.LineTo(new Point(x - d, h / 2));
                c.EndFigure(true);
            }
            context.DrawGeometry(i < Level ? s_lit : s_unlit, s_outline, g);
        }
    }
}
