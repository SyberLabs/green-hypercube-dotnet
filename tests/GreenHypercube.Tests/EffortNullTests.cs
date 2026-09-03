using GreenHypercube;
using Xunit;

namespace GreenHypercube.Tests;

public sealed class EffortNullTests
{
    [Fact]
    public void Feature_cue_dies_when_labels_shuffle_inside_effort_bins_that_are_not_the_cue()
    {
        var spec = new StudySpec
        {
            Landscapes = 24,
            N = 120,
            Budget = 70,
            Seed = 202,
            SignalStrength = 0.85,
            EffortStrength = 0.0,
            CueFromEffort = 0.0,
            RewardDensity = 0.15,
            RandomReplicates = 8,
            EffortStrata = 5,
        };

        var raw = Study.SensoryAdvantage(spec, NullKind.None);
        var within = Study.SensoryAdvantage(spec, NullKind.PermuteRewardWithinEffort);
        Assert.True(raw.Mean > 0.08, $"independent cue should win (mean {raw.Mean:F3})");
        Assert.True(
            Math.Abs(within.Mean) < 0.05,
            $"within-effort shuffle must break a cue that is not effort (mean {within.Mean:F3})");
    }

    [Fact]
    public void Effort_proxy_survives_within_stratum_shuffle_and_dies_globally()
    {
        var spec = new StudySpec
        {
            Landscapes = 24,
            N = 120,
            Budget = 70,
            Seed = 202,
            SignalStrength = 0.0,
            EffortStrength = 0.95,
            CueFromEffort = 0.9,
            RewardDensity = 0.15,
            RandomReplicates = 8,
            EffortStrata = 5,
        };

        var raw = Study.SensoryAdvantage(spec, NullKind.None);
        var within = Study.SensoryAdvantage(spec, NullKind.PermuteRewardWithinEffort);
        var global = Study.SensoryAdvantage(spec, NullKind.PermuteReward);
        Assert.True(raw.Mean > 0.06, $"mirage should look real (mean {raw.Mean:F3})");
        Assert.True(within.Mean > 0.06, $"effort–reward is preserved inside strata (mean {within.Mean:F3})");
        Assert.True(Math.Abs(global.Mean) < 0.05, $"global shuffle must kill it (mean {global.Mean:F3})");
    }

    [Fact]
    public void Within_effort_permute_preserves_effort_reward_link_better_than_global()
    {
        var sumGlobal = 0.0;
        var sumWithin = 0.0;
        for (ulong k = 0; k < 20; k++)
        {
            var world = Landscape.Generate(
                120,
                signalStrength: 0.2,
                rewardDensity: 0.15,
                seed: 11,
                landscape: k,
                effortStrength: 0.9,
                cueFromEffort: 0.0);
            var gRng = Pcg32.Stream(11, k, RngPurpose.Permute);
            var wRng = Pcg32.Stream(11, k, RngPurpose.Permute);
            var global = world.PermuteReward(ref gRng);
            var within = world.PermuteRewardWithinEffort(ref wRng, strata: 5);
            var effort = world.Effort.ToArray();
            sumGlobal += Math.Abs(Pearson(effort, global.CopyRewards()));
            sumWithin += Math.Abs(Pearson(effort, within.CopyRewards()));
        }

        Assert.True(sumWithin > sumGlobal, $"within {sumWithin / 20:F3} vs global {sumGlobal / 20:F3}");
    }

    private static double Pearson(double[] x, double[] y)
    {
        var mx = x.Average();
        var my = y.Average();
        var num = 0.0;
        var dx = 0.0;
        var dy = 0.0;
        for (var i = 0; i < x.Length; i++)
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
