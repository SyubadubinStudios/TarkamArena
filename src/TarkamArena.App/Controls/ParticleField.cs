using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TarkamArena.App.Controls;

public enum ParticleStyle : byte
{
    /// <summary>Warm embers rising, like sparks off a torch at a night tournament.</summary>
    Embers,
    /// <summary>Slow dust motes drifting in a light beam.</summary>
    Dust,
    /// <summary>Paper confetti in the tournament colours, falling and tumbling.</summary>
    Confetti,
    /// <summary>Fast streaks radiating from the centre (VS screen, finisher).</summary>
    SpeedLines,
}

/// <summary>
/// Lightweight 2D particle overlay for menus, the VS screen and the HUD. Particles are structs in a
/// fixed array, stepped on the compositor's animation frame callback only while the control is
/// visible and attached, so idle screens cost nothing and nothing is allocated per frame.
/// </summary>
public sealed class ParticleField : Control
{
    private struct P
    {
        public double X, Y, Vx, Vy, Life, MaxLife, Size, Spin, Angle;
        public byte Color;
    }

    private static readonly IBrush[] s_ember = [Ui.Brush(0xFFB347), Ui.Brush(0xF6C21B), Ui.Brush(0xE0322B), Ui.Brush(0xFFE7A0)];
    private static readonly IBrush[] s_confetti = [Ui.Spanduk, Ui.Cabai, Ui.Kapur, Ui.Brush(0x2E8B57), Ui.Brush(0x6BD0FF)];
    private static readonly IBrush s_dust = Ui.Brush(0xF3EEE1, 90);
    private static readonly Pen s_lineGold = new(Ui.Spanduk, 2.5);
    private static readonly Pen s_lineChalk = new(Ui.Kapur, 2.5);

    private readonly P[] _p;
    private readonly Random _random = new(13);
    private readonly ParticleStyle _style;
    private TimeSpan? _last;
    private bool _running;
    private double _burst;

    public double Intensity { get; set; } = 1;

    public ParticleField(ParticleStyle style, int count = 90)
    {
        _style = style;
        _p = new P[count];
        IsHitTestVisible = false;
        for (int i = 0; i < _p.Length; i++) Respawn(ref _p[i], initial: true);
    }

    /// <summary>Spawns an extra burst (e.g. confetti on a team combo).</summary>
    public void Burst(double seconds = 0.6) => _burst = seconds;

