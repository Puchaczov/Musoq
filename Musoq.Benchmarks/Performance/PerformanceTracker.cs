using Musoq.Benchmarks.Performance.Models;
using Musoq.Benchmarks.Performance.Services;

namespace Musoq.Benchmarks.Performance;

public class PerformanceTracker
{
    private readonly PerformanceDataService _dataService;
    private readonly ChartGenerationService _chartService;
    private readonly HtmlReportService _reportService;
    private readonly ReadmePerformanceService _readmeService;

    public PerformanceTracker(string dataDirectory = "performance-data")
    {
        Directory.CreateDirectory(dataDirectory);
        
        _dataService = new PerformanceDataService(Path.Combine(dataDirectory, "performance-history.json"));
        _chartService = new ChartGenerationService();
        _reportService = new HtmlReportService();
        _readmeService = new ReadmePerformanceService();
    }

    public async Task ProcessBenchmarkResultsAsync(string csvFilePath, string outputDirectory = "performance-reports")
    {
        Console.WriteLine("🔄 Processing benchmark results...");
        
        // Parse benchmark results from CSV
        var newResults = await _dataService.ParseBenchmarkResultsAsync(csvFilePath);
        Console.WriteLine($"📊 Parsed {newResults.Count} benchmark results");

        // Load existing history
        var history = await _dataService.LoadHistoryAsync();
        Console.WriteLine($"📚 Loaded {history.Benchmarks.Count} existing benchmark histories");

        // Add new results to history
        foreach (var result in newResults)
        {
            history.AddResult(result);
            Console.WriteLine($"✅ Added result for {result.Method}: {result.Mean:F2}ms");
        }

        // Save updated history
        await _dataService.SaveHistoryAsync(history);
        Console.WriteLine("💾 Saved performance history");

        // Generate reports
        await GenerateReportsAsync(history, outputDirectory);
    }

    public async Task GenerateReportsAsync(PerformanceHistory history, string outputDirectory = "performance-reports")
    {
        Directory.CreateDirectory(outputDirectory);
        var chartsDirectory = Path.Combine(outputDirectory, "charts");
        Directory.CreateDirectory(chartsDirectory);

        Console.WriteLine("📈 Generating performance charts...");
        await _chartService.GeneratePerformanceChartsAsync(history, chartsDirectory);

        Console.WriteLine("📄 Generating HTML report...");
        await _reportService.GenerateReportAsync(history, outputDirectory);

        Console.WriteLine("📝 Generating README performance section...");
        await _readmeService.GenerateReadmePerformanceSection(history, outputDirectory);

        Console.WriteLine($"✨ Performance report generated in: {Path.GetFullPath(outputDirectory)}");
        
        // Copy chart images to main directory for HTML report
        if (Directory.Exists(chartsDirectory))
        {
            foreach (var chartFile in Directory.GetFiles(chartsDirectory, "*.png"))
            {
                var fileName = Path.GetFileName(chartFile);
                var destPath = Path.Combine(outputDirectory, fileName);
                File.Copy(chartFile, destPath, true);
            }
        }
    }

    public async Task<PerformanceAnalysis> AnalyzePerformanceAsync()
    {
        var history = await _dataService.LoadHistoryAsync();
        var analysis = new PerformanceAnalysis();

        foreach (var benchmark in history.Benchmarks)
        {
            var results = benchmark.Value.OrderBy(r => r.Timestamp).ToList();
            if (results.Count < 2) continue;

            var latest = results.Last();
            var previous = results[^2];

            var change = ((latest.Mean - previous.Mean) / previous.Mean) * 100;
            
            var benchmarkAnalysis = new BenchmarkAnalysis
            {
                Name = benchmark.Key,
                LatestMean = latest.Mean,
                PreviousMean = previous.Mean,
                ChangePercent = change,
                IsRegression = change > 10, // Consider >10% slower as regression
                IsImprovement = change < -5  // Consider >5% faster as improvement
            };

            analysis.Benchmarks.Add(benchmarkAnalysis);

            if (benchmarkAnalysis.IsRegression)
                analysis.Regressions.Add(benchmarkAnalysis);
            if (benchmarkAnalysis.IsImprovement)
                analysis.Improvements.Add(benchmarkAnalysis);
        }

        return analysis;
    }

    public async Task<PerformanceHistory> LoadHistoryAsync()
    {
        return await _dataService.LoadHistoryAsync();
    }

    public async Task GenerateReadmeReportsAsync(PerformanceHistory history, string outputDirectory = "performance-reports")
    {
        Directory.CreateDirectory(outputDirectory);
        
        Console.WriteLine("📝 Generating README performance section...");
        await _readmeService.GenerateReadmePerformanceSection(history, outputDirectory);
        
        Console.WriteLine($"✨ README performance section generated in: {Path.GetFullPath(outputDirectory)}");
    }

    public async Task PrintPerformanceSummaryAsync()
    {
        var analysis = await AnalyzePerformanceAsync();
        
        Console.WriteLine();
        Console.WriteLine("📊 Performance Analysis Summary");
        Console.WriteLine("═══════════════════════════════");
        
        if (analysis.Improvements.Any())
        {
            Console.WriteLine("✅ Performance Improvements:");
            foreach (var improvement in analysis.Improvements)
            {
                Console.WriteLine($"   • {improvement.Name}: {improvement.ChangePercent:F1}% faster");
            }
        }

        if (analysis.Regressions.Any())
        {
            Console.WriteLine("⚠️  Performance Regressions:");
            foreach (var regression in analysis.Regressions)
            {
                Console.WriteLine($"   • {regression.Name}: {regression.ChangePercent:F1}% slower");
            }
        }

        if (!analysis.Improvements.Any() && !analysis.Regressions.Any())
        {
            Console.WriteLine("🔄 No significant performance changes detected");
        }

        Console.WriteLine($"📈 Total benchmarks analyzed: {analysis.Benchmarks.Count}");
        Console.WriteLine();
    }
}

public class PerformanceAnalysis
{
    public List<BenchmarkAnalysis> Benchmarks { get; set; } = new();
    public List<BenchmarkAnalysis> Improvements { get; set; } = new();
    public List<BenchmarkAnalysis> Regressions { get; set; } = new();
}

public class BenchmarkAnalysis
{
    public string Name { get; set; } = string.Empty;
    public double LatestMean { get; set; }
    public double PreviousMean { get; set; }
    public double ChangePercent { get; set; }
    public bool IsRegression { get; set; }
    public bool IsImprovement { get; set; }
}