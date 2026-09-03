namespace GreenHypercube;

/// <summary>
/// Observable cues. Strategies receive this type and nothing else.
/// There is no reward, effort, or assay on this surface.
/// </summary>
public interface ICueView
{
    int N { get; }
    ReadOnlySpan<double> SensorySalience { get; }
}

public sealed class CueView : ICueView
{
    private readonly double[] _salience;

    internal CueView(double[] salience)
    {
        _salience = salience;
    }

    public int N => _salience.Length;

    public ReadOnlySpan<double> SensorySalience => _salience;
}

/// <summary>
/// Hidden assay. Only <see cref="SearchEnvironment.Experiment"/> spends a
/// draw against this vault. Strategies never receive this type.
/// </summary>
internal sealed class AssayVault
{
    private readonly double[] _reward;

    internal AssayVault(double[] reward, double discoveryThreshold)
    {
        _reward = reward;
        DiscoveryThreshold = discoveryThreshold;
        var useful = 0;
        for (var i = 0; i < reward.Length; i++)
        {
            if (reward[i] >= discoveryThreshold)
            {
                useful++;
            }
        }

        UsefulCount = useful;
    }

    internal double DiscoveryThreshold { get; }
    internal int UsefulCount { get; }
    internal int Length => _reward.Length;

    internal double Reveal(int index) => _reward[index];

    internal double[] CloneRewards() => (double[])_reward.Clone();
}

/// <summary>
/// One synthetic landscape: cues for search, effort for nulls, assay for the engine.
/// </summary>
public sealed class World
{
    private readonly double[] _effort;
    private readonly AssayVault _assay;

    internal World(CueView cues, double[] effort, AssayVault assay)
    {
        Cues = cues;
        _effort = effort;
        _assay = assay;
    }

    public ICueView Cues { get; }

    /// <summary>Study-effort index. Not part of <see cref="ICueView"/>.</summary>
    public ReadOnlySpan<double> Effort => _effort;

    internal AssayVault Assay => _assay;

    internal double[] CopyRewards() => _assay.CloneRewards();

    public World PermuteReward(ref Pcg32 rng)
    {
        var reward = _assay.CloneRewards();
        Shuffle(reward, ref rng);
        return new World((CueView)Cues, _effort, new AssayVault(reward, _assay.DiscoveryThreshold));
    }

    public World PermuteRewardWithinEffort(ref Pcg32 rng, int strata = 5)
    {
        if (strata < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(strata));
        }

        var n = Cues.N;
        var min = _effort[0];
        var max = _effort[0];
        for (var i = 1; i < n; i++)
        {
            if (_effort[i] < min)
            {
                min = _effort[i];
            }

            if (_effort[i] > max)
            {
                max = _effort[i];
            }
        }

        if (max - min <= 1e-12)
        {
            return PermuteReward(ref rng);
        }

        var bins = EffortBins(_effort, strata);
        var reward = _assay.CloneRewards();
        var bucket = new List<int>();
        for (var b = 0; b < strata; b++)
        {
            bucket.Clear();
            for (var i = 0; i < n; i++)
            {
                if (bins[i] == b)
                {
                    bucket.Add(i);
                }
            }

            if (bucket.Count < 2)
            {
                continue;
            }

            var values = new double[bucket.Count];
            for (var k = 0; k < bucket.Count; k++)
            {
                values[k] = reward[bucket[k]];
            }

            Shuffle(values, ref rng);
            for (var k = 0; k < bucket.Count; k++)
            {
                reward[bucket[k]] = values[k];
            }
        }

        return new World((CueView)Cues, _effort, new AssayVault(reward, _assay.DiscoveryThreshold));
    }

    internal static int[] EffortBins(ReadOnlySpan<double> effort, int strata)
    {
        var n = effort.Length;
        var sorted = new double[n];
        effort.CopyTo(sorted);
        Array.Sort(sorted);
        var edges = new double[strata + 1];
        for (var k = 0; k <= strata; k++)
        {
            edges[k] = Quantile(sorted, k / (double)strata);
        }

        edges[strata] += 1e-9;
        var bins = new int[n];
        for (var i = 0; i < n; i++)
        {
            var b = 0;
            while (b < strata - 1 && effort[i] >= edges[b + 1])
            {
                b++;
            }

            bins[i] = b;
        }

        return bins;
    }

    private static double Quantile(double[] sorted, double p)
    {
        if (p <= 0)
        {
            return sorted[0];
        }

        if (p >= 1)
        {
            return sorted[^1];
        }

        var pos = p * (sorted.Length - 1);
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        if (lo == hi)
        {
            return sorted[lo];
        }

        var w = pos - lo;
        return sorted[lo] * (1 - w) + sorted[hi] * w;
    }

    private static void Shuffle(double[] values, ref Pcg32 rng)
    {
        for (var i = values.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }
}
