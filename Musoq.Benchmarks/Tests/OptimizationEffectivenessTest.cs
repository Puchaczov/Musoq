using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Musoq.Evaluator.Optimization;
using Musoq.Schema;
using Musoq.Tests.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Musoq.Benchmarks.Tests;

/// <summary>
/// Practical performance test demonstrating the effectiveness of Phase 4 optimizations.
/// Tests reflection caching, template generation, and staged transformation optimizations.
/// </summary>
public class OptimizationEffectivenessTest
{
    private readonly ILogger<OptimizationEffectivenessTest> _logger;
    
    public OptimizationEffectivenessTest()
    {
        _logger = new NullLogger<OptimizationEffectivenessTest>();
    }
    
    /// <summary>
    /// Runs comprehensive optimization effectiveness testing.
    /// Measures performance with and without optimizations enabled.
    /// </summary>
    public async Task<OptimizationPerformanceReport> RunOptimizationEffectivenessTestAsync()
    {
        _logger.LogInformation("Starting Phase 4 Optimization Effectiveness Test");
        
        var report = new OptimizationPerformanceReport
        {
            TestName = "Phase 4 Code Generation Optimizations",
            TestDate = DateTime.UtcNow,
            Baseline = await MeasureBaselinePerformance(),
            Optimized = await MeasureOptimizedPerformance()
        };
        
        report.CalculateImprovements();
        
        _logger.LogInformation($"Optimization Test Complete - Total Improvement: {report.TotalImprovement:P2}");
        
        return report;
    }
    
    /// <summary>
    /// Measures baseline performance without optimizations.
    /// </summary>
    private async Task<PerformanceMetrics> MeasureBaselinePerformance()
    {
        _logger.LogInformation("Measuring baseline performance (optimizations disabled)");
        
        // Configure without optimizations
        var configuration = new OptimizationConfiguration
        {
            EnableReflectionCaching = false,
            EnableTemplateGeneration = false, 
            EnableStagedTransformation = false,
            EnableCachePreWarming = false
        };
        
        return await MeasurePerformanceWithConfiguration(configuration, "Baseline");
    }
    
    /// <summary>
    /// Measures optimized performance with all Phase 4 optimizations enabled.
    /// </summary>
    private async Task<PerformanceMetrics> MeasureOptimizedPerformance()
    {
        _logger.LogInformation("Measuring optimized performance (all optimizations enabled)");
        
        // Configure with all optimizations
        var configuration = new OptimizationConfiguration
        {
            EnableReflectionCaching = true,
            EnableTemplateGeneration = true,
            EnableStagedTransformation = true,
            EnableCachePreWarming = true
        };
        
        return await MeasurePerformanceWithConfiguration(configuration, "Optimized");
    }
    
    /// <summary>
    /// Measures performance with specific optimization configuration.
    /// </summary>
    private async Task<PerformanceMetrics> MeasurePerformanceWithConfiguration(
        OptimizationConfiguration config, string scenario)
    {
        var stopwatch = new Stopwatch();
        var metrics = new PerformanceMetrics { Scenario = scenario };
        
        // Test 1: Reflection Caching Impact
        stopwatch.Restart();
        await TestReflectionPerformance(config);
        stopwatch.Stop();
        metrics.ReflectionTime = stopwatch.ElapsedMilliseconds;
        
        // Test 2: Code Generation Template Impact  
        stopwatch.Restart();
        await TestCodeGenerationPerformance(config);
        stopwatch.Stop();
        metrics.CodeGenerationTime = stopwatch.ElapsedMilliseconds;
        
        // Test 3: Staged Transformation Impact
        stopwatch.Restart();
        await TestStagedTransformationPerformance(config);
        stopwatch.Stop();
        metrics.StagedTransformationTime = stopwatch.ElapsedMilliseconds;
        
        // Test 4: End-to-End Query Performance
        stopwatch.Restart();
        await TestEndToEndQueryPerformance(config);
        stopwatch.Stop();
        metrics.EndToEndTime = stopwatch.ElapsedMilliseconds;
        
        metrics.TotalTime = metrics.ReflectionTime + metrics.CodeGenerationTime + 
                           metrics.StagedTransformationTime + metrics.EndToEndTime;
        
        _logger.LogInformation($"{scenario} Performance - Total: {metrics.TotalTime}ms");
        
        return metrics;
    }
    
