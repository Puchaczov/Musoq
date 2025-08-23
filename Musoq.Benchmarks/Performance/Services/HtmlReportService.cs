using System.Text;
using Musoq.Benchmarks.Performance.Models;

namespace Musoq.Benchmarks.Performance.Services;

public class HtmlReportService
{
    public async Task GenerateReportAsync(PerformanceHistory history, string outputDirectory)
    {
        var html = GenerateHtmlReport(history);
        var reportPath = Path.Combine(outputDirectory, "performance-report.html");
        await File.WriteAllTextAsync(reportPath, html);
    }

    private string GenerateHtmlReport(PerformanceHistory history)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Musoq Performance Report</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #2c3e50;
            text-align: center;
            margin-bottom: 30px;
        }
        h2 {
            color: #34495e;
            border-bottom: 2px solid #3498db;
            padding-bottom: 10px;
        }
        .summary-card {
            background: #ecf0f1;
            padding: 20px;
            margin: 20px 0;
            border-radius: 6px;
            border-left: 4px solid #3498db;
        }
        .benchmark-section {
            margin: 30px 0;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 6px;
        }
        .performance-table {
            width: 100%;
            border-collapse: collapse;
            margin: 15px 0;
        }
        .performance-table th,
        .performance-table td {
            border: 1px solid #ddd;
            padding: 12px;
            text-align: left;
        }
        .performance-table th {
            background-color: #f8f9fa;
            font-weight: 600;
        }
        .improvement {
            color: #27ae60;
            font-weight: bold;
        }
        .regression {
            color: #e74c3c;
            font-weight: bold;
        }
        .chart-container {
            text-align: center;
            margin: 20px 0;
        }
        .chart-container img {
            max-width: 100%;
            height: auto;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        .metadata {
            background: #f8f9fa;
            padding: 15px;
            border-radius: 4px;
            font-size: 0.9em;
            color: #666;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>🚀 Musoq Performance Report</h1>
""");

        // Summary section
        sb.AppendLine($"""
        <div class="summary-card">
            <h2>📊 Summary</h2>
            <p><strong>Report Generated:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p><strong>Total Benchmarks:</strong> {history.Benchmarks.Count}</p>
            <p><strong>Last Updated:</strong> {history.LastUpdated:yyyy-MM-dd HH:mm:ss} UTC</p>
        </div>
""");

        // Summary chart
        if (File.Exists("charts/performance_summary.png"))
        {
            sb.AppendLine("""
        <div class="chart-container">
            <h2>📈 Performance Overview</h2>
            <img src="performance_summary.png" alt="Performance Summary Chart" />
        </div>
""");
        }

        // Individual benchmark sections
        foreach (var benchmark in history.Benchmarks.OrderBy(b => b.Key))
        {
            GenerateBenchmarkSection(sb, benchmark.Key, benchmark.Value);
        }

        sb.AppendLine("""
    </div>
</body>
</html>
""");

        return sb.ToString();
    }

    private void GenerateBenchmarkSection(StringBuilder sb, string benchmarkName, List<BenchmarkResult> results)
    {
        if (results.Count == 0) return;

        var latest = results.OrderByDescending(r => r.Timestamp).FirstOrDefault();
        var previous = results.OrderByDescending(r => r.Timestamp).Skip(1).FirstOrDefault();

        sb.AppendLine($"""
        <div class="benchmark-section">
            <h2>🎯 {benchmarkName}</h2>
""");

        if (latest != null)
        {
            sb.AppendLine($"""
            <table class="performance-table">
                <thead>
                    <tr>
                        <th>Metric</th>
                        <th>Current</th>
                        <th>Previous</th>
                        <th>Change</th>
                    </tr>
                </thead>
                <tbody>
""");

            var changePercent = previous != null && previous.Mean > 0 
                ? ((latest.Mean - previous.Mean) / previous.Mean) * 100 
                : 0;

            var changeClass = changePercent < -5 ? "improvement" : changePercent > 5 ? "regression" : "";
            var changeText = previous != null 
                ? $"{changePercent:+0.1;-0.1;0}%" 
                : "N/A";

            sb.AppendLine($"""
                    <tr>
                        <td>Mean Execution Time</td>
                        <td>{latest.Mean:F2} ms</td>
                        <td>{(previous?.Mean.ToString("F2") ?? "N/A")} ms</td>
                        <td class="{changeClass}">{changeText}</td>
                    </tr>
                    <tr>
                        <td>Standard Deviation</td>
                        <td>{latest.StdDev:F2} ms</td>
                        <td>{(previous?.StdDev.ToString("F2") ?? "N/A")} ms</td>
                        <td>-</td>
                    </tr>
                    <tr>
                        <td>Error Margin</td>
                        <td>±{latest.Error:F2} ms</td>
                        <td>±{(previous?.Error.ToString("F2") ?? "N/A")} ms</td>
                        <td>-</td>
                    </tr>
""");

            sb.AppendLine("""
                </tbody>
            </table>
""");

            // Add chart if it exists
            var safeFileName = string.Join("_", benchmarkName.Split(Path.GetInvalidFileNameChars()));
            var chartPath = $"{safeFileName}_trend.png";
            if (File.Exists($"charts/{chartPath}"))
            {
                sb.AppendLine($"""
            <div class="chart-container">
                <img src="{chartPath}" alt="{benchmarkName} Trend Chart" />
            </div>
""");
            }

            // Environment info
            sb.AppendLine($"""
            <div class="metadata">
                <strong>Environment:</strong> {latest.Environment.Runtime} on {latest.Environment.Platform}<br>
                <strong>Processor:</strong> {latest.Environment.LogicalCores} logical cores<br>
                <strong>GC Mode:</strong> {latest.Environment.GcMode}<br>
                <strong>Commit:</strong> {latest.CommitHash}<br>
                <strong>Branch:</strong> {latest.Branch}
            </div>
""");
        }

        sb.AppendLine("        </div>");
    }
}