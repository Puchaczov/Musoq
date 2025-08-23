using Musoq.Benchmarks.Performance.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.ImageSharp;
using OxyPlot.Series;

namespace Musoq.Benchmarks.Performance.Services;

public class ReadmePerformanceService
{
    public async Task GenerateReadmePerformanceSection(PerformanceHistory history, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        
        // Generate README-specific compact chart
        try
        {
            await GenerateReadmeChart(history, outputDirectory);
            Console.WriteLine("✅ Generated README performance chart");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Chart generation failed: {ex.Message}. Continuing with markdown generation...");
        }
        
        // Generate performance markdown content
        await GeneratePerformanceMarkdown(history, outputDirectory);
        Console.WriteLine("✅ Generated README performance markdown");
    }

    private async Task GenerateReadmeChart(PerformanceHistory history, string outputDirectory)
    {
        var plotModel = new PlotModel
        {
            Title = "Query Performance Benchmarks",
            Background = OxyColors.White,
            DefaultFont = "DejaVu Sans",
            DefaultFontSize = 11,
            TitleFontSize = 14
        };

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Query Type",
            LabelField = "Category",
            Angle = -45
        };

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Execution Time (ms)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColors.LightGray,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineColor = OxyColors.LightGray
        };

        plotModel.Axes.Add(categoryAxis);
        plotModel.Axes.Add(valueAxis);

        var currentSeries = new BarSeries
        {
            Title = "Current Performance",
            FillColor = OxyColors.SteelBlue,
            StrokeColor = OxyColors.DarkBlue,
            StrokeThickness = 1
        };

        var improvementSeries = new BarSeries
        {
            Title = "Performance Change",
            FillColor = OxyColors.Green,
            StrokeColor = OxyColors.DarkGreen,
            StrokeThickness = 1
        };

        foreach (var benchmark in history.Benchmarks.OrderBy(b => b.Key))
        {
            var latest = benchmark.Value.OrderByDescending(r => r.Timestamp).FirstOrDefault();
            var previous = benchmark.Value.OrderByDescending(r => r.Timestamp).Skip(1).FirstOrDefault();

            if (latest != null)
            {
                // Simplify benchmark name for chart
                var simpleName = SimplifyBenchmarkName(benchmark.Key);
                categoryAxis.Labels.Add(simpleName);
                
                currentSeries.Items.Add(new BarItem(latest.Mean));

                // Add improvement indicator
                if (previous != null)
                {
                    var changePercent = ((latest.Mean - previous.Mean) / previous.Mean) * 100;
                    var changeValue = changePercent < 0 ? Math.Abs(changePercent) : 0; // Only show improvements
                    improvementSeries.Items.Add(new BarItem(changeValue));
                }
                else
                {
                    improvementSeries.Items.Add(new BarItem(0));
                }
            }
        }

        plotModel.Series.Add(currentSeries);
        if (improvementSeries.Items.Any(i => i.Value > 0))
        {
            plotModel.Series.Add(improvementSeries);
        }

        var fileName = Path.Combine(outputDirectory, "readme-performance-chart.png");
        
        try
        {
            await Task.Run(() =>
            {
                var pngExporter = new PngExporter(800, 400, 96);
                using var stream = File.Create(fileName);
                pngExporter.Export(plotModel, stream);
            });
        }
        catch (Exception ex)
        {
            // If chart generation fails, create a simple text-based placeholder
            Console.WriteLine($"Chart generation failed: {ex.Message}. Creating placeholder...");
            await CreatePlaceholderChart(fileName);
        }
    }

    private async Task CreatePlaceholderChart(string fileName)
    {
        // Create a simple 1x1 transparent PNG as placeholder
        var placeholderContent = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        await File.WriteAllBytesAsync(fileName, placeholderContent);
    }

    private async Task GeneratePerformanceMarkdown(PerformanceHistory history, string outputDirectory)
    {
        var markdownContent = new List<string>();
        
        markdownContent.Add("## 📊 Performance Benchmarks");
        markdownContent.Add("");
        markdownContent.Add("Musoq query performance is continuously monitored to ensure optimal execution times across different query patterns.");
        markdownContent.Add("");
        
        // Add chart
        markdownContent.Add("![Performance Benchmarks](./Musoq.Benchmarks/performance-reports/readme-performance-chart.png)");
        markdownContent.Add("");

        // Add current performance summary
        markdownContent.Add("### Current Performance Summary");
        markdownContent.Add("");
        markdownContent.Add("| Query Type | Execution Time | Status |");
        markdownContent.Add("|------------|----------------|--------|");

        foreach (var benchmark in history.Benchmarks.OrderBy(b => b.Key))
        {
            var latest = benchmark.Value.OrderByDescending(r => r.Timestamp).FirstOrDefault();
            var previous = benchmark.Value.OrderByDescending(r => r.Timestamp).Skip(1).FirstOrDefault();

            if (latest != null)
            {
                var simpleName = SimplifyBenchmarkName(benchmark.Key);
                var status = "🔄 Stable";
                
                if (previous != null)
                {
                    var changePercent = ((latest.Mean - previous.Mean) / previous.Mean) * 100;
                    if (changePercent < -5)
                        status = "🚀 Improved";
                    else if (changePercent > 10)
                        status = "🐌 Needs attention";
                }

                markdownContent.Add($"| {simpleName} | {latest.Mean:F1}ms | {status} |");
            }
        }

        markdownContent.Add("");
        markdownContent.Add($"*Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC*");
        markdownContent.Add("");
        markdownContent.Add("### Detailed Performance Analysis");
        markdownContent.Add("");
        markdownContent.Add("For comprehensive performance analysis including:");
        markdownContent.Add("- Historical trends and performance graphs");
        markdownContent.Add("- Detailed execution statistics and error margins");
        markdownContent.Add("- Performance regression detection");
        markdownContent.Add("- Environment-specific benchmarks");
        markdownContent.Add("");
        markdownContent.Add("View the [detailed performance reports](./Musoq.Benchmarks/performance-reports/) generated by our CI/CD pipeline.");

        var fileName = Path.Combine(outputDirectory, "performance-section.md");
        await File.WriteAllTextAsync(fileName, string.Join(Environment.NewLine, markdownContent));
    }

    private string SimplifyBenchmarkName(string benchmarkName)
    {
        // Convert long benchmark names to readable format
        if (benchmarkName.Contains("WithParallelization"))
            return "Parallel Query";
        if (benchmarkName.Contains("WithoutParallelization"))
            return "Sequential Query";
        if (benchmarkName.Contains("SimpleSelect"))
            return "Simple SELECT";
        if (benchmarkName.Contains("GroupBy"))
            return "GROUP BY";
        if (benchmarkName.Contains("Join"))
            return "JOIN";
        if (benchmarkName.Contains("Aggregation"))
            return "Aggregation";
        
        // Default: take first meaningful part
        var parts = benchmarkName.Split('_');
        return parts.Length > 0 ? parts[0] : benchmarkName;
    }
}