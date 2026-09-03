# Green Hypercube (.NET)

The study loop from the Python package, with a type-system vault Python cannot express.

Python original: `D:\syberlabs\green_hypercube`  
Picture: `D:\syberlabs\green_hypercube\docs\visual\green_hypercube.html`

## What this is

- **Cues vs assay.** Searchers receive `ICueView` only. The hidden assay lives in a vault. The only legal read is `SearchEnvironment.Experiment`, which spends budget.
- **Named PCG streams.** Landscape `k` is the same sequential or in `Parallel.For`.
- **Ensembles.** Mean AUDC advantage plus a Wald 95% interval over independent landscapes.
- **Effort null.** Shuffle assay labels inside effort strata. A cue that only tracks study effort must die.

## What this is not

- Not NAEB/GBIF/ChEMBL, phylogeny, or the HTML essay.
- Not Carterra software. Not affiliated.

## Run

```powershell
cd D:\syberlabs\green-hypercube-dotnet
dotnet test
dotnet run --project src/GreenHypercube.Cli
dotnet run --project src/GreenHypercube.Desk
```

Native AOT needs the Visual Studio C++ workload (`link.exe`). This machine does not have it. A self-contained single file still runs without Python:

```powershell
dotnet publish src/GreenHypercube.Cli -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o artifacts/cli
.\artifacts\cli\GreenHypercube.Cli.exe
```

When the C++ linker is installed:

```powershell
dotnet publish src/GreenHypercube.Cli -c Release -r win-x64 -p:PublishAot=true -o artifacts/cli
```

## Interview trunk

1. The searcher cannot compile a peek at the assay.
2. Advantage is an interval over landscapes, not one lucky curve.
3. Within-effort shuffle keeps effort–reward. If the cue is just study effort, the win remains; if it is not effort, the win dies. Global shuffle kills both.
