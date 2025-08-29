using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Benchmarks.CodeGeneration;
using Musoq.Benchmarks.Components;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Visitors;
using Musoq.Schema;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Comprehensive performance analysis focused on visitor patterns, code generation, and metadata handling
/// </summary>
public class ComprehensivePerformanceAnalysis
{
    private readonly ISchemaProvider _schemaProvider;
    private readonly CodeGenerationAnalyzer _analyzer;

    public ComprehensivePerformanceAnalysis()
    {
        _schemaProvider = new TestSchemaProvider();
        _analyzer = new CodeGenerationAnalyzer(_schemaProvider);
    }

    /// <summary>
    /// Main entry point for comprehensive performance analysis
    /// </summary>
    public async Task<ComprehensiveAnalysisReport> RunAnalysisAsync()
    {
        var report = new ComprehensiveAnalysisReport();
        
        Console.WriteLine("Starting Comprehensive Performance Analysis of Musoq Query Engine...");
        Console.WriteLine("============================================================================");
        
        // Phase 1: Analyze Code Generation Performance (using existing infrastructure)
        report.CodeGenerationAnalysis = await AnalyzeCodeGenerationPerformanceAsync();
        
        // Phase 2: Analyze Visitor Performance Patterns
        report.VisitorPatternAnalysis = await AnalyzeVisitorPatternsAsync();
        
        // Phase 3: Analyze Compilation Performance
        report.CompilationAnalysis = await AnalyzeCompilationPerformanceAsync();
        
        // Phase 4: Identify Performance Bottlenecks
        report.BottleneckAnalysis = AnalyzeBottlenecks(report);
        
        // Phase 5: Generate Optimization Recommendations
        report.OptimizationRecommendations = GenerateOptimizationRecommendations(report);
        
        Console.WriteLine("Comprehensive analysis completed!");
        return report;
    }

    /// <summary>
    /// Analyzes code generation performance using existing infrastructure
    /// </summary>
    private async Task<CodeGenerationPerformanceAnalysis> AnalyzeCodeGenerationPerformanceAsync()
    {
        Console.WriteLine("\n🏗️ Phase 1: Analyzing Code Generation Performance...");
        
        var analysis = new CodeGenerationPerformanceAnalysis();
        var testQueries = GetCodeGenerationTestQueries();
        
        foreach (var (query, name) in testQueries)
        {
            var result = _analyzer.AnalyzeQuery(query, name);
            if (result.IsSuccessful)
            {
                var cgResult = new CodeGenerationResult
                {
                    QueryName = name,
                    GeneratedLinesOfCode = result.CodeMetrics.NonEmptyLines,
                    CodeComplexityScore = result.CodeMetrics.CodeComplexityScore,
                    CompilationTimeMs = MeasureCompilationTime(query),
                    ObjectAllocations = result.CodeMetrics.ObjectAllocations,
                    ReflectionCalls = result.CodeMetrics.ReflectionCalls,
                    LinqOperations = result.CodeMetrics.LinqOperations,
                    GeneratedCode = result.GeneratedCode
                };
                
                analysis.Results.Add(cgResult);
            }
        }
        
        analysis.AverageGeneratedLines = analysis.Results.Any() ? analysis.Results.Average(r => r.GeneratedLinesOfCode) : 0;
        analysis.AverageComplexity = analysis.Results.Any() ? analysis.Results.Average(r => r.CodeComplexityScore) : 0;
        analysis.AverageCompilationTime = analysis.Results.Any() ? analysis.Results.Average(r => r.CompilationTimeMs) : 0;
        analysis.TotalReflectionCalls = analysis.Results.Sum(r => r.ReflectionCalls);
        
        Console.WriteLine($"   ✓ Analyzed {analysis.Results.Count} code generation patterns");
        Console.WriteLine($"   ✓ Average generated LOC: {analysis.AverageGeneratedLines:F0}");
        Console.WriteLine($"   ✓ Average compilation time: {analysis.AverageCompilationTime:F2}ms");
        Console.WriteLine($"   ✓ Total reflection calls: {analysis.TotalReflectionCalls}");
        
        return analysis;
    }

