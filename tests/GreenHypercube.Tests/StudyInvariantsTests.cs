using GreenHypercube;
using Xunit;

namespace GreenHypercube.Tests;

public sealed class StudyInvariantsTests
{
    private static StudySpec Spec(double signal, double effort = 0.5, double cueFromEffort = 0) => new()
    {
        Landscapes = 24,
        N = 120,
        Budget = 70,
        Seed = 100,
        SignalStrength = signal,
        EffortStrength = effort,
        CueFromEffort = cueFromEffort,
        RewardDensity = 0.15,
        RandomReplicates = 8,
    };

    [Fact]
    public void Zero_signal_interval_does_not_claim_a_win()
    {
        var result = Study.SensoryAdvantage(Spec(0.0), NullKind.None);
        Assert.True(result.Mean < 0.04, $"zero-signal mean {result.Mean:F3}");
        Assert.True(result.Ci95High < 0.08, $"zero-signal CI high {result.Ci95High:F3}");
    }

    [Fact]
    public void Coupled_landscape_interval_is_positive()
    {
        var result = Study.SensoryAdvantage(Spec(0.85), NullKind.None);
        Assert.True(result.Mean > 0.08, $"coupled mean {result.Mean:F3}");
        Assert.True(result.Ci95Low > 0.0, $"coupled CI low {result.Ci95Low:F3}");
    }

    [Fact]
    public void Global_permute_collapses_coupled_advantage()
    {
        var result = Study.SensoryAdvantage(Spec(0.85), NullKind.PermuteReward);
        Assert.True(Math.Abs(result.Mean) < 0.04, $"global permute mean {result.Mean:F3}");
        Assert.True(result.IntervalContains(0.0) || Math.Abs(result.Mean) < 0.04);
    }

    [Fact]
    public void Repeat_runs_match_because_streams_are_named()
    {
        var spec = Spec(0.85);
        var a = Study.SensoryAdvantage(spec, NullKind.None);
        var b = Study.SensoryAdvantage(spec, NullKind.None);
        Assert.Equal(a.Mean, b.Mean, precision: 9);
    }

    [Fact]
    public void Audc_matches_python_mean_discovery_curve()
    {
        var result = new EpisodeResult
        {
            Strategy = "all-hits",
            UsefulInOrder = [true, true, true],
            TotalUseful = 3,
        };
        Assert.Equal(2.0 / 3.0, result.Audc(), precision: 9);
    }

    [Fact]
    public void Global_permute_destroys_salience_reward_correlation()
    {
        var sum = 0.0;
        for (ulong k = 0; k < 24; k++)
        {
            var world = Landscape.Generate(120, 0.85, 0.15, 100, k);
            var rng = Pcg32.Stream(100, k, RngPurpose.Permute);
            var p = world.PermuteReward(ref rng);
            sum += Pearson(ToArray(p.Cues.SensorySalience), p.CopyRewards());
        }

        Assert.True(Math.Abs(sum / 24) < 0.05);
    }

    [Fact]
    public void Global_permute_keeps_cues_and_preserves_reward_mass()
    {
        var world = Landscape.Generate(40, 0.9, 0.2, 7);
        var rng = Pcg32.Stream(99, 0, RngPurpose.Permute);
        var p = world.PermuteReward(ref rng);
        Assert.True(world.Cues.SensorySalience.ToArray().SequenceEqual(p.Cues.SensorySalience.ToArray()));
        Assert.Equal(world.CopyRewards().Sum(), p.CopyRewards().Sum(), precision: 9);
        Assert.False(world.CopyRewards().SequenceEqual(p.CopyRewards()));
    }

    private static double[] ToArray(ReadOnlySpan<double> span)
    {
        var a = new double[span.Length];
        span.CopyTo(a);
        return a;
    }

    private static double Pearson(double[] x, double[] y)
    {
        var n = x.Length;
        var mx = x.Average();
        var my = y.Average();
        var num = 0.0;
        var dx = 0.0;
        var dy = 0.0;
        for (var i = 0; i < n; i++)
        {
            var a = x[i] - mx;
            var b = y[i] - my;
            num += a * b;
            dx += a * a;
            dy += b * b;
        }

        return num / Math.Sqrt(dx * dy);
    }
}