    // A low-priority 30 fps timer instead of the compositor's frame callback: decorative particles
    // must never compete with the 3D view, input or the simulation for the UI thread.
    private Avalonia.Threading.DispatcherTimer? _timer;
    private readonly System.Diagnostics.Stopwatch _clock = new();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _running = true;
        _clock.Restart();
        _last = null;
        _timer ??= new Avalonia.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(33), Avalonia.Threading.DispatcherPriority.Background, (_, _) => OnFrame(_clock.Elapsed));
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _running = false;
        _timer?.Stop();
        base.OnDetachedFromVisualTree(e);
    }

    private void OnFrame(TimeSpan time)
    {
        if (!_running) return;
        double dt = _last is { } l ? Math.Min(0.05, (time - l).TotalSeconds) : 0;
        _last = time;
        if (!IsEffectivelyVisible || (Intensity <= 0 && _burst <= 0)) return;
        if (dt > 0) Step(dt);
        InvalidateVisual();
    }

    private void Respawn(ref P p, bool initial = false)
    {
        double w = Math.Max(1, Bounds.Width), h = Math.Max(1, Bounds.Height);
        if (Bounds.Width <= 0) { w = 1600; h = 900; }
        double r() => _random.NextDouble();
        switch (_style)
        {
            case ParticleStyle.Embers:
                p.X = r() * w;
                p.Y = initial ? r() * h : h + 10;
                p.Vx = (r() - 0.5) * 30;
                p.Vy = -(30 + r() * 70);
                p.MaxLife = 3 + r() * 5;
                p.Size = 1.5 + r() * 3.5;
                break;
            case ParticleStyle.Dust:
                p.X = r() * w;
                p.Y = r() * h;
                p.Vx = 6 + r() * 12;
                p.Vy = (r() - 0.5) * 6;
                p.MaxLife = 6 + r() * 8;
                p.Size = 1 + r() * 2.5;
                break;
            case ParticleStyle.Confetti:
                p.X = r() * w;
                p.Y = initial ? r() * h * -1 : -20 - r() * 80;
                p.Vx = (r() - 0.5) * 60;
                p.Vy = 60 + r() * 90;
                p.MaxLife = 5 + r() * 3;
                p.Size = 5 + r() * 7;
                p.Spin = (r() - 0.5) * 12;
                break;
            case ParticleStyle.SpeedLines:
                double a = r() * Math.PI * 2;
                double speed = 900 + r() * 900;
                p.X = w / 2 + Math.Cos(a) * 60;
                p.Y = h / 2 + Math.Sin(a) * 60;
                p.Vx = Math.Cos(a) * speed;
                p.Vy = Math.Sin(a) * speed;
                p.Angle = a;
                p.MaxLife = 0.35 + r() * 0.4;
                p.Size = 40 + r() * 140;
                break;
        }
        p.Life = initial ? r() * p.MaxLife : p.MaxLife;
        p.Color = (byte)_random.Next(5);
    }

    private void Step(double dt)
    {
        if (_burst > 0) _burst -= dt;
        double gust = Math.Sin(_last!.Value.TotalSeconds * 0.7) * 12;
        for (int i = 0; i < _p.Length; i++)
        {
            ref var p = ref _p[i];
            p.Life -= dt * (_burst > 0 ? 2 : 1);
            if (p.Life <= 0)
            {
                if (_style == ParticleStyle.Confetti && Intensity <= 0 && _burst <= 0) { p.Life = 0; continue; }
                Respawn(ref p);
            }
            p.X += (p.Vx + gust * (_style == ParticleStyle.Embers ? 1 : 0.3)) * dt;
            p.Y += p.Vy * dt;
            p.Angle += p.Spin * dt;
            if (_style == ParticleStyle.Embers) p.Vx += Math.Sin(p.Y * 0.02 + i) * 8 * dt;
        }
    }

    public override void Render(DrawingContext context)
    {
        double intensity = Math.Clamp(Intensity, 0, 1);
        if (intensity <= 0 && _burst <= 0) return;
        for (int i = 0; i < _p.Length; i++)
        {
            ref readonly var p = ref _p[i];
            if (p.Life <= 0) continue;
            double t = p.Life / p.MaxLife;
            double alpha = Math.Min(1, t * 3) * Math.Min(1, (1 - t) * 6 + 0.2) * (intensity > 0 ? intensity : 1);
            using (context.PushOpacity(alpha))
            {
                switch (_style)
                {
                    case ParticleStyle.Embers:
                        var brush = s_ember[p.Color % s_ember.Length];
                        context.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Size, p.Size);
                        using (context.PushOpacity(0.25)) context.DrawEllipse(brush, null, new Point(p.X, p.Y), p.Size * 3, p.Size * 3);
                        break;
                    case ParticleStyle.Dust:
                        context.DrawEllipse(s_dust, null, new Point(p.X, p.Y), p.Size, p.Size);
                        break;
                    case ParticleStyle.Confetti:
                        using (context.PushTransform(Matrix.CreateRotation(p.Angle) * Matrix.CreateTranslation(p.X, p.Y)))
                            context.FillRectangle(s_confetti[p.Color % s_confetti.Length], new Rect(-p.Size / 2, -p.Size / 4, p.Size, p.Size / 2 * Math.Abs(Math.Cos(p.Angle * 1.7)) + 1));
                        break;
                    case ParticleStyle.SpeedLines:
                        var end = new Point(p.X - Math.Cos(p.Angle) * p.Size, p.Y - Math.Sin(p.Angle) * p.Size);
                        context.DrawLine(i % 3 == 0 ? s_lineGold : s_lineChalk, new Point(p.X, p.Y), end);
                        break;
                }
            }
        }
    }
}
