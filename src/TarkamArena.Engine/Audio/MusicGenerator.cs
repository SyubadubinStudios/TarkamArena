using TarkamArena.Core.Content;

namespace TarkamArena.Engine.Audio;

/// <summary>
/// Composes a seamless stage loop for every <see cref="MusicStyle"/>: traditional ensembles
/// (gamelan, angklung, degung, talempong, sasando, baleganjur, reog) and modern ones (metal, rock,
/// EDM, orchestral hybrid, dangdut koplo, punk, sound horeg). Seeded, so a stage always gets the
/// same tune.
/// </summary>
public static class MusicGenerator
{
    // Approximate tunings in cents. Slendro is close to five equal steps; pelog is uneven.
    private static readonly float[] s_slendro = [0, 240, 480, 720, 960];
    private static readonly float[] s_pelog = [0, 120, 270, 540, 670, 785, 950];
    private static readonly float[] s_degung = [0, 120, 400, 520, 800];
    private static readonly float[] s_minor = [0, 200, 300, 500, 700, 800, 1000];
    private static readonly float[] s_phrygian = [0, 100, 300, 500, 700, 800, 1000];

    public static StereoBuffer Compose(MusicStyle style, int seed, int bars = 8)
    {
        var rng = new Random(seed);
        float bpm = style switch
        {
            MusicStyle.Gamelan => 84, MusicStyle.Angklung => 96, MusicStyle.DegungSunda => 76, MusicStyle.Talempong => 112,
            MusicStyle.Sasando => 88, MusicStyle.MetalHardcore => 170, MusicStyle.Rock => 132, MusicStyle.Edm => 126,
            MusicStyle.OrchestralHybrid => 100, MusicStyle.DangdutKoplo => 118, MusicStyle.Baleganjur => 140,
            MusicStyle.ReogGamelan => 124, MusicStyle.Punk => 184, MusicStyle.Horeg => 128, _ => 100,
        };
        float beat = 60f / bpm;
        float bar = beat * 4;
        float length = bar * bars;
        var buffer = new StereoBuffer((int)(length * Synth.SampleRate)) { Wrap = true };

        switch (style)
        {
            case MusicStyle.Gamelan: Gamelan(buffer, rng, beat, bars, s_slendro, 196f, reog: false); break;
            case MusicStyle.ReogGamelan: Gamelan(buffer, rng, beat, bars, s_pelog, 233f, reog: true); break;
            case MusicStyle.DegungSunda: Degung(buffer, rng, beat, bars); break;
            case MusicStyle.Angklung: AngklungEnsemble(buffer, rng, beat, bars); break;
            case MusicStyle.Talempong: TalempongEnsemble(buffer, rng, beat, bars); break;
            case MusicStyle.Sasando: SasandoBallad(buffer, rng, beat, bars); break;
            case MusicStyle.Baleganjur: Baleganjur(buffer, rng, beat, bars); break;
            case MusicStyle.MetalHardcore: Band(buffer, rng, beat, bars, metal: true, punk: false); break;
            case MusicStyle.Rock: Band(buffer, rng, beat, bars, metal: false, punk: false); break;
            case MusicStyle.Punk: Band(buffer, rng, beat, bars, metal: false, punk: true); break;
            case MusicStyle.Edm: Electronic(buffer, rng, beat, bars, horeg: false); break;
            case MusicStyle.Horeg: Electronic(buffer, rng, beat, bars, horeg: true); break;
            case MusicStyle.DangdutKoplo: Koplo(buffer, rng, beat, bars); break;
            default: Orchestral(buffer, rng, beat, bars); break;
        }
        return buffer;
    }

    private static float Note(float baseHz, float[] scale, int degree)
    {
        int n = scale.Length;
        int octave = (int)Math.Floor(degree / (double)n);
        int step = ((degree % n) + n) % n;
        return Synth.Cents(baseHz, scale[step] + octave * 1200);
    }

    /// <summary>A looping melody: random walk over the scale, repeated with variation so it ends where it began.</summary>
    private static int[] Melody(Random rng, int length, int range)
    {
        var motif = new int[length / 2];
        int d = 0;
        for (int i = 0; i < motif.Length; i++)
        {
            d = Math.Clamp(d + rng.Next(-2, 3), -1, range);
            motif[i] = d;
        }
        var result = new int[length];
        for (int i = 0; i < length; i++) result[i] = i < motif.Length ? motif[i] : motif[i - motif.Length] + (i % 4 == 3 ? 1 : 0);
        result[^1] = result[0];
        return result;
    }

