using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Styling;

namespace TarkamArena.App.Controls;

/// <summary>
/// Code-side animation helpers built on Avalonia's keyframe animations. They use
/// <see cref="FillMode.Backward"/>, so an element holds its start pose while it waits for its
/// delay and then returns to its styled values, leaving hover/focus styles untouched afterwards.
/// </summary>
public static class Anim
{
    public static bool Reduced { get; set; }

    /// <summary>Avalonia 12 has no keyframe animator for RenderTransform by default; this registers one that blends TransformOperations.</summary>
    public static void Register() => Animation.RegisterCustomAnimator<ITransform, TransformOperationsAnimator>();

    private sealed class TransformOperationsAnimator : InterpolatingAnimator<ITransform>
    {
        public override ITransform Interpolate(double progress, ITransform oldValue, ITransform newValue)
        {
            var from = oldValue as TransformOperations ?? TransformOperations.Identity;
            var to = newValue as TransformOperations ?? TransformOperations.Identity;
            return TransformOperations.Interpolate(from, to, progress);
        }
    }

    private static Animation Make(TimeSpan duration, TimeSpan delay, Easing easing, params (double Cue, Setter[] Setters)[] frames)
    {
        var a = new Animation { Duration = duration, Delay = delay, Easing = easing, FillMode = FillMode.Backward };
        foreach (var (cue, setters) in frames)
        {
            var kf = new KeyFrame { Cue = new Cue(cue) };
            foreach (var s in setters) kf.Setters.Add(s);
            a.Children.Add(kf);
        }
        return a;
    }

    private static int s_loopId;

    /// <summary>
    /// Infinite animations cannot be started with RunAsync; they are attached through a style that
    /// only matches this element (a unique class), so they run for as long as it is on screen.
    /// </summary>
    private static void Loop(Animatable target, Animation animation)
    {
        if (target is not StyledElement element) return;
        string cls = $"tarkam-loop-{++s_loopId}";
        element.Classes.Add(cls);
        var style = new Style(x => x.Class(cls));
        style.Animations.Add(animation);
        element.Styles.Add(style);
    }

    private static Setter T(string ops) => new(Visual.RenderTransformProperty, TransformOperations.Parse(ops));

    private static Setter O(double opacity) => new(Visual.OpacityProperty, opacity);

    /// <summary>Slides in from the side and fades up. <paramref name="baseTransform"/> is the element's resting transform (e.g. the spanduk skew).</summary>
    public static Task SlideIn(Animatable target, double fromX, double delayMs = 0, double durationMs = 380, string baseTransform = "", double fromY = 0)
    {
        if (Reduced) return Task.CompletedTask;
        string rest = string.IsNullOrEmpty(baseTransform) ? "translate(0px, 0px)" : $"{baseTransform} translate(0px, 0px)";
        string start = string.IsNullOrEmpty(baseTransform) ? $"translate({fromX}px, {fromY}px)" : $"{baseTransform} translate({fromX}px, {fromY}px)";
        return Make(TimeSpan.FromMilliseconds(durationMs), TimeSpan.FromMilliseconds(delayMs), new BackEaseOut(),
            (0, [O(0), T(start)]), (0.6, [O(1)]), (1, [O(1), T(rest)])).RunAsync(target);
    }

    public static Task FadeIn(Animatable target, double delayMs = 0, double durationMs = 420)
    {
        if (Reduced) return Task.CompletedTask;
        return Make(TimeSpan.FromMilliseconds(durationMs), TimeSpan.FromMilliseconds(delayMs), new CubicEaseOut(), (0, [O(0)]), (1, [O(1)])).RunAsync(target);
    }

    /// <summary>A rubber-stamp hit: big, tilted and transparent, slammed down to rest with a tiny overshoot.</summary>
    public static Task Stamp(Animatable target, double delayMs = 0, double durationMs = 420, double fromScale = 2.2, double tiltDeg = -8)
    {
        if (Reduced) return Task.CompletedTask;
        return Make(TimeSpan.FromMilliseconds(durationMs), TimeSpan.FromMilliseconds(delayMs), new LinearEasing(),
            (0, [O(0), T($"scale({fromScale}) rotate({tiltDeg}deg)")]),
            (0.45, [O(1), T("scale(0.92) rotate(0deg)")]),
            (0.7, [T("scale(1.05) rotate(0deg)")]),
            (1, [O(1), T("scale(1) rotate(0deg)")])).RunAsync(target);
    }

