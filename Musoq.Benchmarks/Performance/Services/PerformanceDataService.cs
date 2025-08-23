using System.Globalization;
using CsvHelper;
using Musoq.Benchmarks.Performance.Models;
using Newtonsoft.Json;

namespace Musoq.Benchmarks.Performance.Services;

public class PerformanceDataService
{
    private readonly string _dataFilePath;

    public PerformanceDataService(string dataFilePath = "performance-history.json")
    {
        _dataFilePath = dataFilePath;
    }

    public async Task<PerformanceHistory> LoadHistoryAsync()
    {
        if (!File.Exists(_dataFilePath))
        {
            return new PerformanceHistory();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath);
            return JsonConvert.DeserializeObject<PerformanceHistory>(json) ?? new PerformanceHistory();
        }
        catch
        {
            return new PerformanceHistory();
        }
    }

    public async Task SaveHistoryAsync(PerformanceHistory history)
    {
        var json = JsonConvert.SerializeObject(history, Formatting.Indented);
        await File.WriteAllTextAsync(_dataFilePath, json);
    }

    public Task<List<BenchmarkResult>> ParseBenchmarkResultsAsync(string csvFilePath)
    {
        return Task.Run(() =>
        {
            var results = new List<BenchmarkResult>();

            using var reader = new StreamReader(csvFilePath);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            while (csv.Read())
            {
                var method = csv.GetField<string>("Method") ?? string.Empty;
                var meanText = csv.GetField<string>("Mean") ?? "0";
                var errorText = csv.GetField<string>("Error") ?? "0";
                
                // Try to get StdDev field, fallback to 0 if not available
                string stdDevText = "0";
                try
                {
                    stdDevText = csv.GetField<string>("StdDev") ?? "0";
                }
                catch
                {
                    // StdDev column might not exist in failed benchmark results
                }

                // Skip results that show "NA" (failed benchmarks)
                if (meanText.Equals("NA", StringComparison.OrdinalIgnoreCase) || 
                    errorText.Equals("NA", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var result = new BenchmarkResult
                {
                    Method = method,
                    Mean = ParseTimeValue(meanText),
                    Error = ParseTimeValue(errorText),
                    StdDev = ParseTimeValue(stdDevText),
                    Timestamp = DateTime.UtcNow,
                    CommitHash = Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
                    Branch = Environment.GetEnvironmentVariable("GITHUB_REF_NAME") ?? "local",
                    Environment = GetEnvironmentInfo()
                };

                results.Add(result);
            }

            return results;
        });
    }

    private static double ParseTimeValue(string timeText)
    {
        if (string.IsNullOrEmpty(timeText))
            return 0;

        // Remove unit suffix (ms, μs, ns, s) and parse the number
        var cleanText = timeText.Replace("ms", "").Replace("μs", "").Replace("ns", "").Replace("s", "").Trim();
        
        if (double.TryParse(cleanText, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            // Convert everything to milliseconds
            if (timeText.Contains("μs"))
                return value / 1000.0;
            if (timeText.Contains("ns"))
                return value / 1_000_000.0;
            if (timeText.Contains("s") && !timeText.Contains("ms"))
                return value * 1000.0;
            
            return value; // Already in milliseconds
        }

        return 0;
    }

    private static EnvironmentInfo GetEnvironmentInfo()
    {
        return new EnvironmentInfo
        {
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ProcessorName = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown",
            LogicalCores = Environment.ProcessorCount,
            PhysicalCores = Environment.ProcessorCount / 2, // Approximation
            GcMode = System.Runtime.GCSettings.IsServerGC ? "Server" : "Workstation"
        };
    }
}