    private static void Gamelan(StereoBuffer b, Random rng, float beat, int bars, float[] scale, float baseHz, bool reog)
    {
        int steps = bars * 4;
        var balungan = Melody(rng, steps, scale.Length + 1);
        for (int i = 0; i < steps; i++)
        {
            float t = i * beat;
            // Saron plays the core melody, bonang doubles it at twice the speed an octave up.
            Synth.Metallophone(b, t, Note(baseHz, scale, balungan[i]), 0.5f, 1.4f, -0.3f);
            Synth.Metallophone(b, t, Note(baseHz * 2, scale, balungan[i]), 0.22f, 0.7f, 0.35f, 4f);
            Synth.Metallophone(b, t + beat / 2, Note(baseHz * 2, scale, balungan[(i + 1) % steps]), 0.18f, 0.6f, 0.4f, 4f);
            if (i % 4 == 2) Synth.Metallophone(b, t, Note(baseHz / 2, scale, balungan[i]), 0.35f, 2.2f, 0.1f, 2f); // kenong
            if (i % 2 == 1) Synth.Metallophone(b, t, Note(baseHz / 2, scale, 2), 0.18f, 0.5f, -0.1f, 2f); // kethuk
            Synth.Kendang(b, t, open: i % 4 == 0, 0.45f, 0.1f);
            if (reog || i % 2 == 0) Synth.Kendang(b, t + beat * 0.5f, open: false, 0.3f, -0.1f);
            if (reog) Synth.Kendang(b, t + beat * 0.75f, open: true, 0.25f, 0.2f);
        }
        for (int bar = 0; bar < bars; bar += 4) Synth.Gong(b, bar * 4 * beat, 58f, 0.9f, Math.Min(7f, 16 * beat));
        if (reog)
        {
            // Slompret: the shrill reed that leads the reog troupe.
            var tune = Melody(rng, steps, 5);
            for (int i = 0; i < steps; i += 2) Synth.Flute(b, i * beat, Note(baseHz * 2, scale, tune[i]), 0.35f, beat * 1.9f, 0.15f, reed: true);
        }
    }

    private static void Degung(StereoBuffer b, Random rng, float beat, int bars)
    {
        int steps = bars * 4;
        var tune = Melody(rng, steps * 2, 5);
        for (int i = 0; i < steps * 2; i++)
            Synth.Metallophone(b, i * beat / 2, Note(293f, s_degung, tune[i]), 0.3f, 0.9f, (i % 2) * 0.6f - 0.3f, 3.5f);
        var flute = Melody(rng, steps, 6);
        for (int i = 0; i < steps; i += 2) Synth.Flute(b, i * beat, Note(587f, s_degung, flute[i]), 0.45f, beat * 1.95f, 0.2f);
        for (int i = 0; i < steps; i++) Synth.Kendang(b, i * beat, open: i % 2 == 0, 0.3f);
        for (int bar = 0; bar < bars; bar += 2) Synth.Gong(b, bar * 4 * beat, 65f, 0.7f, 8 * beat);
    }

    private static void AngklungEnsemble(StereoBuffer b, Random rng, float beat, int bars)
    {
        int steps = bars * 4;
        var tune = Melody(rng, steps * 2, 7);
        for (int i = 0; i < steps * 2; i++)
            Synth.Angklung(b, i * beat / 2, Note(392f, s_minor, tune[i]), 0.45f, beat * 0.9f, (i % 3 - 1) * 0.35f);
        for (int i = 0; i < steps; i++)
        {
            Synth.Angklung(b, i * beat, Note(196f, s_minor, i % 8 < 4 ? 0 : 3), 0.35f, beat * 1.2f, 0);
            Synth.Kendang(b, i * beat, open: i % 4 == 0, 0.35f);
        }
    }

    private static void TalempongEnsemble(StereoBuffer b, Random rng, float beat, int bars)
    {
        // Minangkabau talempong: interlocking gong-chime patterns with gandang drums.
        int steps = bars * 8;
        var a = Melody(rng, steps, 5);
        for (int i = 0; i < steps; i++)
        {
            float t = i * beat / 2;
            Synth.Metallophone(b, t, Note(523f, s_pelog, a[i]), 0.32f, 0.45f, i % 2 == 0 ? -0.4f : 0.4f, 5f);
            if (i % 2 == 0) Synth.Kendang(b, t, open: i % 8 == 0, 0.35f);
        }
        var suling = Melody(rng, bars * 4, 6);
        for (int i = 0; i < bars * 4; i += 2) Synth.Flute(b, i * beat, Note(784f, s_pelog, suling[i]), 0.3f, beat * 1.9f, 0.1f);
    }

