namespace TarkamArena.Core.Mathematics;

/// <summary>
/// xorshift32 generator whose whole state is one <see cref="uint"/>, so it can live inside a
/// rollback snapshot and be restored by a plain struct copy.
/// </summary>
public static class DeterministicRandom
{
    public static uint Seed(uint seed) => seed == 0 ? 0x9E3779B9u : seed;

    public static uint Next(ref uint state)
    {
        uint x = state == 0 ? 0x9E3779B9u : state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        state = x;
        return x;
    }

    /// <summary>Uniform integer in [0, <paramref name="maxExclusive"/>).</summary>
    public static int Range(ref uint state, int maxExclusive) =>
        maxExclusive <= 0 ? 0 : (int)(Next(ref state) % (uint)maxExclusive);

    public static int Range(ref uint state, int minInclusive, int maxExclusive) =>
        minInclusive + Range(ref state, maxExclusive - minInclusive);

    /// <summary>True with probability <paramref name="percent"/>/100.</summary>
    public static bool Chance(ref uint state, int percent) => Range(ref state, 100) < percent;

    /// <summary>Stable 32-bit FNV-1a hash, used to derive per-character seeds from string ids.</summary>
    public static uint Hash(string text)
    {
        uint hash = 2166136261;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }
}
