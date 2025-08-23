using System.Globalization;
using System.Text.Json;
using CsvHelper;

namespace Musoq.Benchmarks.Performance;

/// <summary>
/// Simplified performance tracker that handles benchmark data parsing, storage, and README generation
/// </summary>
public class SimplePerformanceTracker
{
    private readonly string _dataFile;
    private readonly string _reportsDir;

    public SimplePerformanceTracker(string dataFile = "performance-data/performance-history.json", string reportsDir = "performance-reports")
    {
        _dataFile = dataFile;
        _reportsDir = reportsDir;
        Directory.CreateDirectory(Path.GetDirectoryName(_dataFile)!);
        Directory.CreateDirectory(_reportsDir);
    }

    public async Task ProcessBenchmarkResultsAsync(string csvFilePath)
    {
        Console.WriteLine("🔄 Processing benchmark results...");
        
        var newResults = ParseBenchmarkResults(csvFilePath);
        var history = await LoadHistoryAsync();
        
        foreach (var result in newResults)
        {
            history.AddResult(result);
            Console.WriteLine($"✅ Added result for {result.Method}: {result.Mean:F2}ms");
        }

        await SaveHistoryAsync(history);
        await GenerateReadmePerformanceSection(history);
        
        Console.WriteLine($"✨ Performance tracking completed");
    }

    public async Task GenerateReadmeFromExistingDataAsync()
    {
        var history = await LoadHistoryAsync();
        await GenerateReadmePerformanceSection(history);
        Console.WriteLine("✅ README performance section generated from existing data");
    }

    private List<BenchmarkResult> ParseBenchmarkResults(string csvFilePath)
    {
        var results = new List<BenchmarkResult>();
        
        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        csv.Read();
        csv.ReadHeader();
        
        while (csv.Read())
        {
            var method = csv.GetField<string>("Method") ?? "";
            var meanStr = csv.GetField<string>("Mean") ?? "0";
            
            // Parse mean value (handle units like "ms", "ns", etc.)
            var mean = ParseMeanValue(meanStr);
            
            if (!string.IsNullOrEmpty(method) && mean > 0)
            {
                results.Add(new BenchmarkResult
                {
                    Method = method,
                    Mean = mean,
                    Timestamp = DateTime.UtcNow,
                    CommitHash = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
                    Branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "local"
                });
            }
        }
        
        return results;
    }

    private static double ParseMeanValue(string meanStr)
    {
        // Remove units and parse value (convert to milliseconds)
        var value = meanStr.Replace(",", "").Trim();
        
        if (value.EndsWith("ns"))
            return double.Parse(value[..^2]) / 1_000_000; // nanoseconds to ms
        if (value.EndsWith("μs") || value.EndsWith("us"))
            return double.Parse(value[..^2]) / 1_000; // microseconds to ms
        if (value.EndsWith("ms"))
            return double.Parse(value[..^2]); // already in ms
        if (value.EndsWith("s"))
            return double.Parse(value[..^1]) * 1_000; // seconds to ms
        
        // Assume ms if no unit
        return double.Parse(value);
    }

    private async Task<PerformanceHistory> LoadHistoryAsync()
    {
        if (!File.Exists(_dataFile))
            return new PerformanceHistory();

        try
        {
            var json = await File.ReadAllTextAsync(_dataFile);
            return JsonSerializer.Deserialize<PerformanceHistory>(json) ?? new PerformanceHistory();
        }
        catch
        {
            return new PerformanceHistory();
        }
    }

