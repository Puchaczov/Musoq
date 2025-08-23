using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Musoq.Benchmarks.Performance;

/// <summary>
/// Performance regression testing framework that validates performance doesn't degrade
/// beyond acceptable thresholds. This addresses the baseline regression testing
/// requirement in Phase 1 of the performance optimization roadmap.
/// </summary>
public class PerformanceRegressionTester
{
    private readonly string _baselineFile;
    private readonly PerformanceThresholds _thresholds;

    public PerformanceRegressionTester(string baselineFile = "performance-data/performance-baseline.json")
    {
        _baselineFile = baselineFile;
        _thresholds = new PerformanceThresholds();
        Directory.CreateDirectory(Path.GetDirectoryName(_baselineFile)!);
    }

    /// <summary>
    /// Establishes a new performance baseline from current metrics
    /// </summary>
    public async Task EstablishBaselineAsync(IEnumerable<PerformanceMetrics> metrics)
    {
        var baseline = new PerformanceBaseline
        {
            EstablishedAt = DateTime.UtcNow,
            Benchmarks = metrics.ToDictionary(m => m.Name, m => new BaselineMetrics
            {
                ExecutionTimeMs = m.ExecutionTimeMs,
                MemoryAllocatedKB = m.MemoryAllocatedKB,
                TotalAllocatedMB = m.TotalAllocatedMB,
                TotalGCCollections = m.TotalGCCollections
            })
        };

        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_baselineFile, json);
        
