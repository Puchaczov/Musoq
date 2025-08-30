using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Musoq.Evaluator.Optimization;

namespace Musoq.Benchmarks.Programs;

/// <summary>
/// Simple performance test to validate actual optimization improvements.
/// </summary>
public class SimpleOptimizationBenchmark
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Simple Optimization Performance Test ===");
        Console.WriteLine();
        
        try
        {
            var baselineTime = await MeasureWithoutOptimizations();
            var optimizedTime = await MeasureWithOptimizations();
            
            var improvement = ((double)(baselineTime - optimizedTime) / baselineTime) * 100;
            
            Console.WriteLine($"Baseline (no optimizations): {baselineTime}ms");
            Console.WriteLine($"Optimized (Phase 2 + Phase 4): {optimizedTime}ms");
            Console.WriteLine($"Improvement: {improvement:F1}%");
            Console.WriteLine();
            
            if (improvement >= 15.0)
            {
                Console.WriteLine("✅ Optimization targets achieved!");
            }
            else
            {
                Console.WriteLine("⚠️  Modest optimization improvement");
            }
            
            // Save result
            var reportPath = Path.Combine("performance-reports", $"simple-optimization-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? ".");
            
            var report = new { 
                TestDate = DateTime.UtcNow,
                BaselineMs = baselineTime,
                OptimizedMs = optimizedTime,
                ImprovementPercent = improvement
            };
            
            await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Report saved to: {reportPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }
    
    private static async Task<long> MeasureWithoutOptimizations()
    {
        var config = new OptimizationConfiguration
        {
            EnableExpressionTreeCompilation = false,
            EnableMemoryPooling = false,
            EnableReflectionCaching = false,
            EnableTemplateGeneration = false,
            EnableStagedTransformation = false
        };
        
        return await RunBenchmark(config);
    }
    
    private static async Task<long> MeasureWithOptimizations()
    {
        var config = new OptimizationConfiguration
        {
            EnableExpressionTreeCompilation = true,
            EnableMemoryPooling = true,
            EnableReflectionCaching = true,
            EnableTemplateGeneration = true,
            EnableStagedTransformation = true
        };
        
        return await RunBenchmark(config);
    }
    
    private static async Task<long> RunBenchmark(OptimizationConfiguration config)
    {
        var optimizationManager = new OptimizationManager(configuration: config);
        var stopwatch = Stopwatch.StartNew();
        
        // Run optimization tasks that represent real workload
        for (int i = 0; i < 1000; i++)
        {
            var input = new QueryAnalysisInput
            {
                QueryId = $"benchmark_query_{i}",
                Pattern = new QueryPattern
                {
                    HasJoins = i % 3 == 0,
                    HasAggregations = i % 4 == 0,
                    ComplexityScore = i % 10,
                    RequiredFields = new[] { "Id", "Name", "Value" },
                    RequiredTypes = new[] { typeof(int), typeof(string), typeof(decimal) }
                }
            };
            
            var plan = optimizationManager.AnalyzeQuery(input);
            var result = optimizationManager.GenerateOptimizedCode(plan, $"Query_{i}");
            
            // Simulate using the result
            var codeLength = result.GeneratedCode.Length;
        }
        
        // Include some type caching operations
        if (config.EnableReflectionCaching)
        {
            for (int i = 0; i < 100; i++)
            {
                TypeCacheManager.GetCachedType("System.String");
                TypeCacheManager.GetCachedType("System.Int32");
                TypeCacheManager.GetCachedCastableTypeName(typeof(decimal));
            }
        }
        
        stopwatch.Stop();
        await Task.Delay(1); // Ensure async
        return stopwatch.ElapsedMilliseconds;
    }
}