# Musoq Performance Tracking

This directory contains performance tracking and reporting for Musoq benchmarks.

## Files

- **`readme-performance-chart.png`** - Performance graph displayed in README.md showing last 10 measurements
- **`performance-history.json`** - Historical performance data in JSON format  
- **`performance-summary.md`** - Performance summary table for README.md

## Usage

### Update Performance Data

Run the performance update script from the repository root:

```bash
./update-performance.sh
```

This will:
1. Build the solution
2. Run execution benchmarks
3. Update performance history
4. Regenerate performance charts
5. Update summary table

### Manual Chart Generation

To manually regenerate charts with current data:

```bash
python3 /tmp/performance_generator.py
```

### Integration with CI/CD

The performance tracking can be integrated into GitHub Actions or other CI/CD pipelines to automatically update performance metrics on each build.

## Performance Metrics

The system tracks three key performance indicators:

- **Sequential Query**: Single-threaded query execution time
- **Parallel Query**: Multi-threaded query execution time  
- **Complex Parsing**: Time to parse very long SQL queries with complex CASE statements

## Data Format

Performance history is stored in JSON format with the following structure:

```json
{
  "date": "2025-08-24",
  "sequential_query_ms": 68.8,
  "parallel_query_ms": 45.1,
  "parsing_query_ms": 82.3,
  "git_commit": "current",
  "status": "Stable"
}
```

## Chart Configuration

The performance chart shows:
- Last 10 performance measurements
- Trend lines for each metric
- Latest values annotated on the chart
- Clean, professional styling suitable for README display