    /// <summary>
    /// Tests reflection caching performance by performing multiple type lookups.
    /// </summary>
    private async Task TestReflectionPerformance(OptimizationConfiguration config)
    {
        if (config.EnableReflectionCaching)
        {
            TypeCacheManager.ClearCaches();
            if (config.EnableCachePreWarming)
            {
                TypeCacheManager.PreWarmCache();
            }
        }
        
        // Simulate heavy reflection usage
        for (int i = 0; i < 1000; i++)
        {
            if (config.EnableReflectionCaching)
            {
                _ = TypeCacheManager.GetCachedType("System.String");
                _ = TypeCacheManager.GetCachedType("System.Int32");
                _ = TypeCacheManager.GetCachedCastableTypeName(typeof(decimal));
            }
            else
            {
                // Baseline reflection without caching
                _ = Type.GetType("System.String");
                _ = Type.GetType("System.Int32"); 
                _ = typeof(decimal).Name.ToLower();
            }
        }
        
        await Task.Delay(1); // Simulate async work
    }
    
    /// <summary>
    /// Tests code generation template performance.
    /// </summary>
    private async Task TestCodeGenerationPerformance(OptimizationConfiguration config)
    {
        if (config.EnableTemplateGeneration)
        {
            // Generate templates for common patterns
            for (int i = 0; i < 100; i++)
            {
                var className = $"GeneratedQuery_{i}";
                var sourceExpression = "provider.GetTable(\"test\")";
                var fieldExpressions = new[] { "row[\"Name\"]", "row[\"Age\"]", "row[\"City\"]" };
                var filterExpression = i % 2 == 0 ? "row[\"Active\"] == true" : null;
                
                var template = CodeGenerationTemplates.SimpleSelectTemplate(
                    className, sourceExpression, fieldExpressions, filterExpression);
                _ = template.Length; // Use the template
                
                // Also test aggregation template
                if (i % 4 == 0)
                {
                    var groupByFields = new[] { "row[\"Category\"]" };
                    var aggregationFields = new[] { "Count(*)", "Sum(row[\"Amount\"])" };
                    var aggTemplate = CodeGenerationTemplates.AggregationTemplate(
                        $"AggQuery_{i}", sourceExpression, groupByFields, aggregationFields);
                    _ = aggTemplate.Length;
                }
            }
        }
        else
        {
            // Baseline: Generate code without templates 
            for (int i = 0; i < 100; i++)
            {
                var basicCode = GenerateBasicCode(i);
                _ = basicCode.Length;
            }
        }
        
        await Task.Delay(1);
    }
    
    /// <summary>
    /// Tests staged transformation performance.
    /// </summary>
    private async Task TestStagedTransformationPerformance(OptimizationConfiguration config)
    {
        if (config.EnableStagedTransformation)
        {
            var manager = new StagedTransformationManager();
            
            for (int i = 0; i < 50; i++)
            {
                var context = new QueryAnalysisContext
                {
                    HasFiltering = i % 2 == 0,
                    HasProjections = true,
                    HasJoins = i % 3 == 0,
                    HasAggregations = i % 4 == 0,
                    ComplexityScore = i % 10
                };
                
                var plan = manager.AnalyzeAndCreatePlan(context);
                _ = plan.Stages.Count; // Use the plan
            }
        }
        else
        {
            // Baseline: Simple processing without staging
            for (int i = 0; i < 50; i++)
            {
                var simpleProcessing = ProcessWithoutStaging(i);
                _ = simpleProcessing;
            }
        }
        
        await Task.Delay(1);
    }
    