    private static void SasandoBallad(StereoBuffer b, Random rng, float beat, int bars)
    {
        int steps = bars * 8;
        var tune = Melody(rng, steps, 9);
        int[] chords = [0, 5, 3, 4];
        for (int i = 0; i < steps; i++)
        {
            int chord = chords[(i / 8) % chords.Length];
            Synth.Pluck(b, i * beat / 2, Note(220f, s_minor, chord + (i % 3) * 2), 0.4f, 1.8f, (i % 2) * 0.5f - 0.25f, 0.997f);
            if (i % 2 == 0) Synth.Pluck(b, i * beat / 2, Note(440f, s_minor, tune[i]), 0.35f, 1.4f, 0.3f);
        }
        for (int i = 0; i < bars; i++) Synth.Pluck(b, i * beat * 4, Note(110f, s_minor, chords[i % 4]), 0.5f, beat * 4, 0, 0.998f);
    }

    private static void Baleganjur(StereoBuffer b, Random rng, float beat, int bars)
    {
        // Balinese processional: kotekan between two reyong parts, crashing ceng-ceng, driving kendang.
        int steps = bars * 8;
        var polos = Melody(rng, steps, 4);
        for (int i = 0; i < steps; i++)
        {
            float t = i * beat / 2;
            Synth.Metallophone(b, t, Note(466f, s_pelog, polos[i]), 0.25f, 0.35f, -0.45f, 6f);
            Synth.Metallophone(b, t + beat / 4, Note(466f, s_pelog, polos[i] + 1), 0.22f, 0.35f, 0.45f, 6f);
            Synth.Cymbal(b, t, i % 2 == 0 ? 0.35f : 0.2f, (i % 3 - 1) * 0.3f);
            Synth.Kendang(b, t, open: i % 4 == 0, 0.5f, 0);
        }
        for (int i = 0; i < bars * 4; i += 2) Synth.Metallophone(b, i * beat, 110f, 0.4f, 1.2f, 0, 2f);
        for (int bar = 0; bar < bars; bar += 2) Synth.Gong(b, bar * 4 * beat, 52f, 0.8f, 8 * beat);
    }

    private static void Band(StereoBuffer b, Random rng, float beat, int bars, bool metal, bool punk)
    {
        float root = metal ? 82.4f : 110f;
        int[] riff = metal ? [0, 0, 1, 0, 3, 0, 1, 5] : punk ? [0, 0, 5, 5, 3, 3, 4, 4] : [0, 3, 5, 3, 0, 5, 6, 5];
        var scale = metal ? s_phrygian : s_minor;
        for (int bar = 0; bar < bars; bar++)
        {
            for (int s = 0; s < 8; s++)
            {
                float t = (bar * 8 + s) * beat / 2;
                int degree = riff[(bar % 2 == 0 ? s : (s + 4)) % riff.Length];
                Synth.PowerChord(b, t, Note(root, scale, degree), punk ? 0.2f : 0.18f, beat / 2 * (metal ? 0.6f : 0.9f), s % 2 == 0 ? -0.5f : 0.5f);
                Synth.Saw(b, t, Note(root / 2, scale, degree), 0.35f, beat / 2 * 0.9f, 0, 0.05f, 2f);
                Synth.Hat(b, t, 0.25f);
                if (metal) Synth.Kick(b, t, 0.7f, 0.8f);
            }
            for (int q = 0; q < 4; q++)
            {
                float t = (bar * 4 + q) * beat;
                if (!metal) { if (q % 2 == 0) Synth.Kick(b, t, 0.8f); }
                if (q % 2 == 1 || punk) Synth.Snare(b, t + (punk && q % 2 == 0 ? beat / 2 : 0), 0.55f);
            }
        }
        if (!metal && !punk)
        {
            var lead = Melody(rng, bars * 4, 8);
            for (int i = 0; i < bars * 4; i++) Synth.Saw(b, i * beat, Note(root * 4, scale, lead[i]), 0.08f, beat * 0.95f, 0.2f, 0.2f, 3f);
        }
    }

