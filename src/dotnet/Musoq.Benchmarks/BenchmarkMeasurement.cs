using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Musoq.Benchmarks;

public static class BenchmarkMeasurement
{
    public static Measurement Measure(CampaignCase item, CampaignStore store, AttemptStart attempt)
    {
        var started = DateTimeOffset.UtcNow;
        var previousMetadataPath = Environment.GetEnvironmentVariable(MeasuredProcess.OutputVariable);
        Environment.SetEnvironmentVariable(MeasuredProcess.OutputVariable, store.RawDirectory(attempt));
        try
        {
            using var run = new BenchmarkRunInfo([item.Benchmark], item.Benchmark.Descriptor.Type, item.Benchmark.Config);
            var summary = BenchmarkRunner.Run(run);
            var reports = CopyReports(summary, store, attempt);
            var report = summary.Reports.SingleOrDefault();
            if (report is not { Success: true, ResultStatistics: { } statistics } || statistics.N == 0 || !double.IsFinite(statistics.Mean) || statistics.Mean <= 0)
                return new Measurement(item.Definition.Id, started, DateTimeOffset.UtcNow, "failed",
                    string.Join("\n", summary.ValidationErrors.Select(error => error.Message)) + "\nBenchmarkDotNet produced no successful samples; inspect raw reports.", RawReports: reports);
            CampaignIntegrity.ValidateProcesses(store.RawDirectory(attempt), store.Manifest, item.Definition);
            var memory = item.Definition.Phase != "compilation-cold" && report.GcStats.TotalOperations > 0;
            long? allocatedBytes = null;
            if (memory)
                allocatedBytes = report.GcStats.GetBytesAllocatedPerOperation(item.Benchmark);
            return new Measurement(item.Definition.Id, started, DateTimeOffset.UtcNow, "complete", null,
                statistics.Mean, statistics.Median, statistics.StandardDeviation,
                Finite(statistics.ConfidenceInterval.Lower), Finite(statistics.ConfidenceInterval.Upper), statistics.N,
                allocatedBytes,
                memory ? report.GcStats.Gen0Collections : null, memory ? report.GcStats.Gen1Collections : null,
                memory ? report.GcStats.Gen2Collections : null, memory ? report.GcStats.TotalOperations : null, reports);
        }
        catch (Exception exception)
        {
            var directory = store.RawDirectory(attempt);
            string[] reports = [];
            if (Directory.Exists(directory))
                reports = Directory.GetFiles(directory, "*.json").Select(path => Path.GetRelativePath(store.DirectoryPath, path).Replace('\\', '/')).ToArray();
            return new Measurement(item.Definition.Id, started, DateTimeOffset.UtcNow, "failed", exception.ToString(), RawReports: reports);
        }
        finally
        {
            Environment.SetEnvironmentVariable(MeasuredProcess.OutputVariable, previousMetadataPath);
        }
    }

    private static string[] CopyReports(Summary summary, CampaignStore store, AttemptStart attempt)
    {
        var target = store.RawDirectory(attempt);
        Directory.CreateDirectory(target);
        var results = new List<string>();
        foreach (var source in Directory.GetFiles(summary.ResultsDirectoryPath, "*.json"))
        {
            var destination = Path.Combine(target, Path.GetFileName(source));
            File.Copy(source, destination, overwrite: false);
            results.Add(Path.GetRelativePath(store.DirectoryPath, destination).Replace('\\', '/'));
        }
        results.AddRange(Directory.GetFiles(target, "measured-process-*.json")
            .Select(path => Path.GetRelativePath(store.DirectoryPath, path).Replace('\\', '/')));
        return results.Order(StringComparer.Ordinal).ToArray();
    }

    private static double? Finite(double value) => double.IsFinite(value) ? value : null;
}
