# Musoq.Benchmarks

Follow the repository-wide guide first. This non-packable .NET 10 project contains
one BenchmarkDotNet execution case while the benchmark suite awaits redesign.

## Structure and behavior

- `Program.cs` forwards command-line arguments to BenchmarkDotNet in all configurations.
- `SimpleQueryExecutionBenchmark.cs` compiles an inline three-row query in global
  setup and verifies the exact results 1, 2, 3 outside measurement.
- Its single benchmark method executes the compiled query, consumes the rows,
  disposes the result table, and returns checksum 6. Global cleanup disposes the query.
- Keep the project self-contained: no external files, test helpers, datasource
  fixtures, parameter matrices, qualification gates, or custom report commands.
- The project references BenchmarkDotNet and Musoq.Converter. It remains in the
  solution and CI build, but is not part of the NuGet release train.

## Validation and measurement

Use the commands in [README.md](README.md) to build, list the single case, run a
Dry smoke check, or select a short measurement job. Keep MemoryDiagnoser enabled.
Dry runs validate execution only; do not interpret them as performance evidence.
Do not run builds or tests concurrently with measurements. Keep raw reports in
ignored BenchmarkDotNet artifacts.

Choose wider validation by actual change impact, following the repository policy.
Engine performance changes still require correctness regressions and controlled,
equivalent measurements. Design the future benchmark suite separately.
