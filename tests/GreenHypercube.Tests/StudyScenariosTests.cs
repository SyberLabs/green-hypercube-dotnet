using GreenHypercube;
using Xunit;

namespace GreenHypercube.Tests;

public sealed class StudyScenariosTests
{
    [Fact]
    public void Demonstration_covers_real_signal_and_both_null_models()
    {
        var scenarios = StudyScenarios.Demonstration;

        Assert.Equal(7, scenarios.Count);
        Assert.Contains(scenarios, scenario =>
            scenario.Spec.SignalStrength == 0.85 && scenario.NullKind == NullKind.None);
        Assert.Contains(scenarios, scenario => scenario.NullKind == NullKind.PermuteReward);
        Assert.Contains(scenarios, scenario => scenario.NullKind == NullKind.PermuteRewardWithinEffort);
        Assert.All(scenarios, scenario => Assert.Equal(24, scenario.Spec.Landscapes));
    }
}
