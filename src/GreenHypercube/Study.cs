using System.Threading.Tasks;

namespace GreenHypercube;

public enum NullKind
{
    None,
    PermuteReward,
    PermuteRewardWithinEffort,
}

public sealed class StudySpec
{
    public int Landscapes { get; init; } = 48;
    public int N { get; init; } = 120;
    public int Budget { get; init; } = 70;
    public ulong Seed { get; init; } = 100;
    public double SignalStrength { get; init; }
    public double EffortStrength { get; init; } = 0.5;
    public double CueFromEffort { get; init; }
    public double RewardDensity { get; init; } = 0.15;
    public int RandomReplicates { get; init; } = 8;
    public int EffortStrata { get; init; } = 5;
}

public readonly struct EnsembleResult
{
    public EnsembleResult(double mean, double standardError, int landscapes)
    {
        Mean = mean;
        StandardError = standardError;
        Landscapes = landscapes;
        var half = 1.96 * standardError;
        Ci95Low = mean - half;
        Ci95High = mean + half;
    }

    public double Mean { get; }
    public double StandardError { get; }
    public double Ci95Low { get; }
    public double Ci95High { get; }
    public int Landscapes { get; }

    public bool IntervalContains(double value) => value >= Ci95Low && value <= Ci95High;
}

public readonly struct StudyProgress
{
    public StudyProgress(int completed, int total)
    {
        Completed = completed;
        Total = total;
    }

    public int Completed { get; }
    public int Total { get; }
}

public static class Study
{
    public static EnsembleResult SensoryAdvantage(
        StudySpec spec,
        NullKind nullKind,
        IProgress<StudyProgress>? progress = null)
    {
        if (spec.Landscapes < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(spec), "Need at least two landscapes for an interval.");
        }

        var diffs = new double[spec.Landscapes];
        var completed = 0;
        Parallel.For(0, spec.Landscapes, k =>
        {
            diffs[k] = OneLandscape(spec, nullKind, (ulong)k);
            var n = Interlocked.Increment(ref completed);
            progress?.Report(new StudyProgress(n, spec.Landscapes));
        });

        return Summarize(diffs);
    }

    public static EnsembleResult Summarize(ReadOnlySpan<double> diffs)
    {
        var n = diffs.Length;
        var mean = 0.0;
        for (var i = 0; i < n; i++)
        {
            mean += diffs[i];
        }

        mean /= n;
        var ss = 0.0;
        for (var i = 0; i < n; i++)
        {
            var d = diffs[i] - mean;
            ss += d * d;
        }

        var se = Math.Sqrt(ss / (n - 1)) / Math.Sqrt(n);
        return new EnsembleResult(mean, se, n);
    }

    private static double OneLandscape(StudySpec spec, NullKind nullKind, ulong landscape)
    {
        var world = Landscape.Generate(
            spec.N,
            spec.SignalStrength,
            spec.RewardDensity,
            spec.Seed,
            landscape,
            spec.EffortStrength,
            spec.CueFromEffort);

        world = ApplyNull(world, spec, nullKind, landscape);

        var sensoryRng = Pcg32.Stream(spec.Seed, landscape, RngPurpose.Sensory);
        var sensory = Engine.RunEpisode(
            world,
            new SensorySearch(world.Cues),
            spec.Budget,
            ref sensoryRng);

        var randomSum = 0.0;
        for (var r = 0; r < spec.RandomReplicates; r++)
        {
            var randomRng = Pcg32.Stream(spec.Seed, landscape, RngPurpose.RandomBase + (ulong)r);
            randomSum += Engine.RunEpisode(world, new RandomSearch(), spec.Budget, ref randomRng).Audc();
        }

        return sensory.Audc() - (randomSum / spec.RandomReplicates);
    }

    private static World ApplyNull(World world, StudySpec spec, NullKind nullKind, ulong landscape)
    {
        if (nullKind == NullKind.None)
        {
            return world;
        }

        var rng = Pcg32.Stream(spec.Seed, landscape, RngPurpose.Permute);
        return nullKind switch
        {
            NullKind.PermuteReward => world.PermuteReward(ref rng),
            NullKind.PermuteRewardWithinEffort => world.PermuteRewardWithinEffort(ref rng, spec.EffortStrata),
            _ => world,
        };
    }
}
