namespace TarkamArena.Engine.Audio;

/// <summary>
/// Small additive/subtractive synthesizer that renders instruments straight into a stereo float
/// buffer. Notes are written one at a time into a preallocated track, so a whole loop renders in a
/// fraction of a second at load time and nothing runs on the audio thread.
/// </summary>
public sealed class StereoBuffer(int frames, int sampleRate = Synth.SampleRate)
{
    public readonly float[] Left = new float[frames];
    public readonly float[] Right = new float[frames];
    public int Frames => Left.Length;
    public int SampleRate { get; } = sampleRate;

    /// <summary>Music loops wrap note tails back to the start so the loop point is seamless.</summary>
    public bool Wrap { get; init; }

    public void Add(int frame, float value, float pan)
    {
        if ((uint)frame >= (uint)Left.Length)
        {
            if (!Wrap || frame < 0) return;
            frame %= Left.Length;
        }
        Left[frame] += value * (1f - MathF.Max(0, pan));
        Right[frame] += value * (1f + MathF.Min(0, pan));
    }

    /// <summary>Soft-clips and interleaves into the layout <c>AudioEngine.CreateClip</c> expects.</summary>
    public float[] ToInterleaved(float gain = 1f)
    {
        float peak = 1e-6f;
        for (int i = 0; i < Left.Length; i++) peak = MathF.Max(peak, MathF.Max(MathF.Abs(Left[i]), MathF.Abs(Right[i])));
        // Normalise every clip to the same peak; playback gain decides the final loudness.
        float norm = gain * 0.9f / peak;
        var data = new float[Left.Length * 2];
        for (int i = 0; i < Left.Length; i++)
        {
            data[i * 2] = MathF.Tanh(Left[i] * norm * 1.2f);
            data[i * 2 + 1] = MathF.Tanh(Right[i] * norm * 1.2f);
        }
        return data;
    }
}

public static class Synth
{
    public const int SampleRate = 44100;
    private const float Tau = MathF.PI * 2f;

    // Fast deterministic white noise.
    private static uint s_noise = 0x12345678;

    private static float Noise()
    {
        s_noise ^= s_noise << 13;
        s_noise ^= s_noise >> 17;
        s_noise ^= s_noise << 5;
        return (s_noise & 0xFFFFFF) / (float)0x7FFFFF - 1f;
    }

    public static float Midi(float note) => 440f * MathF.Pow(2f, (note - 69f) / 12f);

    public static float Cents(float baseHz, float cents) => baseHz * MathF.Pow(2f, cents / 1200f);