    /// <summary>
    /// Analyzes visitor performance patterns
    /// </summary>
    private async Task<VisitorPatternAnalysis> AnalyzeVisitorPatternsAsync()
    {
        Console.WriteLine("\n🔄 Phase 2: Analyzing Visitor Performance Patterns...");
        
        var analysis = new VisitorPatternAnalysis();
        var testQueries = GetVisitorTestQueries();
        
        foreach (var (query, name) in testQueries)
        {
            var result = await AnalyzeVisitorPerformanceAsync(query, name);
            analysis.Results.Add(result);
        }
        
        analysis.AverageVisitorTime = analysis.Results.Any() ? analysis.Results.Average(r => r.VisitorTimeMs) : 0;
        analysis.TotalVisitorCalls = analysis.Results.Sum(r => r.EstimatedVisitorCalls);
        
        Console.WriteLine($"   ✓ Analyzed {analysis.Results.Count} visitor patterns");
        Console.WriteLine($"   ✓ Average visitor time: {analysis.AverageVisitorTime:F2}ms");
        
        return analysis;
    }

    /// <summary>
    /// Analyzes compilation performance
    /// </summary>
    private async Task<CompilationAnalysis> AnalyzeCompilationPerformanceAsync()
    {
        Console.WriteLine("\n⚙️ Phase 3: Analyzing Compilation Performance...");
        
        var analysis = new CompilationAnalysis();
        var testQueries = GetCompilationTestQueries();
        
        foreach (var (query, name) in testQueries)
        {
            var result = await AnalyzeCompilationStagesAsync(query, name);
            analysis.Results.Add(result);
        }
        
        analysis.AverageCompilationTime = analysis.Results.Any() ? analysis.Results.Average(r => r.TotalCompilationTimeMs) : 0;
        analysis.AverageMemoryUsed = analysis.Results.Any() ? analysis.Results.Average(r => r.MemoryUsedBytes) : 0;
        
        Console.WriteLine($"   ✓ Analyzed {analysis.Results.Count} compilation patterns");
        Console.WriteLine($"   ✓ Average compilation time: {analysis.AverageCompilationTime:F2}ms");
        
        return analysis;
    }

    /// <summary>
    /// Analyzes visitor performance for a specific query
    /// </summary>
    private async Task<VisitorResult> AnalyzeVisitorPerformanceAsync(string query, string name)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Measure time taken for visitor operations
            var assemblyName = $"VisitorTest_{Guid.NewGuid().ToString("N")[..8]}";
            var buildItems = InstanceCreator.CreateForAnalyze(query, assemblyName, _schemaProvider, new BenchmarkLoggerResolver());
            
            sw.Stop();
            
