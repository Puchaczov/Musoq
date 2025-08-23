using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Running;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Performance;

var commandArgs = Environment.GetCommandLineArgs();
var isPerformanceTrackingMode = commandArgs.Contains("--track-performance");
var isExtendedBenchmarks = commandArgs.Contains("--extended");

if (isPerformanceTrackingMode)
{
    Console.WriteLine("🚀 Running benchmarks with performance tracking...");
    
    // Use default config which includes CSV export
    var config = DefaultConfig.Instance;

    if (isExtendedBenchmarks)
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
    if (isExtendedBenchmarks)
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
    if (isExtendedBenchmarks)
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
        var tracker = new PerformanceTracker();
        
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