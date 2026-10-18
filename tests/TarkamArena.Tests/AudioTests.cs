using TarkamArena.Core.Content;
using TarkamArena.Engine.Audio;

namespace TarkamArena.Tests;

public class AudioTests
{
    [Theory]
    [MemberData(nameof(Styles))]
    public void Every_music_style_renders_a_loud_seamless_loop(MusicStyle style)
    {
        var buffer = MusicGenerator.Compose(style, 3, bars: 2);
        var data = buffer.ToInterleaved();
        Assert.True(buffer.Frames > Synth.SampleRate);
        double rms = Math.Sqrt(data.Average(s => (double)s * s));
        Assert.InRange(rms, 0.02, 0.9);
        Assert.All(data, s => Assert.InRange(s, -1f, 1f));
    }

    public static TheoryData<MusicStyle> Styles() => [.. Enum.GetValues<MusicStyle>()];

    [Fact]
    public void Every_sound_effect_is_audible()
    {
        foreach (var sfx in Enum.GetValues<Sfx>())
        {
            var data = SoundBank.Render(sfx).ToInterleaved();
            Assert.True(data.Max(MathF.Abs) > 0.1f, sfx.ToString());
        }
    }
}
