using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Performance;

var commandArgs = Environment.GetCommandLineArgs();
var isPerformanceTrackingMode = commandArgs.Contains("--track-performance");
var isExtendedBenchmarks = commandArgs.Contains("--extended");
var isCompilationBenchmarks = commandArgs.Contains("--compilation");
var isPerformanceAnalysis = commandArgs.Contains("--performance-analysis");
var isAssemblyCachingBenchmarks = commandArgs.Contains("--assembly-caching");
var isSchemaProviderBenchmarks = commandArgs.Contains("--schema-provider");
var isReadmeGenMode = commandArgs.Contains("--readme-gen");

if (isReadmeGenMode)
{
    Console.WriteLine("📝 Generating README performance section from existing data...");
    try
    {
        var tracker = new SimplePerformanceTracker();
        await tracker.GenerateReadmeFromExistingDataAsync();
        Console.WriteLine("✅ README components generated successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error: {ex.Message}");
    }
    return;
}

if (isPerformanceTrackingMode)
{
    Console.WriteLine("🚀 Running benchmarks with performance tracking...");
    
    // Use default config which includes CSV export
    var config = DefaultConfig.Instance;

    if (isCompilationBenchmarks)
    {
        var summary = BenchmarkRunner.Run<CompilationBenchmark>(config);
        await ProcessPerformanceResults(summary);
    }
    else if (isPerformanceAnalysis)
    {
        var summary = BenchmarkRunner.Run<PerformanceAnalysisBenchmark>(config);
        await ProcessPerformanceResults(summary);
    }
    else if (isAssemblyCachingBenchmarks)
    {
        var summary = BenchmarkRunner.Run<AssemblyCachingBenchmark>(config);
        await ProcessPerformanceResults(summary);
    }
    else if (isSchemaProviderBenchmarks)
    {
        var summary = BenchmarkRunner.Run<SchemaProviderOptimizationBenchmark>(config);
        await ProcessPerformanceResults(summary);
        
        // Run additional performance tracking for schema provider optimization
        Console.WriteLine("\n🔍 Running detailed schema provider optimization analysis...");
        var benchmark = new SchemaProviderOptimizationBenchmark();
        benchmark.Setup();
        benchmark.SimpleQuery_WithPerformanceTracking();
        benchmark.ComplexMethodQuery_WithPerformanceTracking();
    }
    else if (isExtendedBenchmarks)
    {
        var summary = BenchmarkRunner.Run<ExtendedExecutionBenchmark>(config);
        await ProcessPerformanceResults(summary);
    }
    else
    {
        var summary = BenchmarkRunner.Run<ExecutionBenchmark>(config);
        await ProcessPerformanceResults(summary);
    }
}
else
{
    // Run benchmarks normally
#if DEBUG
    if (isCompilationBenchmarks)
    {
        BenchmarkRunner.Run<CompilationBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(CompilationBenchmark.CompileSimpleQuery_Profiles)))
            )
        );
    }
    else if (isPerformanceAnalysis)
    {
        BenchmarkRunner.Run<PerformanceAnalysisBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(PerformanceAnalysisBenchmark.SimpleSelect_CurrentSchemaProvider)))
            )
        );
    }
    else if (isAssemblyCachingBenchmarks)
    {
        BenchmarkRunner.Run<AssemblyCachingBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(AssemblyCachingBenchmark.SimpleQuery_WithCache_CacheHit)))
            )
        );
    }
    else if (isSchemaProviderBenchmarks)
    {
        BenchmarkRunner.Run<SchemaProviderOptimizationBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(SchemaProviderOptimizationBenchmark.SimpleQuery_WithOptimization)))
            )
        );
    }
    else if (isExtendedBenchmarks)
    {
        BenchmarkRunner.Run<ExtendedExecutionBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(ExtendedExecutionBenchmark.SimpleSelect_Profiles)))
            )
        );
    }
    else
    {
        BenchmarkRunner.Run<ExecutionBenchmark>(
            new DebugInProcessConfig().AddFilter(
                new NameFilter(name => name.Contains(nameof(ExecutionBenchmark.ComputeSimpleSelect_WithParallelization_10MbOfData_Profiles))))
        );
    }
#else
    if (isCompilationBenchmarks)
    {
        BenchmarkRunner.Run<CompilationBenchmark>();
    }
    else if (isPerformanceAnalysis)
    {
        BenchmarkRunner.Run<PerformanceAnalysisBenchmark>();
    }
    else if (isAssemblyCachingBenchmarks)
    {
        BenchmarkRunner.Run<AssemblyCachingBenchmark>();
    }
    else if (isSchemaProviderBenchmarks)
    {
        BenchmarkRunner.Run<SchemaProviderOptimizationBenchmark>();
    }
    else if (isExtendedBenchmarks)
    {
        BenchmarkRunner.Run<ExtendedExecutionBenchmark>();
    }
    else
    {
        BenchmarkRunner.Run<ExecutionBenchmark>();
    }
#endif
}

static async Task ProcessPerformanceResults(BenchmarkDotNet.Reports.Summary summary)
{
    try
    {
        var tracker = new SimplePerformanceTracker();
        
        // Find the CSV report file
        var resultsDir = Path.Combine(Directory.GetCurrentDirectory(), "BenchmarkDotNet.Artifacts", "results");
        var csvFiles = Directory.GetFiles(resultsDir, "*-report.csv", SearchOption.AllDirectories);
        
        if (csvFiles.Length > 0)
        {
            var latestCsv = csvFiles.OrderByDescending(File.GetCreationTime).First();
            Console.WriteLine($"📊 Processing results from: {latestCsv}");
            
            await tracker.ProcessBenchmarkResultsAsync(latestCsv);
            await tracker.PrintPerformanceSummaryAsync();
        }
        else
        {
            Console.WriteLine("⚠️ No CSV benchmark results found!");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error processing performance results: {ex.Message}");
    }
}