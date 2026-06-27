# Musoq Benchmarks

## Two-Mode Execution Baselines

`TwoModeExecutionBenchmark` protects the typed enumerable work from silent performance drift. It measures table execution, public typed execution versus equivalent LINQ, compile-from-scratch versus reusable typed queries, caller-owned artifact load/run paths, table-backed typed profiling, serial versus parallel typed final projection, typed fallback materialization for `distinct/order/skip/take`, and the typed post-operation row helper path for `distinct/order/skip/take`.

Run it with:

```powershell
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release -- --filter "*TwoModeExecutionBenchmark*" --job short --memory
```

These benchmarks are characterization only; unit tests must not assert timing thresholds.

## Streaming Chunk-Parallel Projection

`StreamingChunkParallelProjectionBenchmark` compares the old serial streaming projection path
(`ParallelizationMode.None`) with chunk-parallel streaming projection (`ParallelizationMode.Full`)
for a CPU-heavy row-local filter/project query over `RowSourceBase<T>` chunks.

Run it with:

```powershell
dotnet run -c Release --project src/dotnet/Musoq.Benchmarks/Musoq.Benchmarks.csproj --filter "*StreamingChunkParallelProjectionBenchmark*"
```

Interpret the `ChunkParallelStreamingProjection` ratio against `SerialStreamingProjection`.
For CPU-heavy rows, ratios below `0.50` indicate a 2x+ speedup; local short-run results showed
roughly `0.35-0.40` at MaxDegree 4 and `0.28-0.32` at MaxDegree 8, with allocation ratios below
the serial baseline.
