using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Musoq.Benchmarks.Performance;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Schema;
using Musoq.Schema.Compilation;
using Musoq.Tests.Common;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Benchmarks for validating Phase 2 schema provider optimization performance improvements.
/// Tests method resolution and invocation performance with and without compiled expression trees.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class SchemaProviderOptimizationBenchmark : BenchmarkBase
{
    private string _simpleQuery = null!;
    private string _complexMethodQuery = null!;
    private string _aggregationQuery = null!;
    private string _repeatedMethodQuery = null!;
    private ISchemaProvider _schemaProvider = null!;
    
    private readonly string[] _testData = Enumerable.Range(1, 1000).Select(i => $"Item_{i}").ToArray();

    [GlobalSetup]
    public void Setup()
    {
        SetupSchemaProvider();
        SetupQueries();
    }

    private void SetupSchemaProvider()
    {
        // Create sample profile data for testing
        var profileData = Enumerable.Range(1, 1000).Select(i => new ProfileEntity
        {
            FirstName = $"FirstName{i}",
            LastName = $"LastName{i}",
            Email = $"user{i}@example.com",
            Gender = i % 2 == 0 ? "Male" : "Female",
            IpAddress = $"192.168.1.{i % 255}",
            Date = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd"),
            Image = $"image{i}.jpg",
            Animal = $"Animal{i}",
            Avatar = $"avatar{i}.png"
        }).ToArray();

        var data = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", profileData }
        };
        
        _schemaProvider = new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            data, 
            ProfileEntity.KNameToIndexMap, 
            ProfileEntity.KIndexToObjectAccessMap);
    }

    private void SetupQueries()
    {
        // Simple query with basic field access
        _simpleQuery = "SELECT FirstName, LastName FROM #A.entities() WHERE FirstName like 'FirstName1%'";
        
        // Complex query with multiple field accesses and filtering
        _complexMethodQuery = @"
            SELECT 
                FirstName, 
                LastName,
                Email,
                Gender,
                IpAddress,
                Date,
                Animal
            FROM #A.entities() 
            WHERE FirstName like 'FirstName1%' AND Gender = 'Male'";
            
        // Aggregation query
        _aggregationQuery = @"
            SELECT 
                Gender,
                Count() as ItemCount
            FROM #A.entities() 
            WHERE FirstName like 'FirstName%'
            GROUP BY Gender";
            
        // Query with repeated field access (should still benefit from schema optimizations)
        _repeatedMethodQuery = @"
            SELECT 
                FirstName as Name1,
                FirstName as Name2,
                LastName as LName1,
                LastName as LName2,
                Email as Email1,
                Email as Email2
            FROM #A.entities() 
            WHERE FirstName like 'FirstName1%'";
    }

    [Benchmark(Baseline = true)]
    public object SimpleQuery_WithoutOptimization()
    {
        return ExecuteQueryWithOptimization(_simpleQuery, false);
    }

    [Benchmark]
    public object SimpleQuery_WithOptimization()
    {
        return ExecuteQueryWithOptimization(_simpleQuery, true);
    }

    [Benchmark]
    public object ComplexMethodQuery_WithoutOptimization()
    {
        return ExecuteQueryWithOptimization(_complexMethodQuery, false);
    }

    [Benchmark]
    public object ComplexMethodQuery_WithOptimization()
    {
        return ExecuteQueryWithOptimization(_complexMethodQuery, true);
    }

    [Benchmark]
    public object AggregationQuery_WithoutOptimization()
    {
        return ExecuteQueryWithOptimization(_aggregationQuery, false);
    }

    [Benchmark]
    public object AggregationQuery_WithOptimization()
    {
        return ExecuteQueryWithOptimization(_aggregationQuery, true);
    }

    [Benchmark]
    public object RepeatedMethodQuery_WithoutOptimization()
    {
        return ExecuteQueryWithOptimization(_repeatedMethodQuery, false);
    }

    [Benchmark]
    public object RepeatedMethodQuery_WithOptimization()
    {
        return ExecuteQueryWithOptimization(_repeatedMethodQuery, true);
    }

    /// <summary>
    /// Benchmarks method resolution performance in isolation (compilation phase).
    /// </summary>
    [Benchmark]
    public object MethodResolution_WithoutOptimization()
    {
        return CompileQueryWithOptimization(_complexMethodQuery, false);
    }

    [Benchmark]
    public object MethodResolution_WithOptimization()
    {
        return CompileQueryWithOptimization(_complexMethodQuery, true);
    }

    /// <summary>
    /// Performance tracking version of simple query benchmark.
    /// </summary>
    public void SimpleQuery_WithPerformanceTracking()
    {
        Console.WriteLine("\n📊 Schema Provider Optimization - Simple Query Performance Analysis");
        
        PerformanceMetrics metricsWithout = null!;
        PerformanceMetrics metricsWith = null!;
        
        // Test without optimization
        using (var trackerWithout = EnhancedPerformanceTracker.CreateScoped("SimpleQuery_WithoutOptimization", m => metricsWithout = m))
        {
            var resultWithout = ExecuteQueryWithOptimization(_simpleQuery, false);
        }
        
        // Test with optimization
        using (var trackerWith = EnhancedPerformanceTracker.CreateScoped("SimpleQuery_WithOptimization", m => metricsWith = m))
        {
            var resultWith = ExecuteQueryWithOptimization(_simpleQuery, true);
        }
        
        // Report performance comparison
        ReportPerformanceComparison("Simple Query", metricsWithout, metricsWith);
        
        // Report cache statistics
        if (SchemaMethodCompilationCacheManager.IsEnabled)
        {
            var cacheStats = SchemaMethodCompilationCacheManager.GetStatistics();
            Console.WriteLine($"\n🔧 Schema Method Compilation Cache Statistics:");
            Console.WriteLine($"   Cache Efficiency: {cacheStats.CacheEfficiency:P1}");
            Console.WriteLine($"   Total Requests: {cacheStats.TotalRequests:N0}");
            Console.WriteLine($"   Cache Hits: {cacheStats.CacheHits:N0}");
            Console.WriteLine($"   Cache Misses: {cacheStats.CacheMisses:N0}");
            Console.WriteLine($"   Average Compilation Time: {cacheStats.AverageCompilationTime.TotalMilliseconds:F2}ms");
        }
    }

    /// <summary>
    /// Performance tracking version of complex method query benchmark.
    /// </summary>
    public void ComplexMethodQuery_WithPerformanceTracking()
    {
        Console.WriteLine("\n📊 Schema Provider Optimization - Complex Method Query Performance Analysis");
        
        // Clear cache to ensure fair comparison
        SchemaMethodCompilationCacheManager.ClearCache();
        
        PerformanceMetrics metricsWithout = null!;
        PerformanceMetrics metricsWith = null!;
        
        // Test without optimization
        using (var trackerWithout = EnhancedPerformanceTracker.CreateScoped("ComplexMethodQuery_WithoutOptimization", m => metricsWithout = m))
        {
            var resultWithout = ExecuteQueryWithOptimization(_complexMethodQuery, false);
        }
        
        // Test with optimization
        using (var trackerWith = EnhancedPerformanceTracker.CreateScoped("ComplexMethodQuery_WithOptimization", m => metricsWith = m))
        {
            var resultWith = ExecuteQueryWithOptimization(_complexMethodQuery, true);
        }
        
        // Report performance comparison
        ReportPerformanceComparison("Complex Method Query", metricsWithout, metricsWith);
        
        // Report method compilation cache effectiveness
        var cacheStats = SchemaMethodCompilationCacheManager.GetStatistics();
        Console.WriteLine($"\n🔧 Method Compilation Performance:");
        Console.WriteLine($"   Cached Methods: {cacheStats.TotalEntries:N0}");
        Console.WriteLine($"   Cache Efficiency: {cacheStats.CacheEfficiency:P1}");
        Console.WriteLine($"   Average Compilation Time: {cacheStats.AverageCompilationTime.TotalMilliseconds:F2}ms");
    }

    /// <summary>
    /// Reports performance comparison between optimized and non-optimized execution.
    /// </summary>
    private static void ReportPerformanceComparison(string testName, 
        PerformanceMetrics withoutOptimization,
        PerformanceMetrics withOptimization)
    {
        var executionImprovement = CalculateImprovementPercentage(
            withoutOptimization.ExecutionTime.TotalMilliseconds,
            withOptimization.ExecutionTime.TotalMilliseconds);
            
        var memoryImprovement = CalculateImprovementPercentage(
            withoutOptimization.MemoryAllocated,
            withOptimization.MemoryAllocated);
            
        var gcImprovement = CalculateImprovementPercentage(
            withoutOptimization.TotalGCCollections,
            withOptimization.TotalGCCollections);

        Console.WriteLine($"\n🎯 {testName} Performance Results:");
        Console.WriteLine($"   Execution Time: {withoutOptimization.ExecutionTime.TotalMilliseconds:F2}ms → {withOptimization.ExecutionTime.TotalMilliseconds:F2}ms ({executionImprovement:+0.0;-0.0}%)");
        Console.WriteLine($"   Memory Allocated: {withoutOptimization.MemoryAllocated / 1024.0:F1}KB → {withOptimization.MemoryAllocated / 1024.0:F1}KB ({memoryImprovement:+0.0;-0.0}%)");
        Console.WriteLine($"   GC Collections: {withoutOptimization.TotalGCCollections} → {withOptimization.TotalGCCollections} ({gcImprovement:+0.0;-0.0}%)");
    }

    /// <summary>
    /// Calculates improvement percentage (negative means improvement).
    /// </summary>
    private static double CalculateImprovementPercentage(double before, double after)
    {
        if (before == 0) return 0;
        return ((after - before) / before) * 100.0;
    }

    /// <summary>
    /// Executes a query with or without schema provider optimization.
    /// </summary>
    private object ExecuteQueryWithOptimization(string query, bool useOptimization)
    {
        // Set optimization state
        var originalState = SchemaMethodCompilationCacheManager.IsEnabled;
        SchemaMethodCompilationCacheManager.IsEnabled = useOptimization;
        
        try
        {
            var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), _schemaProvider, LoggerResolver);
            var result = compiled.Run();
            
            // Materialize the result to ensure execution is complete
            var materialized = new List<object>();
            foreach (var row in result)
            {
                materialized.Add(row);
            }
            
            return materialized;
        }
        finally
        {
            // Restore original state
            SchemaMethodCompilationCacheManager.IsEnabled = originalState;
        }
    }

    /// <summary>
    /// Compiles a query (without execution) with or without schema provider optimization.
    /// Used to isolate compilation performance from execution performance.
    /// </summary>
    private object CompileQueryWithOptimization(string query, bool useOptimization)
    {
        // Set optimization state
        var originalState = SchemaMethodCompilationCacheManager.IsEnabled;
        SchemaMethodCompilationCacheManager.IsEnabled = useOptimization;
        
        try
        {
            return InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), _schemaProvider, LoggerResolver);
        }
        finally
        {
            // Restore original state
            SchemaMethodCompilationCacheManager.IsEnabled = originalState;
        }
    }
}