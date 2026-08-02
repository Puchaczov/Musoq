# Musoq.Benchmarks

Performance measurement suite using BenchmarkDotNet. It covers compilation, execution, joins, aggregation, window functions, source planning, optimization passes, and more.

## Internal Structure

```
Musoq.Benchmarks/
├── Benchmark Classes (representative list below)
│   ├── CompilationPipelineBenchmark.cs      # End-to-end compilation benchmarks
│   ├── ExecutionBenchmark.cs                # Query execution benchmarks
│   ├── DistinctBenchmark.cs                 # DISTINCT performance
│   ├── JoinBenchmark.cs                     # JOIN performance
│   ├── HashJoinComplexBenchmark.cs          # Complex hash join scenarios
│   ├── AsOfJoinBenchmark.cs                 # AS OF JOIN performance
│   ├── NonEquiJoinBenchmark.cs              # Non-equijoin performance
│   ├── LowSelectivityJoinBenchmark.cs       # Low selectivity join performance
│   ├── OuterJoinUnmatchedBenchmark.cs       # Outer join with unmatched rows
│   ├── GroupByAggregationBenchmark.cs       # GROUP BY + aggregation
│   ├── WindowFunctionBenchmark.cs           # Window function performance
│   ├── WindowFrameAndQualifyBenchmark.cs    # Window frame + QUALIFY
│   ├── InClauseOptimizationBenchmark.cs     # IN clause optimization
│   ├── CommonSubexpressionEliminationBenchmark.cs  # CSE optimization impact
│   ├── ConstantFoldingBenchmark.cs          # Constant folding impact
│   ├── OptimizationsToggleBenchmark.cs      # Compare with/without optimizations
│   ├── SourcePlanningV1Benchmark.cs         # Source-local ORDER/SKIP/TAKE planning impact
│   ├── OptimizationToggleIsolationBenchmark.cs # One engine optimization toggle per scenario
│   ├── ConversionBenchmark.cs               # Type conversion performance
│   ├── InterpretationBenchmark.cs           # binary/text interpretation
│   ├── CteDependencyGraphBenchmark.cs       # CTE dependency resolution
│   ├── LexerBenchmark.cs                    # Lexer tokenization speed
│   ├── LexerOptimizationBenchmark.cs        # Optimized lexer comparison
│   ├── RegexOptimizationBenchmark.cs        # Regex optimization impact
│   ├── RegexPluginBenchmark.cs              # Regex plugin functions
│   ├── StringOperationsBenchmark.cs         # String function performance
│   └── TableLockBenchmark.cs                # Table locking concurrency
├── Test Fixtures (per-benchmark isolation)
│   ├── TestEntity.cs, TestSchema.cs, TestSchemaProvider.cs, TestTable.cs
│   ├── TestRowSource.cs
│   ├── OptBench*.cs                         # Optimization benchmark fixtures
│   ├── OptimizationBenchmark*.cs            # Shared optimization/source-planning benchmark datasource
│   ├── CteBench*.cs                         # CTE benchmark fixtures
│   ├── CseTest*.cs                          # CSE benchmark fixtures
│   ├── BenchmarkBinary*.cs                  # Binary interpretation fixtures
│   ├── BenchmarkText*.cs                    # Text interpretation fixtures
│   ├── BenchmarkEntitySource.cs
│   ├── BenchmarkLibrary.cs, BenchmarkSchemaColumn.cs
│   ├── SchemaColumn.cs, TableTestColumn.cs
│   ├── TableTest*.cs                        # Table test fixtures
│   └── Program.cs                           # BenchmarkDotNet entry point
├── Components/                              # Shared benchmark components
├── Schema/                                  # Benchmark schema utilities
├── Helpers/                                 # Benchmark helpers
├── Exceptions/                              # Benchmark-specific exceptions
├── Data/                                    # Test data files
└── Baselines/                               # Baseline result storage
```

## Running Benchmarks

