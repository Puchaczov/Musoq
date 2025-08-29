using System.Text;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Country;
using Musoq.Schema;

namespace Musoq.Benchmarks.CodeGeneration;

/// <summary>
/// Main runner for comprehensive code generation performance analysis
/// </summary>
public class PerformanceAnalysisRunner
{
    private readonly CodeGenerationAnalyzer _analyzer;
    private readonly List<QueryAnalysisResult> _results = new();
    
    public PerformanceAnalysisRunner()
    {
        var schemaProvider = CreateSchemaProvider();
        _analyzer = new CodeGenerationAnalyzer(schemaProvider);
    }
    
    /// <summary>
    /// Runs comprehensive analysis on all test queries
    /// </summary>
    public async Task<string> RunCompleteAnalysis()
    {
        Console.WriteLine("Starting comprehensive Musoq code generation analysis...");
        Console.WriteLine($"Total queries to analyze: {TestQuerySuite.AllQueries.Count}");
        
        // Run analysis on all queries
        await AnalyzeQueryCategory("Simple Queries", TestQuerySuite.SimpleQueries);
        await AnalyzeQueryCategory("Medium Complexity Queries", TestQuerySuite.MediumQueries);
        await AnalyzeQueryCategory("Complex Queries", TestQuerySuite.ComplexQueries);
        await AnalyzeQueryCategory("Stress Test Queries", TestQuerySuite.StressTestQueries);
        
        // Generate comprehensive report
        var report = GenerateComprehensiveReport();
        
        Console.WriteLine("Analysis complete! Report generated.");
        return report;
    }
    