        Console.WriteLine($"✅ Performance baseline established with {baseline.Benchmarks.Count} benchmarks");
    }

    /// <summary>
    /// Validates current performance against the established baseline
    /// </summary>
    public async Task<RegressionTestResult> ValidatePerformanceAsync(IEnumerable<PerformanceMetrics> currentMetrics)
    {
        var baseline = await LoadBaselineAsync();
        if (baseline == null)
        {
            return new RegressionTestResult
            {
                Success = false,
                Message = "No baseline found. Please establish a baseline first.",
                Regressions = new List<PerformanceRegression>()
            };
        }

        var regressions = new List<PerformanceRegression>();
        var improvements = new List<PerformanceImprovement>();

        foreach (var current in currentMetrics)
        {
            if (!baseline.Benchmarks.TryGetValue(current.Name, out var baselineMetric))
            {
                Console.WriteLine($"⚠️  No baseline found for benchmark: {current.Name}");
                continue;
            }

            // Check execution time regression
            var timeRegressionPercent = ((current.ExecutionTimeMs - baselineMetric.ExecutionTimeMs) / baselineMetric.ExecutionTimeMs) * 100;
            if (timeRegressionPercent > _thresholds.MaxExecutionTimeRegressionPercent)
            {
                regressions.Add(new PerformanceRegression
                {
                    BenchmarkName = current.Name,
                    MetricType = "ExecutionTime",
                    BaselineValue = baselineMetric.ExecutionTimeMs,
                    CurrentValue = current.ExecutionTimeMs,
                    RegressionPercent = timeRegressionPercent,
                    Threshold = _thresholds.MaxExecutionTimeRegressionPercent
                });
            }
            else if (timeRegressionPercent < -_thresholds.ImprovementThresholdPercent)
            {
                improvements.Add(new PerformanceImprovement
                {
                    BenchmarkName = current.Name,
                    MetricType = "ExecutionTime",
                    BaselineValue = baselineMetric.ExecutionTimeMs,
                    CurrentValue = current.ExecutionTimeMs,
                    ImprovementPercent = -timeRegressionPercent
                });
            }

            // Check memory allocation regression
            var memoryRegressionPercent = ((current.MemoryAllocatedKB - baselineMetric.MemoryAllocatedKB) / Math.Max(baselineMetric.MemoryAllocatedKB, 1)) * 100;
            if (memoryRegressionPercent > _thresholds.MaxMemoryRegressionPercent)
            {
                regressions.Add(new PerformanceRegression
                {
                    BenchmarkName = current.Name,
                    MetricType = "MemoryAllocation",
                    BaselineValue = baselineMetric.MemoryAllocatedKB,
                    CurrentValue = current.MemoryAllocatedKB,
                    RegressionPercent = memoryRegressionPercent,
                    Threshold = _thresholds.MaxMemoryRegressionPercent
                });
            }

            // Check GC collection regression
            var gcRegressionPercent = ((current.TotalGCCollections - baselineMetric.TotalGCCollections) / Math.Max(baselineMetric.TotalGCCollections, 1.0)) * 100;
            if (gcRegressionPercent > _thresholds.MaxGCRegressionPercent)
            {
                regressions.Add(new PerformanceRegression
                {
                    BenchmarkName = current.Name,
                    MetricType = "GCCollections",
                    BaselineValue = baselineMetric.TotalGCCollections,
                    CurrentValue = current.TotalGCCollections,
                    RegressionPercent = gcRegressionPercent,
                    Threshold = _thresholds.MaxGCRegressionPercent
                });
            }
        }

        var result = new RegressionTestResult
        {
            Success = regressions.Count == 0,
            Message = regressions.Count == 0 
                ? $"✅ All {currentMetrics.Count()} benchmarks passed regression tests"
                : $"❌ {regressions.Count} performance regressions detected",
            Regressions = regressions,
            Improvements = improvements,
            BaselineDate = baseline.EstablishedAt
        };

        return result;
    }

    /// <summary>
    /// Prints a detailed regression test report
    /// </summary>
    public void PrintRegressionReport(RegressionTestResult result)
    {
        Console.WriteLine();
        Console.WriteLine("📊 Performance Regression Test Report");
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine($"Baseline Date: {result.BaselineDate:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Test Result: {result.Message}");
        Console.WriteLine();

        if (result.Improvements.Any())
        {
            Console.WriteLine("🚀 Performance Improvements:");
            foreach (var improvement in result.Improvements)
            {
                Console.WriteLine($"   • {improvement.BenchmarkName} ({improvement.MetricType}): {improvement.ImprovementPercent:F1}% better");
            }
            Console.WriteLine();
        }

        if (result.Regressions.Any())
        {
            Console.WriteLine("⚠️  Performance Regressions:");
            foreach (var regression in result.Regressions)
            {
                Console.WriteLine($"   • {regression.BenchmarkName} ({regression.MetricType}): {regression.RegressionPercent:F1}% worse (threshold: {regression.Threshold}%)");
                Console.WriteLine($"     Baseline: {regression.BaselineValue:F2}, Current: {regression.CurrentValue:F2}");
            }
            Console.WriteLine();
        }
    }

    private async Task<PerformanceBaseline?> LoadBaselineAsync()
    {
        if (!File.Exists(_baselineFile))
            return null;

        try
        {
            var json = await File.ReadAllTextAsync(_baselineFile);
            return JsonSerializer.Deserialize<PerformanceBaseline>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Configuration for performance regression thresholds
/// </summary>
public class PerformanceThresholds
{
    /// <summary>
    /// Maximum allowed execution time regression percentage before failing (default: 20%)
    /// </summary>
    public double MaxExecutionTimeRegressionPercent { get; set; } = 20.0;

    /// <summary>
    /// Maximum allowed memory allocation regression percentage before failing (default: 30%)
    /// </summary>
    public double MaxMemoryRegressionPercent { get; set; } = 30.0;

    /// <summary>
    /// Maximum allowed GC collection regression percentage before failing (default: 50%)
    /// </summary>
    public double MaxGCRegressionPercent { get; set; } = 50.0;

    /// <summary>
    /// Minimum improvement percentage to report as significant (default: 10%)
    /// </summary>
    public double ImprovementThresholdPercent { get; set; } = 10.0;
}

public class PerformanceBaseline
{
    public DateTime EstablishedAt { get; set; }
    public Dictionary<string, BaselineMetrics> Benchmarks { get; set; } = new();
}

public class BaselineMetrics
{
    public double ExecutionTimeMs { get; set; }
    public double MemoryAllocatedKB { get; set; }
    public double TotalAllocatedMB { get; set; }
    public int TotalGCCollections { get; set; }
}

public class RegressionTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<PerformanceRegression> Regressions { get; set; } = new();
    public List<PerformanceImprovement> Improvements { get; set; } = new();
    public DateTime BaselineDate { get; set; }
}

public class PerformanceRegression
{
    public string BenchmarkName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double BaselineValue { get; set; }
    public double CurrentValue { get; set; }
    public double RegressionPercent { get; set; }
    public double Threshold { get; set; }
}

public class PerformanceImprovement
{
    public string BenchmarkName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public double BaselineValue { get; set; }
    public double CurrentValue { get; set; }
    public double ImprovementPercent { get; set; }
}