```bash
# Build once with quiet output before benchmark runs.
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet

# Available benchmark suites. Redirect long BenchmarkDotNet output to a log file and inspect BenchmarkDotNet.Artifacts/results/ afterward.
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*CompilationPipeline*" --job short --exporters json > TestResults/benchmark-compilation.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*ExecutionBenchmark*" --job short --exporters json > TestResults/benchmark-execution.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*DistinctBenchmark*" --job short --exporters json > TestResults/benchmark-distinct.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*JoinBenchmark*" --job short --memory --exporters json > TestResults/benchmark-join.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*InClause*" --job short --exporters json > TestResults/benchmark-in-clause.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*WindowFunction*" --job short --exporters json > TestResults/benchmark-window.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*GroupByAggregation*" --job short --exporters json > TestResults/benchmark-group-by.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*HashJoinComplex*" --job short --exporters json > TestResults/benchmark-hash-join.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*CSE*" --job short --exporters json > TestResults/benchmark-cse.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*ConstantFolding*" --job short --exporters json > TestResults/benchmark-constant-folding.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*SourcePlanningV1Benchmark*" --job short --exporters json > TestResults/benchmark-source-planning.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*OptimizationToggleIsolationBenchmark*" --job short --exporters json > TestResults/benchmark-optimization-toggles.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*Interpretation*" --job short --exporters json > TestResults/benchmark-interpretation.log 2>&1
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*LexerOptimization*" --job short --exporters json > TestResults/benchmark-lexer.log 2>&1

# For memory allocation analysis, add --memory flag.
# For detailed results, inspect JSON or Markdown reports under BenchmarkDotNet.Artifacts/results/ instead of pasting console logs.
```

## Interpreting Results

- Changes within **±3%** are likely measurement noise — mark as "≈ NOISE"
- Improvements beyond **-3%** are genuine — mark as "✅ FASTER"
- Regressions beyond **+3%** need justification — mark as "⚠️ SLOWER"
- **Compilation time vs execution time trade-off**: It's acceptable for compilation to be slower if execution is significantly faster (compilation happens once per query, execution happens per row/batch)
- Always consider the **absolute magnitude** — a 50% improvement on a 1μs operation matters less than a 5% improvement on a 500ms operation

## Baseline Workflow

Before implementing ANY performance optimization, identify the owning layer and establish a baseline. Optimizations to generated C# shapes should normally be represented in the physical plan or Execution IR before the renderer emits them.

```bash
# 1. Start from a clean worktree or a separate worktree for the baseline.
#    If you need to stash user changes, ask first.

# 2. Build and run benchmarks on clean code (baseline)
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*RelevantBenchmark*" --job short --exporters json > TestResults/benchmark-baseline.log 2>&1

# 3. Record baseline results

# 4. Apply the optimization in the physical plan or Execution IR owner layer

# 5. Build and run the same benchmarks (optimized)
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build -- --filter "*RelevantBenchmark*" --job short --exporters json > TestResults/benchmark-optimized.log 2>&1

# 6. Compare results and present before/after table
```

## Adding New Benchmarks

If your optimization targets a code path not covered by existing benchmarks:

1. Create a new benchmark class in the project root (e.g., `MyFeatureBenchmark.cs`)
2. Create isolated test fixtures (entity, schema, provider, row source) — each benchmark should be self-contained
3. Add an evaluator correctness test that compares optimized execution with the baseline behavior
4. Add one benchmark where only that optimization changes
5. Use `[MemoryDiagnoser]` for memory analysis
6. Use `[SimpleJob(RuntimeMoniker.Net80)]` or equivalent
7. Run with `--filter "*MyFeature*" --job short` to validate

For source-planning work, toggle the optimization through datasource behavior (`RejectAll` vs the specific accepted plan). For engine work, toggle it through `CompilationOptions`. Avoid using only broad "all optimizations on/off" benchmarks as evidence.

## Dependencies

```
Musoq.Benchmarks
├── Musoq.Converter   (InstanceCreator API)
├── Musoq.Evaluator   (compilation, runtime)
├── Musoq.Schema      (ISchema, data sources)
├── Musoq.Plugins     (built-in functions)
└── BenchmarkDotNet   (benchmarking framework)
```