    /// <summary>Gamelan-style bronze key (saron, bonang, kenong): inharmonic partials plus the "ombak" beating of a detuned twin.</summary>
    public static void Metallophone(StereoBuffer b, float start, float freq, float amp, float decay = 1.8f, float pan = 0, float beat = 3f)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(decay * 3.5f * b.SampleRate);
        ReadOnlySpan<float> ratios = [1f, 2.76f, 5.4f, 8.93f];
        ReadOnlySpan<float> amps = [1f, 0.45f, 0.22f, 0.08f];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float env = MathF.Exp(-t / decay) * MathF.Min(1, t * 400f);
            float v = 0;
            for (int p = 0; p < ratios.Length; p++)
            {
                float pd = MathF.Exp(-t * p * 1.5f);
                v += amps[p] * pd * (MathF.Sin(Tau * freq * ratios[p] * t) + 0.6f * MathF.Sin(Tau * (freq + beat) * ratios[p] * t));
            }
            b.Add(s0 + i, v * env * amp * 0.5f, pan);
        }
    }

    /// <summary>Large hanging gong: slow bloom, very long tail.</summary>
    public static void Gong(StereoBuffer b, float start, float freq, float amp, float length = 6f)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float env = MathF.Exp(-t / (length * 0.35f)) * MathF.Min(1, t * 30f);
            float wobble = 1f + 0.004f * MathF.Sin(Tau * 4.5f * t);
            float v = MathF.Sin(Tau * freq * t * wobble) + 0.5f * MathF.Sin(Tau * freq * 1.48f * t) * MathF.Exp(-t * 0.8f)
                    + 0.3f * MathF.Sin(Tau * freq * 2.12f * t) * MathF.Exp(-t * 1.6f) + 0.15f * Noise() * MathF.Exp(-t * 25f);
            b.Add(s0 + i, v * env * amp, 0);
        }
    }

    /// <summary>Kendang: <paramref name="open"/> is the deep "dut", otherwise the slapped "tak".</summary>
    public static void Kendang(StereoBuffer b, float start, bool open, float amp, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(0.35f * b.SampleRate);
        float phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float v;
            if (open)
            {
                float f = 70f + 90f * MathF.Exp(-t * 18f);
                phase += Tau * f / b.SampleRate;
                v = MathF.Sin(phase) * MathF.Exp(-t * 9f);
            }
            else
            {
                phase += Tau * 340f / b.SampleRate;
                v = (0.6f * MathF.Sin(phase) + 0.7f * Noise()) * MathF.Exp(-t * 45f);
            }
            b.Add(s0 + i, v * amp, pan);
        }
    }

    /// <summary>Angklung: shaken bamboo tubes, a fast tremolo over an octave pair.</summary>
    public static void Angklung(StereoBuffer b, float start, float freq, float amp, float length = 0.7f, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float trem = 0.55f + 0.45f * MathF.Sin(Tau * 13f * t);
            float env = MathF.Min(1, t * 60f) * MathF.Exp(-t * 2.2f);
            float v = MathF.Sin(Tau * freq * t) + 0.5f * MathF.Sin(Tau * freq * 2f * t) + 0.12f * MathF.Sin(Tau * freq * 3.02f * t);
            b.Add(s0 + i, v * trem * env * amp * 0.6f, pan);
        }
    }

    /// <summary>Suling bamboo flute, or a slompret reed when <paramref name="reed"/> is set.</summary>
    public static void Flute(StereoBuffer b, float start, float freq, float amp, float length, float pan = 0, bool reed = false)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float env = MathF.Min(1, t * 14f) * MathF.Min(1, (length - t) * 10f);
            float vib = 1f + 0.012f * MathF.Sin(Tau * 5.2f * t) * MathF.Min(1, t * 3);
            phase += Tau * freq * vib / b.SampleRate;
            float v = reed
                ? MathF.Tanh(3f * MathF.Sin(phase)) * 0.5f + 0.3f * MathF.Sin(phase * 2f)
                : MathF.Sin(phase) + 0.15f * MathF.Sin(phase * 2f) + 0.08f * Noise();
            b.Add(s0 + i, v * env * amp * 0.5f, pan);
        }
    }

    /// <summary>Plucked string (sasando, guitar) via Karplus-Strong.</summary>
    public static void Pluck(StereoBuffer b, float start, float freq, float amp, float length = 1.6f, float pan = 0, float damping = 0.996f)
    {
        int s0 = (int)(start * b.SampleRate);
        int period = Math.Max(2, (int)(b.SampleRate / freq));
        Span<float> ring = period <= 2048 ? stackalloc float[period] : new float[period];
        for (int i = 0; i < period; i++) ring[i] = Noise();
        int n = (int)(length * b.SampleRate);
        int idx = 0;
        for (int i = 0; i < n; i++)
        {
            int next = (idx + 1) % period;
            float v = ring[idx];
            ring[idx] = (ring[idx] + ring[next]) * 0.5f * damping;
            idx = next;
            b.Add(s0 + i, v * amp * 0.7f, pan);
        }
    }

    public static void Kick(StereoBuffer b, float start, float amp, float punch = 1f)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(0.42f * b.SampleRate);
        float phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float f = 44f + 120f * punch * MathF.Exp(-t * 32f);
            phase += Tau * f / b.SampleRate;
            float v = MathF.Sin(phase) * MathF.Exp(-t * 7f) + 0.25f * Noise() * MathF.Exp(-t * 200f);
            b.Add(s0 + i, v * amp, 0);
        }
    }

    /// <summary>Long distorted 808 sub, the heart of sound horeg.</summary>
    public static void Sub808(StereoBuffer b, float start, float freq, float amp, float length)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float f = freq * (1f + 1.5f * MathF.Exp(-t * 28f));
            phase += Tau * f / b.SampleRate;
            float env = MathF.Min(1, (length - t) * 20f) * (0.7f + 0.3f * MathF.Exp(-t * 3f));
            float v = MathF.Tanh(2.2f * MathF.Sin(phase));
            b.Add(s0 + i, v * env * amp, 0);
        }
    }

    public static void Snare(StereoBuffer b, float start, float amp, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(0.22f * b.SampleRate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float v = (0.8f * Noise() + 0.5f * MathF.Sin(Tau * 190f * t)) * MathF.Exp(-t * 22f);
            b.Add(s0 + i, v * amp, pan);
        }
    }

    public static void Hat(StereoBuffer b, float start, float amp, float length = 0.05f, float pan = 0.3f)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * 3 * b.SampleRate);
        float last = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float x = Noise();
            float hp = x - last;
            last = x;
            b.Add(s0 + i, hp * MathF.Exp(-t / length) * amp * 0.6f, pan);
        }
    }

    /// <summary>Ceng-ceng: clashing Balinese cymbals.</summary>
    public static void Cymbal(StereoBuffer b, float start, float amp, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(0.25f * b.SampleRate);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float v = (Noise() * 0.7f + 0.4f * MathF.Sin(Tau * 5300f * t) * MathF.Sin(Tau * 7100f * t)) * MathF.Exp(-t * 16f);
            b.Add(s0 + i, v * amp, pan);
        }
    }

    /// <summary>Filtered saw bass or lead.</summary>
    public static void Saw(StereoBuffer b, float start, float freq, float amp, float length, float pan = 0, float cutoff = 0.08f, float drive = 1f, float detune = 0f)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float p1 = 0, p2 = 0, lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            p1 += freq / b.SampleRate;
            p2 += freq * (1 + detune) / b.SampleRate;
            p1 -= MathF.Floor(p1);
            p2 -= MathF.Floor(p2);
            float raw = (p1 * 2 - 1) + (detune > 0 ? (p2 * 2 - 1) : 0);
            lp += (raw - lp) * cutoff;
            float env = MathF.Min(1, t * 200f) * MathF.Min(1, (length - t) * 40f);
            float v = drive > 1 ? MathF.Tanh(lp * drive) : lp;
            b.Add(s0 + i, v * env * amp, pan);
        }
    }

    /// <summary>Distorted power chord (root, fifth, octave).</summary>
    public static void PowerChord(StereoBuffer b, float start, float root, float amp, float length, float pan)
    {
        Saw(b, start, root, amp, length, pan, 0.25f, 6f, 0.004f);
        Saw(b, start, root * 1.4983f, amp * 0.8f, length, -pan, 0.25f, 6f, 0.003f);
        Saw(b, start, root * 2f, amp * 0.5f, length, pan * 0.5f, 0.25f, 6f);
    }

    /// <summary>Slow-attack string pad for the orchestral hybrid tracks.</summary>
    public static void Pad(StereoBuffer b, float start, float freq, float amp, float length, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float p1 = 0, p2 = 0, p3 = 0, lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            p1 += freq / b.SampleRate; p1 -= MathF.Floor(p1);
            p2 += freq * 1.006f / b.SampleRate; p2 -= MathF.Floor(p2);
            p3 += freq * 0.994f / b.SampleRate; p3 -= MathF.Floor(p3);
            float raw = (p1 + p2 + p3) * 2f / 3f - 1f;
            lp += (raw - lp) * 0.03f;
            float env = MathF.Min(1, t * 1.5f) * MathF.Min(1, (length - t) * 2f);
            b.Add(s0 + i, lp * env * amp, pan);
        }
    }

    /// <summary>Generic noise sweep for whooshes, crowd swells, fire and water.</summary>
    public static void NoiseSweep(StereoBuffer b, float start, float length, float amp, float fromCutoff, float toCutoff, float attack = 0.02f, float pan = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float k = fromCutoff + (toCutoff - fromCutoff) * (t / length);
            lp += (Noise() - lp) * k;
            float env = MathF.Min(1, t / attack) * MathF.Min(1, (length - t) * 8f);
            b.Add(s0 + i, lp * env * amp, pan);
        }
    }

    public static void Tone(StereoBuffer b, float start, float freq, float amp, float length, float decay = 20f, float pan = 0, float slideTo = 0)
    {
        int s0 = (int)(start * b.SampleRate);
        int n = (int)(length * b.SampleRate);
        float phase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)b.SampleRate;
            float f = slideTo > 0 ? freq + (slideTo - freq) * (t / length) : freq;
            phase += Tau * f / b.SampleRate;
            b.Add(s0 + i, MathF.Sin(phase) * MathF.Exp(-t * decay) * MathF.Min(1, t * 500) * amp, pan);
        }
    }
}
