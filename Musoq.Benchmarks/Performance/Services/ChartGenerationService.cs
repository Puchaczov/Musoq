using Musoq.Benchmarks.Performance.Models;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.ImageSharp;
using OxyPlot.Series;

namespace Musoq.Benchmarks.Performance.Services;

public class ChartGenerationService
{
    public async Task GeneratePerformanceChartsAsync(PerformanceHistory history, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        foreach (var benchmark in history.Benchmarks)
        {
            await GenerateTimeSeriesChartAsync(benchmark.Key, benchmark.Value, outputDirectory);
        }

        // Generate summary chart with all benchmarks
        await GenerateSummaryChartAsync(history, outputDirectory);
    }

    private async Task GenerateTimeSeriesChartAsync(string benchmarkName, List<BenchmarkResult> results, string outputDirectory)
    {
        if (results.Count == 0) return;

        var plotModel = new PlotModel
        {
            Title = $"Performance Trend: {benchmarkName}",
            Background = OxyColors.White,
            DefaultFont = "DejaVu Sans",
            DefaultFontSize = 12
        };

        // Configure axes
        var dateAxis = new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Date",
            StringFormat = "MM/dd",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColors.LightGray,
            MinorGridlineColor = OxyColors.LightGray
        };

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Execution Time (ms)",
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColors.LightGray,
            MinorGridlineColor = OxyColors.LightGray
        };

        plotModel.Axes.Add(dateAxis);
        plotModel.Axes.Add(valueAxis);

        // Add mean line series
        var meanSeries = new LineSeries
        {
            Title = "Mean",
            Color = OxyColors.Blue,
            StrokeThickness = 2
        };

        // Add error bars
        var errorSeries = new LineSeries
        {
            Title = "Error Range",
            Color = OxyColors.LightBlue,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };

        var upperErrorSeries = new LineSeries
        {
            Color = OxyColors.LightBlue,
            StrokeThickness = 1,
            LineStyle = LineStyle.Dash
        };

        foreach (var result in results.OrderBy(r => r.Timestamp))
        {
            var dateValue = DateTimeAxis.ToDouble(result.Timestamp);
            meanSeries.Points.Add(new DataPoint(dateValue, result.Mean));
            errorSeries.Points.Add(new DataPoint(dateValue, result.Mean - result.Error));
            upperErrorSeries.Points.Add(new DataPoint(dateValue, result.Mean + result.Error));
        }

        plotModel.Series.Add(meanSeries);
        plotModel.Series.Add(errorSeries);
        plotModel.Series.Add(upperErrorSeries);

        // Generate chart
        var safeFileName = string.Join("_", benchmarkName.Split(Path.GetInvalidFileNameChars()));
        var fileName = Path.Combine(outputDirectory, $"{safeFileName}_trend.png");
        
        await Task.Run(() =>
        {
            var pngExporter = new PngExporter(800, 600, 96);
            using var stream = File.Create(fileName);
            pngExporter.Export(plotModel, stream);
        });
    }

    private async Task GenerateSummaryChartAsync(PerformanceHistory history, string outputDirectory)
    {
        var plotModel = new PlotModel
        {
            Title = "Latest Performance Summary",
            Background = OxyColors.White,
            DefaultFont = "DejaVu Sans",
            DefaultFontSize = 12
        };

        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Benchmarks"
        };

        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            Title = "Execution Time (ms)",
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = OxyColors.LightGray
        };

        plotModel.Axes.Add(categoryAxis);
        plotModel.Axes.Add(valueAxis);

        var barSeries = new BarSeries
        {
            Title = "Current Performance",
            FillColor = OxyColors.SteelBlue
        };

        var previousSeries = new BarSeries
        {
            Title = "Previous Performance", 
            FillColor = OxyColors.LightSteelBlue
        };

        var benchmarkIndex = 0;
        foreach (var benchmark in history.Benchmarks.OrderBy(b => b.Key))
        {
            categoryAxis.Labels.Add(benchmark.Key.Split('_')[0]); // Simplified name

            var latest = benchmark.Value.OrderByDescending(r => r.Timestamp).FirstOrDefault();
            var previous = benchmark.Value.OrderByDescending(r => r.Timestamp).Skip(1).FirstOrDefault();

            if (latest != null)
            {
                barSeries.Items.Add(new BarItem(latest.Mean));
            }

            if (previous != null)
            {
                previousSeries.Items.Add(new BarItem(previous.Mean));
            }

            benchmarkIndex++;
        }

        plotModel.Series.Add(previousSeries);
        plotModel.Series.Add(barSeries);

        var fileName = Path.Combine(outputDirectory, "performance_summary.png");
        
        await Task.Run(() =>
        {
            var pngExporter = new PngExporter(1000, 600, 96);
            using var stream = File.Create(fileName);
            pngExporter.Export(plotModel, stream);
        });
    }
}