            return new VisitorResult
            {
                QueryName = name,
                VisitorTimeMs = sw.ElapsedMilliseconds,
                EstimatedVisitorCalls = EstimateVisitorComplexity(query),
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new VisitorResult
            {
                QueryName = name,
                VisitorTimeMs = sw.ElapsedMilliseconds,
                Error = ex.Message,
                IsSuccessful = false
            };
        }
    }

    /// <summary>
    /// Analyzes compilation stages performance
    /// </summary>
    private async Task<CompilationResult> AnalyzeCompilationStagesAsync(string query, string name)
    {
        var totalSw = Stopwatch.StartNew();
        var memoryBefore = GC.GetTotalMemory(false);
        
        try
        {
            // Stage 1: Parse and Build
            var parseSw = Stopwatch.StartNew();
            var assemblyName = $"CompileTest_{Guid.NewGuid().ToString("N")[..8]}";
            var buildItems = InstanceCreator.CreateForAnalyze(query, assemblyName, _schemaProvider, new BenchmarkLoggerResolver());
            parseSw.Stop();
            
            // Stage 2: Compile
            var compileSw = Stopwatch.StartNew();
            var compiledQuery = InstanceCreator.CompileForExecution(query, assemblyName + "_exec", _schemaProvider, new BenchmarkLoggerResolver());
            compileSw.Stop();
            
            totalSw.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            
            return new CompilationResult
            {
                QueryName = name,
                ParseTimeMs = parseSw.ElapsedMilliseconds,
                CompileTimeMs = compileSw.ElapsedMilliseconds,
                TotalCompilationTimeMs = totalSw.ElapsedMilliseconds,
                MemoryUsedBytes = memoryAfter - memoryBefore,
                IsSuccessful = true
            };
        }
        catch (Exception ex)
        {
            totalSw.Stop();
            var memoryAfter = GC.GetTotalMemory(false);
            
            return new CompilationResult
            {
                QueryName = name,
                TotalCompilationTimeMs = totalSw.ElapsedMilliseconds,
                MemoryUsedBytes = memoryAfter - memoryBefore,
                Error = ex.Message,
                IsSuccessful = false
            };
        }
    }

    /// <summary>
    /// Measures compilation time for code generation
    /// </summary>
    private long MeasureCompilationTime(string query)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var assemblyName = $"CompileTest_{Guid.NewGuid().ToString("N")[..8]}";
            var buildItems = InstanceCreator.CreateForAnalyze(query, assemblyName, _schemaProvider, new BenchmarkLoggerResolver());
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
        catch
        {
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Analyzes performance bottlenecks across all phases
    /// </summary>
    private BottleneckAnalysis AnalyzeBottlenecks(ComprehensiveAnalysisReport report)
    {
        Console.WriteLine("\n🔍 Phase 4: Analyzing Performance Bottlenecks...");
        
        var analysis = new BottleneckAnalysis();
        
        // Identify heavy code generation
        var heavyCodeGen = report.CodeGenerationAnalysis.Results
            .Where(r => r.GeneratedLinesOfCode > report.CodeGenerationAnalysis.AverageGeneratedLines * 2)
            .ToList();
        
        if (heavyCodeGen.Any())
        {
            analysis.Bottlenecks.Add(new PerformanceBottleneck
            {
                Category = "Code Generation",
                Description = $"Excessive code generation detected in {heavyCodeGen.Count} queries",
                Impact = "Medium",
                AffectedQueries = heavyCodeGen.Select(r => r.QueryName).ToList(),
                RecommendedAction = "Implement code generation templates and optimize ToCSharpRewriteTreeVisitor"
            });
        }
        
        // Identify reflection-heavy queries
        var reflectionHeavy = report.CodeGenerationAnalysis.Results
            .Where(r => r.ReflectionCalls > 10)
            .ToList();
        
        if (reflectionHeavy.Any())
        {
            analysis.Bottlenecks.Add(new PerformanceBottleneck
            {
                Category = "Reflection Usage",
                Description = $"Heavy reflection usage detected in {reflectionHeavy.Count} queries",
                Impact = "High",
                AffectedQueries = reflectionHeavy.Select(r => r.QueryName).ToList(),
                RecommendedAction = "Implement reflection caching and expression tree compilation"
            });
        }
        
        // Identify slow compilation
        var slowCompilation = report.CompilationAnalysis.Results
            .Where(r => r.TotalCompilationTimeMs > report.CompilationAnalysis.AverageCompilationTime * 2)
            .ToList();
        
        if (slowCompilation.Any())
        {
            analysis.Bottlenecks.Add(new PerformanceBottleneck
            {
                Category = "Compilation Performance",
                Description = $"Slow compilation detected in {slowCompilation.Count} queries",
                Impact = "High",
                AffectedQueries = slowCompilation.Select(r => r.QueryName).ToList(),
                RecommendedAction = "Optimize visitor algorithms and implement compilation caching"
            });
        }
        
        Console.WriteLine($"   ✓ Identified {analysis.Bottlenecks.Count} performance bottlenecks");
        
        return analysis;
    }

    /// <summary>
    /// Generates specific optimization recommendations
    /// </summary>
    private List<OptimizationRecommendation> GenerateOptimizationRecommendations(ComprehensiveAnalysisReport report)
    {
        Console.WriteLine("\n💡 Phase 5: Generating Optimization Recommendations...");
        
        var recommendations = new List<OptimizationRecommendation>();
        
        // Recommendation 1: Optimize Code Generation
        if (report.CodeGenerationAnalysis.AverageGeneratedLines > 100 || report.CodeGenerationAnalysis.TotalReflectionCalls > 50)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Priority = "High",
                Category = "Code Generation",
                Title = "Optimize ToCSharpRewriteTreeVisitor Code Generation",
                Description = "Code generation produces verbose output with heavy reflection usage",
                SpecificIssues = new List<string>
                {
                    $"Average generated lines: {report.CodeGenerationAnalysis.AverageGeneratedLines:F0}",
                    $"Total reflection calls: {report.CodeGenerationAnalysis.TotalReflectionCalls}",
                    $"Average compilation time: {report.CodeGenerationAnalysis.AverageCompilationTime:F2}ms"
                },
                RecommendedActions = new List<string>
                {
                    "Implement code generation templates for common patterns",
                    "Cache reflection calls and use expression trees",
                    "Optimize string concatenation in generated code",
                    "Implement inlining for simple operations",
                    "Use static typing where possible to reduce runtime overhead"
                },
                EstimatedImpact = "20-30% reduction in generated code size, 40-60% reduction in compilation time",
                ImplementationEffort = "3-4 weeks"
            });
        }
        
        // Recommendation 2: Optimize Visitor Patterns
        if (report.VisitorPatternAnalysis.AverageVisitorTime > 5)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Priority = "High",
                Category = "Visitor Performance",
                Title = "Optimize RewriteQueryVisitor and Other Visitor Patterns",
                Description = "Visitor operations show performance overhead during query processing",
                SpecificIssues = new List<string>
                {
                    $"Average visitor time: {report.VisitorPatternAnalysis.AverageVisitorTime:F2}ms",
                    $"Total visitor calls: {report.VisitorPatternAnalysis.TotalVisitorCalls}"
                },
                RecommendedActions = new List<string>
                {
                    "Implement visitor call caching for repeated operations",
                    "Optimize FieldProcessingHelper.CreateAndConcatFields method",
                    "Reduce redundant AST traversals in RewriteQueryTraverseVisitor",
                    "Implement early termination for optimization passes",
                    "Cache expensive calculations in QueryRewriteUtilities"
                },
                EstimatedImpact = "15-25% improvement in query compilation time",
                ImplementationEffort = "2-3 weeks"
            });
        }
        
        // Recommendation 3: Optimize Compilation Pipeline
        if (report.CompilationAnalysis.AverageCompilationTime > 10)
        {
            recommendations.Add(new OptimizationRecommendation
            {
                Priority = "Medium",
                Category = "Compilation Pipeline",
                Title = "Optimize Overall Compilation Pipeline",
                Description = "Compilation pipeline shows performance bottlenecks",
                SpecificIssues = new List<string>
                {
                    $"Average compilation time: {report.CompilationAnalysis.AverageCompilationTime:F2}ms",
                    $"Average memory used: {report.CompilationAnalysis.AverageMemoryUsed / 1024:F0}KB"
                },
                RecommendedActions = new List<string>
                {
                    "Implement assembly caching for repeated compilation",
                    "Optimize metadata generation with better caching",
                    "Reduce memory allocations during compilation",
                    "Implement incremental compilation for query modifications",
                    "Optimize schema provider method resolution"
                },
                EstimatedImpact = "10-20% improvement in overall compilation performance",
                ImplementationEffort = "2-4 weeks"
            });
        }
        
        Console.WriteLine($"   ✓ Generated {recommendations.Count} optimization recommendations");
        
        return recommendations;
    }

    #region Helper Methods
    
    private List<(string Query, string Name)> GetCodeGenerationTestQueries()
    {
        return new List<(string, string)>
        {
            ("SELECT City, Population FROM #test.Entities()", "Simple Select"),
            ("SELECT City FROM #test.Entities() WHERE Population > 1000000", "Simple Where"),
            ("SELECT City, COUNT(*) FROM #test.Entities() GROUP BY City", "Group By"),
            ("SELECT City, Population FROM #test.Entities() ORDER BY Population DESC", "Order By"),
            ("SELECT e1.City, e2.Country FROM #test.Entities() e1 JOIN #test.Entities() e2 ON e1.City = e2.City", "Simple Join"),
            ("WITH CTE AS (SELECT City, Population FROM #test.Entities() WHERE Population > 500000) SELECT * FROM CTE", "CTE"),
            ("SELECT City, CASE WHEN Population > 1000000 THEN 'Large' ELSE 'Small' END AS Size FROM #test.Entities()", "Case Expression"),
            ("SELECT City, Population, ROW_NUMBER() OVER (ORDER BY Population DESC) as Rank FROM #test.Entities()", "Window Function")
        };
    }
    
    private List<(string Query, string Name)> GetVisitorTestQueries()
    {
        return GetCodeGenerationTestQueries();
    }
    
    private List<(string Query, string Name)> GetCompilationTestQueries()
    {
        return GetCodeGenerationTestQueries();
    }
    
    private int EstimateVisitorComplexity(string query)
    {
        // Simple heuristic based on query complexity
        var complexity = 10; // Base complexity
        complexity += query.Count(c => c == '(') * 2; // Parentheses
        complexity += query.Count(c => c == ',') * 1; // Commas
        complexity += CountKeywords(query) * 3; // SQL keywords
        return complexity;
    }
    
    private int CountKeywords(string query)
    {
        var keywords = new[] { "SELECT", "FROM", "WHERE", "GROUP", "ORDER", "JOIN", "WITH", "CASE", "WHEN" };
        var upperQuery = query.ToUpper();
        return keywords.Sum(keyword => CountOccurrences(upperQuery, keyword));
    }
    
    private int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }
    
    #endregion
}