    private async Task SaveHistoryAsync(PerformanceHistory history)
    {
        var json = JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_dataFile, json);
    }

    private async Task GenerateReadmePerformanceSection(PerformanceHistory history)
    {
        var summary = GeneratePerformanceSummary(history);
        var markdownSection = GenerateMarkdownSection(summary);
        
        await File.WriteAllTextAsync(Path.Combine(_reportsDir, "performance-section.md"), markdownSection);
        
        // Generate simple text-based chart for README
        await GenerateSimpleChart(summary);
    }

    private PerformanceSummary GeneratePerformanceSummary(PerformanceHistory history)
    {
        var summary = new PerformanceSummary();
        
        foreach (var (benchmarkName, results) in history.Benchmarks)
        {
            if (results.Count == 0) continue;
            
            var latest = results.OrderBy(r => r.Timestamp).Last();
            var changePercent = 0.0;
            
            if (results.Count >= 2)
            {
                var previous = results.OrderBy(r => r.Timestamp).SkipLast(1).Last();
                changePercent = ((latest.Mean - previous.Mean) / previous.Mean) * 100;
            }
            
            var status = changePercent switch
            {
                < -5 => "🚀 Improved",
                > 10 => "🐌 Needs attention", 
                _ => "🔄 Stable"
            };
            
            summary.Benchmarks.Add(new BenchmarkSummary
            {
                Name = benchmarkName,
                ExecutionTime = latest.Mean,
                Status = status,
                ChangePercent = changePercent
            });
        }
        
        summary.LastUpdated = DateTime.UtcNow;
        return summary;
    }

    private string GenerateMarkdownSection(PerformanceSummary summary)
    {
        var markdown = $"""
            ## 📊 Performance Benchmarks

            Musoq query performance is continuously monitored to ensure optimal execution times across different query patterns.

            ### Current Performance Summary

            | Query Type | Execution Time | Status |
            |------------|----------------|--------|
            """;

        foreach (var benchmark in summary.Benchmarks.OrderBy(b => b.Name))
        {
            markdown += $"| {benchmark.Name} | {benchmark.ExecutionTime:F1}ms | {benchmark.Status} |\n";
        }

        markdown += $"""

            *Last updated: {summary.LastUpdated:yyyy-MM-dd HH:mm} UTC*

            ### Detailed Performance Analysis

            For comprehensive performance analysis including:
            - Historical trends and performance data
            - Detailed execution statistics
            - Performance regression detection
            - Environment-specific benchmarks

            View the [detailed performance reports](./Musoq.Benchmarks/performance-reports/) generated by our CI/CD pipeline.
            """;

        return markdown;
    }

    private async Task GenerateSimpleChart(PerformanceSummary summary)
    {
        // Generate a simple ASCII-style chart for README
        var chartContent = "Performance Trends (Text-based visualization)\n";
        chartContent += new string('=', 50) + "\n\n";
        
        foreach (var benchmark in summary.Benchmarks.OrderBy(b => b.ExecutionTime))
        {
            var bar = new string('█', Math.Max(1, (int)(benchmark.ExecutionTime / 5))); // Scale bars
            chartContent += $"{benchmark.Name,-25} {bar} {benchmark.ExecutionTime:F1}ms\n";
        }
        
        await File.WriteAllTextAsync(Path.Combine(_reportsDir, "readme-performance-chart.txt"), chartContent);
        
        // For now, create a placeholder PNG that could be replaced with actual chart generation
        var placeholderPath = Path.Combine(_reportsDir, "readme-performance-chart.png");
        if (!File.Exists(placeholderPath))
        {
            await File.WriteAllTextAsync(placeholderPath + ".placeholder", 
                "Placeholder for performance chart - actual chart generation would require additional dependencies");
        }
    }

    public async Task PrintPerformanceSummaryAsync()
    {
        var history = await LoadHistoryAsync();
        var summary = GeneratePerformanceSummary(history);
        
        Console.WriteLine();
        Console.WriteLine("📊 Performance Analysis Summary");
        Console.WriteLine("═══════════════════════════════");
        
        var improvements = summary.Benchmarks.Where(b => b.ChangePercent < -5).ToList();
        var regressions = summary.Benchmarks.Where(b => b.ChangePercent > 10).ToList();
        
        if (improvements.Any())
        {
            Console.WriteLine("✅ Performance Improvements:");
            foreach (var improvement in improvements)
                Console.WriteLine($"   • {improvement.Name}: {improvement.ChangePercent:F1}% faster");
        }

        if (regressions.Any())
        {
            Console.WriteLine("⚠️  Performance Regressions:");
            foreach (var regression in regressions)
                Console.WriteLine($"   • {regression.Name}: {regression.ChangePercent:F1}% slower");
        }

        if (!improvements.Any() && !regressions.Any())
            Console.WriteLine("🔄 No significant performance changes detected");

        Console.WriteLine($"📈 Total benchmarks analyzed: {summary.Benchmarks.Count}");
        Console.WriteLine();
    }
}

public class PerformanceHistory
{
    public Dictionary<string, List<BenchmarkResult>> Benchmarks { get; set; } = new();

    public void AddResult(BenchmarkResult result)
    {
        if (!Benchmarks.ContainsKey(result.Method))
            Benchmarks[result.Method] = new List<BenchmarkResult>();

        Benchmarks[result.Method].Add(result);
        
        // Keep only last 100 results per benchmark to prevent unlimited growth
        if (Benchmarks[result.Method].Count > 100)
        {
            Benchmarks[result.Method] = Benchmarks[result.Method]
                .OrderBy(r => r.Timestamp)
                .TakeLast(100)
                .ToList();
        }
    }
}

public class BenchmarkResult
{
    public string Method { get; set; } = string.Empty;
    public double Mean { get; set; }
    public DateTime Timestamp { get; set; }
    public string CommitHash { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
}

public class PerformanceSummary
{
    public List<BenchmarkSummary> Benchmarks { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class BenchmarkSummary
{
    public string Name { get; set; } = string.Empty;
    public double ExecutionTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public double ChangePercent { get; set; }
}