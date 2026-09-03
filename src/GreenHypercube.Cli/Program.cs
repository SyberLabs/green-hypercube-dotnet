using GreenHypercube;

const int Landscapes = 24;
const int N = 120;
const int Budget = 70;
const ulong Seed = 100;

Console.WriteLine("Green Hypercube (.NET)");
Console.WriteLine("AUDC advantage of sensory over random. Mean and Wald 95% CI over {0} landscapes.", Landscapes);
Console.WriteLine();

StudySpec Spec(double signal, double effort = 0.5, double cueFromEffort = 0) => new()
{
    Landscapes = Landscapes,
    N = N,
    Budget = Budget,
    Seed = Seed,
    SignalStrength = signal,
    EffortStrength = effort,
    CueFromEffort = cueFromEffort,
    RewardDensity = 0.15,
    RandomReplicates = 8,
};

static void Row(string label, EnsembleResult r)
{
    Console.WriteLine($"  {label,-48} {r.Mean,7:F3}   [{r.Ci95Low,6:F3}, {r.Ci95High,6:F3}]");
}

Row("signal=0, real assay", Study.SensoryAdvantage(Spec(0.0), NullKind.None));
Row("signal=0.85, real assay", Study.SensoryAdvantage(Spec(0.85), NullKind.None));
Row("signal=0.85, global shuffle", Study.SensoryAdvantage(Spec(0.85), NullKind.PermuteReward));
Row(
    "signal=0.85, effort independent, within-effort",
    Study.SensoryAdvantage(Spec(0.85, effort: 0.0), NullKind.PermuteRewardWithinEffort));
Row(
    "mirage (cue=effort), real assay",
    Study.SensoryAdvantage(Spec(0.0, effort: 0.95, cueFromEffort: 0.9), NullKind.None));
Row(
    "mirage, within-effort shuffle (effort–reward kept)",
    Study.SensoryAdvantage(Spec(0.0, effort: 0.95, cueFromEffort: 0.9), NullKind.PermuteRewardWithinEffort));
Row(
    "mirage, global shuffle",
    Study.SensoryAdvantage(Spec(0.0, effort: 0.95, cueFromEffort: 0.9), NullKind.PermuteReward));

Console.WriteLine();
Console.WriteLine("A cue with assay signal stays positive until labels are shuffled.");
Console.WriteLine("Within-effort shuffle keeps the effort–reward link: an effort-proxy still wins;");
Console.WriteLine("a cue that is not effort should collapse. Global shuffle kills both.");