#region Data Models

public class ComprehensiveAnalysisReport
{
    public CodeGenerationPerformanceAnalysis CodeGenerationAnalysis { get; set; } = new();
    public VisitorPatternAnalysis VisitorPatternAnalysis { get; set; } = new();
    public CompilationAnalysis CompilationAnalysis { get; set; } = new();
    public BottleneckAnalysis BottleneckAnalysis { get; set; } = new();
    public List<OptimizationRecommendation> OptimizationRecommendations { get; set; } = new();
}

public class CodeGenerationPerformanceAnalysis
{
    public List<CodeGenerationResult> Results { get; set; } = new();
    public double AverageGeneratedLines { get; set; }
    public double AverageComplexity { get; set; }
    public double AverageCompilationTime { get; set; }
    public int TotalReflectionCalls { get; set; }
}

public class VisitorPatternAnalysis
{
    public List<VisitorResult> Results { get; set; } = new();
    public double AverageVisitorTime { get; set; }
    public int TotalVisitorCalls { get; set; }
}

public class CompilationAnalysis
{
    public List<CompilationResult> Results { get; set; } = new();
    public double AverageCompilationTime { get; set; }
    public double AverageMemoryUsed { get; set; }
}

public class BottleneckAnalysis
{
    public List<PerformanceBottleneck> Bottlenecks { get; set; } = new();
}