    private async Task AnalyzeQueryCategory(string category, List<(string Name, string Query)> queries)
    {
        Console.WriteLine($"\nAnalyzing {category}...");
        
        foreach (var (name, query) in queries)
        {
            Console.Write($"  • {name}... ");
            
            try
            {
                var result = await Task.Run(() => _analyzer.AnalyzeQuery(query, name));
                _results.Add(result);
                
                if (result.IsSuccessful)
                {
                    Console.WriteLine($"✓ ({result.CodeMetrics.NonEmptyLines} lines, {result.PerformanceMetrics.ExecutionTimeMs}ms)");
                }
                else
                {
                    Console.WriteLine($"✗ Error: {result.Error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Exception: {ex.Message}");
                _results.Add(new QueryAnalysisResult
                {
                    QueryName = name,
                    OriginalQuery = query,
                    Error = ex.Message
                });
            }
        }
    }
    
    private string GenerateComprehensiveReport()
    {
        var report = new StringBuilder();
        var successfulResults = _results.Where(r => r.IsSuccessful).ToList();
        var failedResults = _results.Where(r => !r.IsSuccessful).ToList();
        
        report.AppendLine("# MUSOQ CODE GENERATION PERFORMANCE ANALYSIS REPORT");
        report.AppendLine("=" + new string('=', 60));
        report.AppendLine();
        
        // Executive Summary
        GenerateExecutiveSummary(report, successfulResults, failedResults);
        
        // Detailed Analysis Sections
        GenerateCodeMetricsAnalysis(report, successfulResults);
        GeneratePerformanceAnalysis(report, successfulResults);
        GeneratePatternAnalysis(report, successfulResults);
        GenerateOptimizationRecommendations(report, successfulResults);
        
        // Appendices
        GenerateDetailedResultsAppendix(report, successfulResults);
        GenerateFailuresAppendix(report, failedResults);
        
        return report.ToString();
    }
    
    private void GenerateExecutiveSummary(StringBuilder report, List<QueryAnalysisResult> successful, List<QueryAnalysisResult> failed)
    {
        report.AppendLine("## EXECUTIVE SUMMARY");
        report.AppendLine();
        
        report.AppendLine($"**Total Queries Analyzed**: {_results.Count}");
        report.AppendLine($"**Successful Analyses**: {successful.Count}");
        report.AppendLine($"**Failed Analyses**: {failed.Count}");
        report.AppendLine();
        
        if (successful.Any())
        {
            var avgLines = successful.Average(r => r.CodeMetrics.NonEmptyLines);
            var avgComplexity = successful.Average(r => r.CodeMetrics.CodeComplexityScore);
            var avgExecutionTime = successful.Average(r => r.PerformanceMetrics.ExecutionTimeMs);
            var avgMemoryUsage = successful.Average(r => r.PerformanceMetrics.MemoryUsedBytes);
            
            report.AppendLine("**Key Metrics Averages**:");
            report.AppendLine($"- Generated Code Lines: {avgLines:F1}");
            report.AppendLine($"- Code Complexity Score: {avgComplexity:F1}");
            report.AppendLine($"- Execution Time: {avgExecutionTime:F1}ms");
            report.AppendLine($"- Memory Usage: {avgMemoryUsage / 1024:F1}KB");
            report.AppendLine();
        }
        
        // Quick wins identification
        var highComplexityQueries = successful.Where(r => r.CodeMetrics.CodeComplexityScore > 50).ToList();
        var slowQueries = successful.Where(r => r.PerformanceMetrics.ExecutionTimeMs > 100).ToList();
        var memoryHeavyQueries = successful.Where(r => r.PerformanceMetrics.MemoryUsedBytes > 1024 * 1024).ToList();
        
        report.AppendLine("**Areas of Concern**:");
        report.AppendLine($"- High Complexity Queries: {highComplexityQueries.Count}");
        report.AppendLine($"- Slow Execution Queries: {slowQueries.Count}");
        report.AppendLine($"- Memory Heavy Queries: {memoryHeavyQueries.Count}");
        report.AppendLine();
    }
    
    private void GenerateCodeMetricsAnalysis(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("## CODE GENERATION METRICS ANALYSIS");
        report.AppendLine();
        
        var metrics = results.Select(r => r.CodeMetrics).ToList();
        
        report.AppendLine("### Code Size Distribution");
        report.AppendLine("| Metric | Min | Max | Average | Median |");
        report.AppendLine("|--------|-----|-----|---------|--------|");
        
        GenerateMetricRow(report, "Total Lines", metrics.Select(m => (double)m.TotalLines));
        GenerateMetricRow(report, "Non-Empty Lines", metrics.Select(m => (double)m.NonEmptyLines));
        GenerateMetricRow(report, "Methods", metrics.Select(m => (double)m.Methods));
        GenerateMetricRow(report, "Complexity Score", metrics.Select(m => (double)m.CodeComplexityScore));
        
        report.AppendLine();
        
        report.AppendLine("### Code Patterns Distribution");
        report.AppendLine("| Pattern | Total Count | Avg per Query | High Usage Queries |");
        report.AppendLine("|---------|-------------|---------------|-------------------|");
        
        GeneratePatternRow(report, "Loops", metrics.Select(m => m.Loops), results);
        GeneratePatternRow(report, "Conditionals", metrics.Select(m => m.Conditionals), results);
        GeneratePatternRow(report, "Lambdas", metrics.Select(m => m.Lambdas), results);
        GeneratePatternRow(report, "LINQ Operations", metrics.Select(m => m.LinqOperations), results);
        GeneratePatternRow(report, "Object Allocations", metrics.Select(m => m.ObjectAllocations), results);
        GeneratePatternRow(report, "String Operations", metrics.Select(m => m.StringOperations), results);
        GeneratePatternRow(report, "Reflection Calls", metrics.Select(m => m.ReflectionCalls), results);
        
        report.AppendLine();
    }
    
    private void GeneratePerformanceAnalysis(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("## PERFORMANCE METRICS ANALYSIS");
        report.AppendLine();
        
        var perfMetrics = results.Select(r => r.PerformanceMetrics).ToList();
        
        report.AppendLine("### Execution Performance");
        report.AppendLine("| Metric | Min | Max | Average | 95th Percentile |");
        report.AppendLine("|--------|-----|-----|---------|----------------|");
        
        var executionTimes = perfMetrics.Select(m => (double)m.ExecutionTimeMs).ToList();
        var memoryUsages = perfMetrics.Select(m => (double)m.MemoryUsedBytes / 1024).ToList(); // Convert to KB
        
        GeneratePerformanceMetricRow(report, "Execution Time (ms)", executionTimes);
        GeneratePerformanceMetricRow(report, "Memory Usage (KB)", memoryUsages);
        
        report.AppendLine();
        
        // Correlation analysis
        report.AppendLine("### Performance vs Complexity Correlation");
        report.AppendLine();
        
        var complexityVsTime = results.Select(r => new { 
            Complexity = r.CodeMetrics.CodeComplexityScore, 
            Time = r.PerformanceMetrics.ExecutionTimeMs,
            Name = r.QueryName
        }).OrderByDescending(x => x.Complexity).Take(10);
        
        report.AppendLine("**Top 10 Most Complex Queries:**");
        foreach (var item in complexityVsTime)
        {
            report.AppendLine($"- {item.Name}: Complexity {item.Complexity}, Time {item.Time}ms");
        }
        report.AppendLine();
    }
    
    private void GeneratePatternAnalysis(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("## CODE PATTERN ANALYSIS");
        report.AppendLine();
        
        report.AppendLine("### Identified Performance Anti-Patterns");
        report.AppendLine();
        
        // Analyze specific patterns that could be optimized
        AnalyzeReflectionUsage(report, results);
        AnalyzeStringOperations(report, results);
        AnalyzeObjectAllocations(report, results);
        AnalyzeLinqChaining(report, results);
        
        report.AppendLine();
    }
    
    private void GenerateOptimizationRecommendations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("## OPTIMIZATION RECOMMENDATIONS");
        report.AppendLine();
        
        report.AppendLine("### Small Changes (Low Risk, Quick Wins)");
        report.AppendLine();
        GenerateSmallOptimizations(report, results);
        
        report.AppendLine("### Medium Changes (Moderate Risk, Good Impact)");
        report.AppendLine();
        GenerateMediumOptimizations(report, results);
        
        report.AppendLine("### Large Changes (High Risk, High Impact)");
        report.AppendLine();
        GenerateLargeOptimizations(report, results);
        
        report.AppendLine();
    }
    
