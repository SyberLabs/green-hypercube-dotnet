namespace GreenHypercube;

/// <summary>A named study condition shared by every presentation surface.</summary>
public sealed record StudyScenario(string Label, StudySpec Spec, NullKind NullKind);

/// <summary>The result of running one named study condition.</summary>
public sealed record StudyScenarioResult(string Label, EnsembleResult Result);

/// <summary>
/// The compact demonstration suite used by both the CLI and WPF application.
/// Keeping it here prevents the two front ends from silently presenting
/// different experiments.
/// </summary>
public static class StudyScenarios
{
    public static IReadOnlyList<StudyScenario> Demonstration { get; } =
    [
        new("signal=0, real assay", Spec(0.0), NullKind.None),
        new("signal=0.85, real assay", Spec(0.85), NullKind.None),
        new("signal=0.85, global shuffle", Spec(0.85), NullKind.PermuteReward),
        new(
            "signal=0.85, within-effort (effort independent)",
            Spec(0.85, effort: 0.0),
            NullKind.PermuteRewardWithinEffort),
        new("mirage (cue=effort), real assay", Spec(0.0, effort: 0.95, cueFromEffort: 0.9), NullKind.None),
        new(
            "mirage, within-effort shuffle",
            Spec(0.0, effort: 0.95, cueFromEffort: 0.9),
            NullKind.PermuteRewardWithinEffort),
        new("mirage, global shuffle", Spec(0.0, effort: 0.95, cueFromEffort: 0.9), NullKind.PermuteReward),
    ];

    public static IEnumerable<StudyScenarioResult> RunDemonstration(
        IProgress<StudyProgress>? progress = null)
    {
        var total = Demonstration.Sum(scenario => scenario.Spec.Landscapes);
        var offset = 0;

        foreach (var scenario in Demonstration)
        {
            var slice = progress is null
                ? null
                : new OffsetProgress(progress, offset, total);
            var result = Study.SensoryAdvantage(scenario.Spec, scenario.NullKind, slice);
            offset += scenario.Spec.Landscapes;
            yield return new StudyScenarioResult(scenario.Label, result);
        }
    }

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

    private sealed class OffsetProgress(
        IProgress<StudyProgress> inner,
        int offset,
        int total) : IProgress<StudyProgress>
    {
        public void Report(StudyProgress value)
        {
            inner.Report(new StudyProgress(offset + value.Completed, total));
        }
    }
}
