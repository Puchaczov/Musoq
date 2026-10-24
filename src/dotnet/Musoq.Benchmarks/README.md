# Musoq.Benchmarks

This project currently contains one simple query execution benchmark. The broader
benchmark suite will be redesigned later; earlier suites remain available in Git history.

`SimpleQueryExecutionBenchmark.Execute` executes a query over three inline VALUES
rows and returns their checksum, 6. Global setup compiles the query and verifies
the exact rows 1, 2, 3. Compilation and verification are outside measurement.
Each execution consumes and disposes its result table; global cleanup disposes
the compiled query. No files, external datasources, or test infrastructure are required.

```powershell
dotnet build src/dotnet/Musoq.Benchmarks -c Release --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --list flat
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*SimpleQueryExecutionBenchmark*" --job Dry
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*SimpleQueryExecutionBenchmark*" --job short --exporters json
```

The Dry job checks that the benchmark runs; it does not establish a performance
baseline. MemoryDiagnoser reports allocations. BenchmarkDotNet writes reports to
`BenchmarkDotNet.Artifacts/results/`. Run measurements without concurrent builds
or tests. The old comparison commands, qualification gates, and performance
driver scripts have been removed.
