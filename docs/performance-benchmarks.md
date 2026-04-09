# Performance Benchmarks

`Outsourced.DataCube.Benchmarks` is the manual BenchmarkDotNet harness for serializer and OLAP operation performance work. It is intentionally not part of the test gate.

## Run the full suite

```bash
dotnet run --project ./tests/Outsourced.DataCube.Benchmarks/Outsourced.DataCube.Benchmarks.csproj -c Release
```

## Run a focused subset

```bash
dotnet run --project ./tests/Outsourced.DataCube.Benchmarks/Outsourced.DataCube.Benchmarks.csproj -c Release -- --filter "*CubeSerializerBenchmarks*"
dotnet run --project ./tests/Outsourced.DataCube.Benchmarks/Outsourced.DataCube.Benchmarks.csproj -c Release -- --filter "*CubeOperationsBenchmarks*"
```

## Artifacts

- BenchmarkDotNet artifacts are written to `artifacts/benchmarks`.
- Markdown and CSV summaries are emitted for each run so baselines can be compared between optimization batches.

## Suggested workflow

1. Run the affected benchmark(s) to capture a baseline.
2. Make one narrow change.
3. Run `dotnet test ./Outsourced.DataCube.sln -c Release`.
4. Rerun the same benchmark filter and compare throughput plus allocated bytes.
5. Keep the change only when it improves the measured path without breaking behavior.

## Public API policy

- Treat public API changes as opt-in and evidence-driven.
- If an optimization changes the public API shape, capture before/after benchmark evidence for the exact hot path that motivated it.
- Keep the change only when the measured gain is clear enough to justify the compatibility cost.
- If the gain is marginal, prefer preserving the existing public surface and keep the optimization internal.