    private static void Electronic(StereoBuffer b, Random rng, float beat, int bars, bool horeg)
    {
        float root = horeg ? 43.6f : 55f;
        var lead = Melody(rng, bars * 8, 6);
        for (int bar = 0; bar < bars; bar++)
        {
            for (int q = 0; q < 4; q++)
            {
                float t = (bar * 4 + q) * beat;
                Synth.Kick(b, t, horeg ? 1.1f : 0.9f, horeg ? 1.3f : 1f);
                Synth.Hat(b, t + beat / 2, 0.35f, 0.04f);
                if (q % 2 == 1) Synth.Snare(b, t, horeg ? 0.3f : 0.45f);
                if (horeg)
                {
                    // Koplo kendang on top of the four-on-the-floor, as horeg carnivals love it.
                    Synth.Kendang(b, t + beat * 0.25f, open: false, 0.35f, 0.2f);
                    Synth.Kendang(b, t + beat * 0.75f, open: true, 0.4f, -0.2f);
                }
            }
            if (horeg) Synth.Sub808(b, bar * 4 * beat, Note(root, s_minor, bar % 4 == 3 ? 3 : 0), 0.9f, beat * 3.8f);
            else
                for (int e = 0; e < 8; e++) Synth.Saw(b, (bar * 8 + e) * beat / 2 + beat / 4, Note(root * 2, s_minor, bar % 4 == 3 ? 3 : 0), 0.3f, beat / 4, 0, 0.12f, 2f);
            for (int e = 0; e < 8; e++)
            {
                float t = (bar * 8 + e) * beat / 2;
                Synth.Saw(b, t, Note(horeg ? 349f : 440f, s_minor, lead[bar * 8 + e]), horeg ? 0.12f : 0.1f, beat * 0.45f, e % 2 == 0 ? -0.3f : 0.3f, 0.35f, 2.5f, 0.01f);
            }
        }
    }

    private static void Koplo(StereoBuffer b, Random rng, float beat, int bars)
    {
        // Dangdut koplo: the kendang "tak-dut" groove carries everything; bass, suling and a tambourine on top.
        var suling = Melody(rng, bars * 4, 7);
        for (int bar = 0; bar < bars; bar++)
        {
            for (int q = 0; q < 4; q++)
            {
                float t = (bar * 4 + q) * beat;
                Synth.Kendang(b, t, open: true, 0.6f, -0.1f);
                Synth.Kendang(b, t + beat * 0.5f, open: false, 0.45f, 0.1f);
                Synth.Kendang(b, t + beat * 0.75f, open: q % 2 == 1, 0.35f, 0.15f);
                Synth.Hat(b, t + beat * 0.5f, 0.3f, 0.07f, -0.4f);
                Synth.Saw(b, t, Note(73.4f, s_minor, q == 3 ? 4 : 0), 0.45f, beat * 0.45f, 0, 0.05f, 1.5f);
                Synth.Saw(b, t + beat / 2, Note(146.8f, s_minor, q == 3 ? 4 : 0), 0.25f, beat * 0.4f, 0, 0.05f, 1.5f);
                Synth.Flute(b, t, Note(587f, s_minor, suling[bar * 4 + q]), 0.35f, beat * 0.95f, 0.25f);
            }
            Synth.Pluck(b, bar * 4 * beat, Note(220f, s_minor, 0), 0.25f, beat * 2, -0.3f);
        }
    }

    private static void Orchestral(StereoBuffer b, Random rng, float beat, int bars)
    {
        int[] chords = [0, 5, 3, 4];
        var theme = Melody(rng, bars * 4, 8);
        for (int bar = 0; bar < bars; bar++)
        {
            int c = chords[bar % chords.Length];
            float t = bar * 4 * beat;
            Synth.Pad(b, t, Note(130.8f, s_minor, c), 0.3f, beat * 4.2f, -0.3f);
            Synth.Pad(b, t, Note(130.8f, s_minor, c + 2), 0.25f, beat * 4.2f, 0.3f);
            Synth.Pad(b, t, Note(130.8f, s_minor, c + 4), 0.2f, beat * 4.2f, 0);
            for (int q = 0; q < 4; q++)
            {
                Synth.Kick(b, t + q * beat, q % 2 == 0 ? 0.8f : 0.4f, 0.6f); // taiko-like hits
                Synth.Kendang(b, t + q * beat + beat / 2, open: false, 0.25f);
                Synth.Metallophone(b, t + q * beat, Note(523f, s_minor, theme[bar * 4 + q]), 0.25f, 1.2f, 0.2f);
            }
        }
        for (int bar = 0; bar < bars; bar += 4) Synth.Gong(b, bar * 4 * beat, 55f, 0.8f, 6f);
    }
}