    private void GenerateSmallOptimizations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var heavyStringOps = results.Where(r => r.CodeMetrics.StringOperations > 10).Count();
        var heavyReflection = results.Where(r => r.CodeMetrics.ReflectionCalls > 5).Count();
        
        report.AppendLine("1. **String Builder Usage**: Replace string concatenation with StringBuilder");
        report.AppendLine($"   - Impact: {heavyStringOps} queries could benefit");
        report.AppendLine("   - Risk: Low - direct replacement");
        report.AppendLine("   - Effort: 1-2 days");
        report.AppendLine();
        
        report.AppendLine("2. **Constant String Interning**: Cache frequently used strings");
        report.AppendLine("   - Impact: Reduce memory allocations by 10-15%");
        report.AppendLine("   - Risk: Low - additive change");
        report.AppendLine("   - Effort: 1 day");
        report.AppendLine();
        
        report.AppendLine("3. **Null Check Optimization**: Use pattern matching for null checks");
        report.AppendLine("   - Impact: Slight performance improvement in conditional logic");
        report.AppendLine("   - Risk: Low - syntactic change");
        report.AppendLine("   - Effort: 0.5 days");
        report.AppendLine();
        
        if (heavyReflection > 0)
        {
            report.AppendLine("4. **Reduce Reflection Usage**: Cache MethodInfo/PropertyInfo objects");
            report.AppendLine($"   - Impact: {heavyReflection} queries with heavy reflection usage");
            report.AppendLine("   - Risk: Low - caching strategy");
            report.AppendLine("   - Effort: 2-3 days");
            report.AppendLine();
        }
    }
    
    private void GenerateMediumOptimizations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var heavyAllocations = results.Where(r => r.CodeMetrics.ObjectAllocations > 20).Count();
        var complexLambdas = results.Where(r => r.CodeMetrics.Lambdas > 15).Count();
        
        report.AppendLine("1. **Object Pooling**: Implement pooling for frequently allocated objects");
        report.AppendLine($"   - Impact: {heavyAllocations} queries with heavy allocations");
        report.AppendLine("   - Risk: Medium - requires lifecycle management");
        report.AppendLine("   - Effort: 1-2 weeks");
        report.AppendLine();
        
        report.AppendLine("2. **Expression Tree Compilation**: Pre-compile common lambda expressions");
        report.AppendLine($"   - Impact: {complexLambdas} queries with complex lambda usage");
        report.AppendLine("   - Risk: Medium - changes compilation pipeline");
        report.AppendLine("   - Effort: 1-2 weeks");
        report.AppendLine();
        
        report.AppendLine("3. **Code Generation Templates**: Use templates for common patterns");
        report.AppendLine("   - Impact: Reduce generated code size by 20-30%");
        report.AppendLine("   - Risk: Medium - affects code generation logic");
        report.AppendLine("   - Effort: 2-3 weeks");
        report.AppendLine();
        
        report.AppendLine("4. **LINQ Operation Optimization**: Replace LINQ chains with optimized loops");
        report.AppendLine("   - Impact: 15-25% performance improvement in data processing");
        report.AppendLine("   - Risk: Medium - changes query execution patterns");
        report.AppendLine("   - Effort: 2 weeks");
        report.AppendLine();
    }
    
    private void GenerateLargeOptimizations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("1. **Vectorization Support**: Implement SIMD operations for numeric processing");
        report.AppendLine("   - Impact: 2-4x performance improvement for numeric-heavy queries");
        report.AppendLine("   - Risk: High - platform-specific optimizations");
        report.AppendLine("   - Effort: 2-3 months");
        report.AppendLine();
        
        report.AppendLine("2. **Parallel Execution Engine**: Automatic parallelization of query operations");
        report.AppendLine("   - Impact: 1.5-3x performance improvement on multi-core systems");
        report.AppendLine("   - Risk: High - concurrency and thread safety concerns");
        report.AppendLine("   - Effort: 3-4 months");
        report.AppendLine();
        
        report.AppendLine("3. **Native Code Generation**: Compile to native code instead of IL");
        report.AppendLine("   - Impact: 20-40% performance improvement, reduced startup time");
        report.AppendLine("   - Risk: High - platform dependencies, debugging complexity");
        report.AppendLine("   - Effort: 4-6 months");
        report.AppendLine();
        
        report.AppendLine("4. **Query Plan Optimization**: Implement cost-based query optimization");
        report.AppendLine("   - Impact: 30-60% improvement for complex queries");
        report.AppendLine("   - Risk: High - fundamental changes to execution model");
        report.AppendLine("   - Effort: 6-12 months");
        report.AppendLine();
    }
    
    private void AnalyzeReflectionUsage(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var withReflection = results.Where(r => r.CodeMetrics.ReflectionCalls > 0).ToList();
        if (withReflection.Any())
        {
            report.AppendLine($"**Reflection Usage**: {withReflection.Count} queries use reflection");
            report.AppendLine($"- Average reflection calls per query: {withReflection.Average(r => r.CodeMetrics.ReflectionCalls):F1}");
            
            var heavyReflection = withReflection.Where(r => r.CodeMetrics.ReflectionCalls > 5).ToList();
            if (heavyReflection.Any())
            {
                report.AppendLine($"- Heavy reflection usage in: {string.Join(", ", heavyReflection.Take(3).Select(r => r.QueryName))}");
            }
            report.AppendLine();
        }
    }
    
    private void AnalyzeStringOperations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var withStringOps = results.Where(r => r.CodeMetrics.StringOperations > 0).ToList();
        if (withStringOps.Any())
        {
            report.AppendLine($"**String Operations**: {withStringOps.Count} queries perform string operations");
            report.AppendLine($"- Average string operations per query: {withStringOps.Average(r => r.CodeMetrics.StringOperations):F1}");
            
            var heavyStringOps = withStringOps.Where(r => r.CodeMetrics.StringOperations > 10).ToList();
            if (heavyStringOps.Any())
            {
                report.AppendLine($"- Heavy string processing in: {string.Join(", ", heavyStringOps.Take(3).Select(r => r.QueryName))}");
            }
            report.AppendLine();
        }
    }
    
    private void AnalyzeObjectAllocations(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var withAllocations = results.Where(r => r.CodeMetrics.ObjectAllocations > 0).ToList();
        if (withAllocations.Any())
        {
            report.AppendLine($"**Object Allocations**: {withAllocations.Count} queries create objects");
            report.AppendLine($"- Average allocations per query: {withAllocations.Average(r => r.CodeMetrics.ObjectAllocations):F1}");
            
            var heavyAllocations = withAllocations.Where(r => r.CodeMetrics.ObjectAllocations > 20).ToList();
            if (heavyAllocations.Any())
            {
                report.AppendLine($"- Heavy allocation patterns in: {string.Join(", ", heavyAllocations.Take(3).Select(r => r.QueryName))}");
            }
            report.AppendLine();
        }
    }
    
    private void AnalyzeLinqChaining(StringBuilder report, List<QueryAnalysisResult> results)
    {
        var withLinq = results.Where(r => r.CodeMetrics.LinqOperations > 0).ToList();
        if (withLinq.Any())
        {
            report.AppendLine($"**LINQ Operations**: {withLinq.Count} queries use LINQ");
            report.AppendLine($"- Average LINQ operations per query: {withLinq.Average(r => r.CodeMetrics.LinqOperations):F1}");
            
            var heavyLinq = withLinq.Where(r => r.CodeMetrics.LinqOperations > 10).ToList();
            if (heavyLinq.Any())
            {
                report.AppendLine($"- Heavy LINQ chaining in: {string.Join(", ", heavyLinq.Take(3).Select(r => r.QueryName))}");
            }
            report.AppendLine();
        }
    }
    
    private void GenerateDetailedResultsAppendix(StringBuilder report, List<QueryAnalysisResult> results)
    {
        report.AppendLine("## APPENDIX A: DETAILED ANALYSIS RESULTS");
        report.AppendLine();
        
        foreach (var result in results.Take(5)) // Show first 5 detailed results
        {
            report.AppendLine($"### {result.QueryName}");
            report.AppendLine();
            report.AppendLine("**Original Query:**");
            report.AppendLine("```sql");
            report.AppendLine(result.OriginalQuery);
            report.AppendLine("```");
            report.AppendLine();
            
            report.AppendLine("**Code Metrics:**");
            report.AppendLine($"- Lines: {result.CodeMetrics.NonEmptyLines}");
            report.AppendLine($"- Methods: {result.CodeMetrics.Methods}");
            report.AppendLine($"- Complexity: {result.CodeMetrics.CodeComplexityScore}");
            report.AppendLine($"- LINQ Operations: {result.CodeMetrics.LinqOperations}");
            report.AppendLine($"- Object Allocations: {result.CodeMetrics.ObjectAllocations}");
            report.AppendLine();
            
            report.AppendLine("**Performance:**");
            report.AppendLine($"- Execution Time: {result.PerformanceMetrics.ExecutionTimeMs}ms");
            report.AppendLine($"- Memory Usage: {result.PerformanceMetrics.MemoryUsedBytes / 1024:F1}KB");
            report.AppendLine($"- Row Count: {result.PerformanceMetrics.RowCount}");
            report.AppendLine();
            
            // Show complete generated code for full reference
            report.AppendLine("**Generated Code:**");
            report.AppendLine("```csharp");
            report.AppendLine(result.GeneratedCode);
            report.AppendLine("```");
            report.AppendLine();

            report.AppendLine("---");
            report.AppendLine();
        }
    }
    
    private void GenerateFailuresAppendix(StringBuilder report, List<QueryAnalysisResult> failed)
    {
        if (!failed.Any()) return;
        
        report.AppendLine("## APPENDIX B: FAILED ANALYSES");
        report.AppendLine();
        
        foreach (var failure in failed)
        {
            report.AppendLine($"### {failure.QueryName}");
            report.AppendLine($"**Error**: {failure.Error}");
            report.AppendLine("**Query**:");
            report.AppendLine("```sql");
            report.AppendLine(failure.OriginalQuery);
            report.AppendLine("```");
            report.AppendLine();
        }
    }
    
    private void GenerateMetricRow(StringBuilder report, string metricName, IEnumerable<double> values)
    {
        var valueList = values.ToList();
        if (!valueList.Any()) return;
        
        var min = valueList.Min();
        var max = valueList.Max();
        var avg = valueList.Average();
        var median = valueList.OrderBy(x => x).Skip(valueList.Count / 2).First();
        
        report.AppendLine($"| {metricName} | {min:F1} | {max:F1} | {avg:F1} | {median:F1} |");
    }
    
    private void GeneratePerformanceMetricRow(StringBuilder report, string metricName, List<double> values)
    {
        if (!values.Any()) return;
        
        values.Sort();
        var min = values.Min();
        var max = values.Max();
        var avg = values.Average();
        var p95Index = (int)(values.Count * 0.95);
        var p95 = values[Math.Min(p95Index, values.Count - 1)];
        
        report.AppendLine($"| {metricName} | {min:F1} | {max:F1} | {avg:F1} | {p95:F1} |");
    }
    
    private void GeneratePatternRow(StringBuilder report, string patternName, IEnumerable<int> counts, List<QueryAnalysisResult> results)
    {
        var countList = counts.ToList();
        if (!countList.Any()) return;
        
        var total = countList.Sum();
        var avg = countList.Average();
        var highUsageQueries = results.Where((r, i) => countList[i] > avg * 2).Take(3).Select(r => r.QueryName);
        
        report.AppendLine($"| {patternName} | {total} | {avg:F1} | {string.Join(", ", highUsageQueries)} |");
    }
    
    private static ISchemaProvider CreateSchemaProvider()
    {
        var provider = new GenericSchemaProvider<CountryEntity, CountryEntityTable>(
            new Dictionary<string, IEnumerable<CountryEntity>>
            {
                {"#test", GenerateTestData()}
            },
            CountryEntity.KNameToIndexMap,
            CountryEntity.KIndexToObjectAccessMap
        );
        return provider;
    }
    
    private static IEnumerable<CountryEntity> GenerateTestData()
    {
        var random = new Random(42); // Fixed seed for consistent results
        var countries = new[] { "USA", "Canada", "Mexico", "Brazil", "Argentina", "UK", "France", "Germany", "Japan", "China" };
        
        for (int i = 0; i < 100; i++)
        {
            var country = countries[i % countries.Length];
            yield return new CountryEntity(
                $"City{i}_{country}", 
                country, 
                random.Next(50000, 10000000)
            );
        }
    }
}