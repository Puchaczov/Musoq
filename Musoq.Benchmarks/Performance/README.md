# Musoq Performance Tracking Framework

This framework provides comprehensive performance monitoring for Musoq query execution, including historical tracking, trend analysis, and automated reporting.

## Features

- 📊 **Historical Performance Tracking**: Stores benchmark results over time with metadata
- 📈 **Trend Analysis**: Compares current performance with previous runs
- 🎯 **Performance Regression Detection**: Automatically identifies significant performance changes
- 📄 **HTML Reports**: Generates detailed performance reports with charts
- 🔄 **CI/CD Integration**: Automatic performance tracking on master branch merges
- 🚀 **Manual Execution**: On-demand performance benchmarking

## Quick Start

### Running Performance Benchmarks

```bash
# Standard benchmarks with performance tracking
cd Musoq.Benchmarks
dotnet run --configuration Release -- --track-performance

# Extended benchmarks with performance tracking
dotnet run --configuration Release -- --track-performance --extended

# Normal benchmarks (no tracking)
dotnet run --configuration Release
```

### Performance Data Storage

Performance data is stored in `performance-data/performance-history.json` and includes:
- Execution times (mean, error, standard deviation)
- Environment information (runtime, platform, hardware)
- Git metadata (commit hash, branch)
- Timestamps for trend analysis

### Generated Reports

The framework generates:
- **HTML Report**: `performance-reports/performance-report.html`
- **Performance Charts**: Trend graphs showing performance over time
- **Comparison Data**: Current vs. previous performance metrics

## CI/CD Integration

### Automatic Execution

The performance framework runs automatically on:
- **Master branch merges**: Tracks performance changes in main branch
- **Manual dispatch**: On-demand execution via GitHub Actions

### GitHub Actions Workflow

```yaml
# Trigger performance benchmarks
name: Performance Tracking
on:
  push:
    branches: [master]
  workflow_dispatch:
```

### Artifacts

Performance results are uploaded as GitHub Actions artifacts:
- **Performance Reports**: HTML reports and charts (90-day retention)
- **Benchmark Artifacts**: Raw BenchmarkDotNet results (30-day retention)

## Performance Analysis

### Regression Detection

The framework automatically detects:
- **Performance Regressions**: >10% slower execution
- **Performance Improvements**: >5% faster execution
- **Stable Performance**: Changes within ±5%

### Console Output Example

```
📊 Performance Analysis Summary
═══════════════════════════════
✅ Performance Improvements:
   • ComputeSimpleSelect_WithParallelization: -1.2% faster
🔄 No significant performance changes detected
📈 Total benchmarks analyzed: 2
```

## Architecture

### Core Components

1. **PerformanceTracker**: Main orchestrator for performance tracking
2. **PerformanceDataService**: Handles data persistence and CSV parsing
3. **ChartGenerationService**: Creates performance trend charts using OxyPlot
4. **HtmlReportService**: Generates comprehensive HTML reports

### Data Models

```csharp
public class BenchmarkResult
{
    public string Method { get; set; }
    public double Mean { get; set; }
    public double Error { get; set; }
    public double StdDev { get; set; }
    public DateTime Timestamp { get; set; }
    public string CommitHash { get; set; }
    public string Branch { get; set; }
    public EnvironmentInfo Environment { get; set; }
}
```

### Storage Format

Performance history is stored as JSON with automatic data retention (last 100 results per benchmark):

```json
{
  "lastUpdated": "2025-08-23T16:45:06.0423371Z",
  "benchmarks": {
    "BenchmarkMethodName": [
      {
        "method": "BenchmarkMethodName",
        "mean": 33.24,
        "error": 0.63,
        "stdDev": 1.382,
        "timestamp": "2025-08-23T16:45:06.0396459Z",
        "commitHash": "abc123...",
        "branch": "master",
        "environment": { ... }
      }
    ]
  }
}
```

## Configuration

### Environment Variables

- `GITHUB_SHA`: Git commit hash (auto-set in CI)
- `GITHUB_REF_NAME`: Git branch name (auto-set in CI)

### Customization

The framework can be extended by:
- Adding new benchmark scenarios to `ExtendedExecutionBenchmark`
- Customizing chart generation in `ChartGenerationService`
- Modifying HTML report templates in `HtmlReportService`

## Usage Examples

### Manual Analysis

```csharp
var tracker = new PerformanceTracker();
var analysis = await tracker.AnalyzePerformanceAsync();

foreach (var regression in analysis.Regressions)
{
    Console.WriteLine($"⚠️ {regression.Name}: {regression.ChangePercent:F1}% slower");
}
```

### Custom Report Generation

```csharp
var dataService = new PerformanceDataService();
var history = await dataService.LoadHistoryAsync();

var reportService = new HtmlReportService();
await reportService.GenerateReportAsync(history, "custom-reports");
```

## Dependencies

- **BenchmarkDotNet**: Performance benchmarking framework
- **OxyPlot**: Chart generation library
- **Newtonsoft.Json**: JSON serialization
- **CsvHelper**: CSV parsing for benchmark results

## Best Practices

1. **Run on consistent hardware** for reliable comparisons
2. **Monitor long-term trends** rather than single-run variations
3. **Investigate significant regressions** (>10% slowdown) immediately
4. **Use extended benchmarks** for comprehensive performance analysis
5. **Archive historical data** for long-term trend analysis

## Troubleshooting

### Common Issues

1. **Chart Generation Errors**: Install system fonts or use alternative font settings
2. **CSV Parsing Failures**: Ensure BenchmarkDotNet completes successfully
3. **Permission Errors**: Run with appropriate file system permissions

### Debug Mode

```bash
# Run in debug mode for detailed logging
cd Musoq.Benchmarks
dotnet run --configuration Debug -- --track-performance
```

## Contributing

To contribute to the performance framework:

1. Add new benchmark scenarios to test different query patterns
2. Enhance chart generation with additional metrics
3. Improve HTML report styling and content
4. Add performance regression notifications
5. Extend CI/CD integration capabilities

The framework is designed to be extensible and can be adapted for various performance monitoring needs beyond the current Musoq benchmarks.