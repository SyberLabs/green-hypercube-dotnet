# Green Hypercube for .NET

[![CI](https://github.com/SyberLabs/green-hypercube-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/SyberLabs/green-hypercube-dotnet/actions/workflows/ci.yml)

An independent .NET 8 implementation of a leakage-resistant adaptive-search study. It demonstrates how C# types, deterministic random streams, parallel execution, tests, a command-line interface, and a Windows desktop interface can make a scientific computation difficult to misuse.

The original study is maintained in the [Green Hypercube Python project](https://github.com/SyberLabs/papers/tree/main/green-hypercube). This repository is an independent portfolio implementation. It is not Carterra software and is not affiliated with Carterra.

## The engineering problem

An adaptive search strategy should see useful cues, but it must not inspect the hidden assay it is trying to predict. A careless API can leak the answer and produce a convincing but meaningless result.

This implementation makes that boundary explicit:

- Search strategies receive `ICueView`, which exposes sensory salience and nothing from the assay.
- Only `SearchEnvironment.Experiment` can reveal an assay value, and every reveal spends experimental budget.
- Named PCG streams make landscape `k` identical whether ensembles run sequentially or through `Parallel.For`.
- Global and within-effort permutation nulls distinguish genuine cue signal from a study-effort proxy.
- The CLI and WPF application run the same scenarios from one core catalog.

```text
CLI / WPF
    |
    v
StudyScenarios --> Study --> Landscape + permutation nulls
                                  |
                    ICueView --> SearchEnvironment --> AssayVault
                    visible         budgeted             hidden
```

## Run it

Requirements: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and Windows for the WPF application.

```powershell
git clone https://github.com/SyberLabs/green-hypercube-dotnet.git
cd green-hypercube-dotnet

dotnet test
dotnet run --project src/GreenHypercube.Cli
dotnet run --project src/GreenHypercube.Desk
```

The CLI prints the mean area-under-discovery-curve advantage of sensory search over random search, with a 95% interval across independent landscapes. The expected pattern is more important than any single number:

1. A real cue wins against the unshuffled assay.
2. Shuffling assay labels destroys that advantage.
3. Within-effort shuffling preserves an effort proxy but destroys a cue independent of effort.
4. Global shuffling destroys both.

## Solution structure

- `src/GreenHypercube` — domain model, deterministic random streams, search engine, null models, and study scenarios.
- `src/GreenHypercube.Cli` — smallest reproducible demonstration.
- `src/GreenHypercube.Desk` — WPF demonstration with asynchronous execution and progress reporting.
- `tests/GreenHypercube.Tests` — deterministic-stream, assay-boundary, null-model, and study-invariant tests.

## Design decisions worth reviewing

### Capability-shaped API

`SensorySearch` can only be constructed from `ICueView`. The assay remains an internal type, so normal strategy code cannot compile a direct peek at the answer. `VaultSurfaceTests` verifies the public surface with reflection.

### Deterministic parallelism

`Pcg32.Stream(seed, landscape, purpose)` gives generation, permutation, sensory search, and each random baseline independent named streams. Work scheduling therefore cannot change a landscape's random sequence.

### One experiment catalog

`StudyScenarios.Demonstration` owns the seven conditions shown by both front ends. Presentation code formats results; it does not redefine the science.

### Nulls that test the actual confounder

A global label shuffle destroys every reward relationship. A within-effort shuffle preserves the effort-reward link, revealing when a sensory cue is merely acting as an effort proxy.

## Delivery

CI builds the complete solution on Windows and runs the xUnit suite with nullable reference types and warnings treated as errors.

A self-contained Windows executable can be produced without Python:

```powershell
dotnet publish src/GreenHypercube.Cli `
  --configuration Release `
  --runtime win-x64 `
  --self-contained `
  -p:PublishSingleFile=true `
  --output artifacts/cli
```

Native AOT is also supported by the core library when the Visual Studio C++ workload is installed:

```powershell
dotnet publish src/GreenHypercube.Cli `
  --configuration Release `
  --runtime win-x64 `
  -p:PublishAot=true `
  --output artifacts/cli
```

## Five-minute interview walkthrough

1. Open `World.cs` and show that `ICueView` contains no assay or effort data.
2. Open `Search.cs` and show that experiments are budgeted through one method.
3. Open `Pcg32.cs` and explain why named streams make parallel runs reproducible.
4. Run the CLI and explain the real-signal, effort-proxy, and shuffled-null rows.
5. Run the tests and show that the statistical and API-boundary claims are executable invariants.
