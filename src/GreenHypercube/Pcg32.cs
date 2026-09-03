namespace GreenHypercube;

/// <summary>
/// PCG-XSH-RR 64/32. Named streams, not <see cref="Random"/>.
/// <c>Stream(seed, landscape, purpose)</c> is independent of call order, so
/// landscape k is the same whether you run sequential or <c>Parallel.For</c>.
/// </summary>
public struct Pcg32
{
    private ulong _state;
    private ulong _inc;

    private const ulong Multiplier = 6364136223846793005UL;

    public static Pcg32 Stream(ulong seed, ulong landscape, ulong purpose)
    {
        var mixed = SplitMix(seed) ^ SplitMix(landscape * 0x9E3779B97F4A7C15UL + purpose);
        return Create(mixed, (landscape << 32) ^ purpose);
    }

    public static Pcg32 Create(ulong seed, ulong stream = 1)
    {
        var rng = new Pcg32
        {
            _state = 0,
            _inc = (stream << 1) | 1UL,
        };
        rng.NextUInt32();
        rng._state += seed;
        rng.NextUInt32();
        return rng;
    }

    public uint NextUInt32()
    {
        var old = _state;
        _state = unchecked(old * Multiplier + _inc);
        var xorshifted = (uint)(((old >> 18) ^ old) >> 27);
        var rot = (int)(old >> 59);
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

        return (int)(NextUInt32() % (uint)maxExclusive);
    }

    public double NextDouble()
    {
        return (NextUInt32() >> 8) * (1.0 / (1 << 24));
    }

    public double NextGaussian()
    {
        var u1 = 1.0 - NextDouble();
        var u2 = 1.0 - NextDouble();
        return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
    }

    private static ulong SplitMix(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }
}

public static class RngPurpose
{
    public const ulong Generate = 1;
    public const ulong Permute = 2;
    public const ulong Sensory = 3;
    public const ulong RandomBase = 100;
}