public class CodeGenerationResult
{
    public string QueryName { get; set; } = "";
    public int GeneratedLinesOfCode { get; set; }
    public int CodeComplexityScore { get; set; }
    public long CompilationTimeMs { get; set; }
    public int ObjectAllocations { get; set; }
    public int ReflectionCalls { get; set; }
    public int LinqOperations { get; set; }
    public string GeneratedCode { get; set; } = "";
}

public class VisitorResult
{
    public string QueryName { get; set; } = "";
    public long VisitorTimeMs { get; set; }
    public int EstimatedVisitorCalls { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Error { get; set; }
}

public class CompilationResult
{
    public string QueryName { get; set; } = "";
    public long ParseTimeMs { get; set; }
    public long CompileTimeMs { get; set; }
    public long TotalCompilationTimeMs { get; set; }
    public long MemoryUsedBytes { get; set; }
    public bool IsSuccessful { get; set; }
    public string? Error { get; set; }
}

public class PerformanceBottleneck
{
    public string Category { get; set; } = "";
    public string Description { get; set; } = "";
    public string Impact { get; set; } = "";
    public List<string> AffectedQueries { get; set; } = new();
    public string RecommendedAction { get; set; } = "";
}

public class OptimizationRecommendation
{
    public string Priority { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> SpecificIssues { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public string EstimatedImpact { get; set; } = "";
    public string ImplementationEffort { get; set; } = "";
}

#endregion