    /// <summary>
    /// Tests end-to-end query performance with real schema operations.
    /// </summary>
    private async Task TestEndToEndQueryPerformance(OptimizationConfiguration config)
    {
        var manager = config.EnableReflectionCaching || config.EnableTemplateGeneration || 
                     config.EnableStagedTransformation 
                     ? new OptimizationManager(configuration: config) 
                     : null;
        
        // Simulate query processing
        for (int i = 0; i < 20; i++)
        {
            var input = new QueryAnalysisInput
            {
                QueryId = $"test_query_{i}",
                Pattern = new QueryPattern
                {
                    HasJoins = i % 2 == 0,
                    HasAggregations = i % 3 == 0,
                    ComplexityScore = i % 10,
                    RequiredFields = new[] { "Field1", "Field2", "Field3" },
                    RequiredTypes = new[] { typeof(string), typeof(int), typeof(DateTime) }
                },
                Context = new QueryAnalysisContext
                {
                    HasFiltering = i % 2 == 0,
                    HasProjections = true,
                    HasJoins = i % 2 == 0,
                    HasAggregations = i % 3 == 0,
                    ComplexityScore = i % 10
                }
            };
            
            if (manager != null)
            {
                var plan = manager.AnalyzeQuery(input);
                _ = plan.EstimatedImprovement; // Use the plan
            }
        }
        
        await Task.Delay(1);
    }
    
    /// <summary>
    /// Generates basic code without template optimization (baseline).
    /// </summary>
    private string GenerateBasicCode(int iteration)
    {
        return $@"
public class GeneratedQuery_{iteration}
{{
    public IEnumerable<object[]> Run()
    {{
        // Basic unoptimized code generation
        var results = new List<object[]>();
        for (int i = 0; i < 100; i++)
        {{
            results.Add(new object[] {{ ""value"", i }});
        }}
        return results;
    }}
}}";
    }
    
    /// <summary>
    /// Processes data without staged transformation optimization (baseline).
    /// </summary>
    private int ProcessWithoutStaging(int iteration)
    {
        // Simple monolithic processing
        var result = 0;
        for (int i = 0; i < 10; i++)
        {
            result += i * iteration;
        }
        return result;
    }
}

/// <summary>
/// Performance metrics for optimization effectiveness testing.
/// </summary>
public class PerformanceMetrics
{
    public string Scenario { get; set; }
    public long ReflectionTime { get; set; }
    public long CodeGenerationTime { get; set; }
    public long StagedTransformationTime { get; set; }
    public long EndToEndTime { get; set; }
    public long TotalTime { get; set; }
}

/// <summary>
/// Complete performance report comparing baseline vs optimized scenarios.
/// </summary>
public class OptimizationPerformanceReport
{
    public string TestName { get; set; }
    public DateTime TestDate { get; set; }
    public PerformanceMetrics Baseline { get; set; }
    public PerformanceMetrics Optimized { get; set; }
    
    // Improvement percentages
    public double ReflectionImprovement { get; set; }
    public double CodeGenerationImprovement { get; set; }
    public double StagedTransformationImprovement { get; set; }
    public double EndToEndImprovement { get; set; }
    public double TotalImprovement { get; set; }
    
    public void CalculateImprovements()
    {
        ReflectionImprovement = CalculateImprovementPercentage(Baseline.ReflectionTime, Optimized.ReflectionTime);
        CodeGenerationImprovement = CalculateImprovementPercentage(Baseline.CodeGenerationTime, Optimized.CodeGenerationTime);
        StagedTransformationImprovement = CalculateImprovementPercentage(Baseline.StagedTransformationTime, Optimized.StagedTransformationTime);
        EndToEndImprovement = CalculateImprovementPercentage(Baseline.EndToEndTime, Optimized.EndToEndTime);
        TotalImprovement = CalculateImprovementPercentage(Baseline.TotalTime, Optimized.TotalTime);
    }
    
    private double CalculateImprovementPercentage(long baseline, long optimized)
    {
        if (baseline == 0) return 0;
        return ((double)(baseline - optimized) / baseline) * 100;
    }
    
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}