    /// <summary>Quick scale punch, for counters and value changes.</summary>
    public static Task Pop(Animatable target, double scale = 1.35, double durationMs = 220)
    {
        if (Reduced) return Task.CompletedTask;
        return Make(TimeSpan.FromMilliseconds(durationMs), TimeSpan.Zero, new CubicEaseOut(),
            (0, [T($"scale({scale})")]), (1, [T("scale(1)")])).RunAsync(target);
    }

    /// <summary>A short horizontal shake (life bars when hit).</summary>
    public static Task Shake(Animatable target, double amplitude = 8, double durationMs = 260)
    {
        if (Reduced) return Task.CompletedTask;
        return Make(TimeSpan.FromMilliseconds(durationMs), TimeSpan.Zero, new LinearEasing(),
            (0, [T($"translateX({amplitude}px)")]), (0.2, [T($"translateX({-amplitude}px)")]),
            (0.4, [T($"translateX({amplitude * 0.6}px)")]), (0.6, [T($"translateX({-amplitude * 0.4}px)")]),
            (0.8, [T($"translateX({amplitude * 0.2}px)")]), (1, [T("translateX(0px)")])).RunAsync(target);
    }

    /// <summary>Full-screen flash that fades out (impact frames).</summary>
    public static Task Flash(Animatable target, double peak = 0.85, double durationMs = 260)
    {
        if (Reduced) peak *= 0.3;
        var a = new Animation { Duration = TimeSpan.FromMilliseconds(durationMs), Easing = new CubicEaseOut(), FillMode = FillMode.None };
        a.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { O(peak) } });
        a.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { O(0) } });
        return a.RunAsync(target);
    }

    /// <summary>Looping gentle sway, e.g. a hanging banner in the wind.</summary>
    public static void Sway(Animatable target, double degrees = 1.2, double seconds = 3.6, string baseTransform = "")
    {
        if (Reduced) return;
        var a = new Animation { Duration = TimeSpan.FromSeconds(seconds), IterationCount = IterationCount.Infinite, PlaybackDirection = PlaybackDirection.Alternate, Easing = new SineEaseInOut() };
        a.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { T($"{baseTransform} rotate({-degrees}deg)".Trim()) } });
        a.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { T($"{baseTransform} rotate({degrees}deg)".Trim()) } });
        Loop(target, a);
    }

    /// <summary>Slow Ken Burns drift for background art.</summary>
    public static void Drift(Animatable target, double seconds = 24, double scale = 1.12, double panPx = 40)
    {
        if (Reduced) return;
        var a = new Animation { Duration = TimeSpan.FromSeconds(seconds), IterationCount = IterationCount.Infinite, PlaybackDirection = PlaybackDirection.Alternate, Easing = new SineEaseInOut() };
        a.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { T($"scale({scale}) translateX({-panPx}px)") } });
        a.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { T($"scale({scale + 0.04}) translateX({panPx}px)") } });
        Loop(target, a);
    }

    /// <summary>Infinite slow spin (loading gunungan).</summary>
    public static void Spin(Animatable target, double seconds = 6)
    {
        if (Reduced) return;
        var a = new Animation { Duration = TimeSpan.FromSeconds(seconds), IterationCount = IterationCount.Infinite };
        a.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { T("rotate(-12deg)") } });
        a.Children.Add(new KeyFrame { Cue = new Cue(0.5), Setters = { T("rotate(12deg)") } });
        a.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { T("rotate(-12deg)") } });
        a.Easing = new SineEaseInOut();
        Loop(target, a);
    }

    /// <summary>Breathing opacity loop (prompts, low-health vignette).</summary>
    public static void Pulse(Animatable target, double min = 0.35, double max = 1, double seconds = 1.1)
    {
        var a = new Animation { Duration = TimeSpan.FromSeconds(seconds), IterationCount = IterationCount.Infinite, PlaybackDirection = PlaybackDirection.Alternate, Easing = new SineEaseInOut() };
        a.Children.Add(new KeyFrame { Cue = new Cue(0), Setters = { O(max) } });
        a.Children.Add(new KeyFrame { Cue = new Cue(1), Setters = { O(min) } });
        Loop(target, a);
    }

    /// <summary>Staggers <see cref="SlideIn"/> over a list of items.</summary>
    public static void Stagger(IEnumerable<Animatable> items, double fromX, double stepMs = 45, double startMs = 60, string baseTransform = "")
    {
        int i = 0;
        foreach (var item in items) _ = SlideIn(item, fromX, startMs + i++ * stepMs, 360, baseTransform);
    }
}
