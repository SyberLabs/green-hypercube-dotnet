using GreenHypercube;

Console.WriteLine("Green Hypercube (.NET)");
Console.WriteLine("AUDC advantage of sensory over random. Mean and Wald 95% CI over independent landscapes.");
Console.WriteLine();

foreach (var row in StudyScenarios.RunDemonstration())
{
    Console.WriteLine(
        $"  {row.Label,-48} {row.Result.Mean,7:F3}   [{row.Result.Ci95Low,6:F3}, {row.Result.Ci95High,6:F3}]");
}

Console.WriteLine();
Console.WriteLine("A cue with assay signal stays positive until labels are shuffled.");
Console.WriteLine("Within-effort shuffle keeps the effort–reward link: an effort-proxy still wins;");
Console.WriteLine("a cue that is not effort should collapse. Global shuffle kills both.");
