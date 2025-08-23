using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Musoq.Benchmarks.Components;
using Musoq.Benchmarks.Performance;
using Musoq.Benchmarks.Schema;
using Musoq.Benchmarks.Schema.Profiles;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Cache;
using Musoq.Schema;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Benchmark for measuring the performance impact of the new assembly caching system.
/// This validates the 40-60% compilation overhead reduction identified in Phase 1 optimization goals.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[GcForce(true)]
public class AssemblyCachingBenchmark : BenchmarkBase
{
    private ISchemaProvider _schemaProvider = null!;
    private string _simpleQuery = null!;
    private string _complexQuery = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create test data - simple profile entities
        var profileData = new List<ProfileEntity>();
        var random = new Random(42); // Deterministic seed for consistent benchmarks
        
        for (int i = 0; i < 1_000; i++)
        {
            profileData.Add(new ProfileEntity
            {
                FirstName = $"FirstName{i}",
                LastName = $"LastName{i}",
                Email = $"user{i}@example.com",
                Gender = i % 2 == 0 ? "Male" : "Female",
                IpAddress = $"192.168.1.{i % 255}",
                Date = DateTime.Now.AddDays(-random.Next(365)).ToString("yyyy-MM-dd"),
                Image = $"image{i}.jpg",
                Animal = $"Animal{i}",
                Avatar = $"avatar{i}.png"
            });
        }
        
        var data = new Dictionary<string, IEnumerable<ProfileEntity>>
        {
            { "#A", profileData }
        };
        
        _schemaProvider = new GenericSchemaProvider<ProfileEntity, ProfileEntityTable>(
            data, 
            ProfileEntity.KNameToIndexMap, 
            ProfileEntity.KIndexToObjectAccessMap);
            
        _simpleQuery = "SELECT FirstName, LastName FROM #A.profiles() WHERE FirstName like 'FirstName1%'";
        _complexQuery = @"
            SELECT 
                FirstName, 
                LastName,
                Email,
                CASE 
                    WHEN Gender = 'Male' THEN 'M'
                    WHEN Gender = 'Female' THEN 'F' 
                    ELSE 'U' 
                END as GenderCode,
                Count(*) OVER (PARTITION BY Gender) as GenderCount
            FROM #A.profiles() 
            WHERE Email like '%.com'
            GROUP BY FirstName, LastName, Email, Gender
            ORDER BY LastName";

        // Clear cache before benchmarks
        QueryAssemblyCacheManager.Reset();
    }

    /// <summary>
    /// Baseline: Simple query compilation without caching
    /// </summary>
    [Benchmark(Baseline = true)]
    public void SimpleQuery_WithoutCache()
    {
        QueryAssemblyCacheManager.IsEnabled = false;
        
        var compiledQuery = InstanceCreator.CompileForExecution(
            _simpleQuery,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        var result = compiledQuery.Run();
        // Consume result to ensure execution
        var rowCount = result.Count;
    }

    /// <summary>
    /// Test: Simple query compilation with caching (first execution - cache miss)
    /// </summary>
    [Benchmark]
    public void SimpleQuery_WithCache_FirstExecution()
    {
        QueryAssemblyCacheManager.IsEnabled = true;
        QueryAssemblyCacheManager.Reset(); // Ensure cache miss
        
        var compiledQuery = InstanceCreator.CompileForExecution(
            _simpleQuery,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        var result = compiledQuery.Run();
        var rowCount = result.Count;
    }

    /// <summary>
    /// Test: Simple query compilation with caching (subsequent execution - cache hit)
    /// </summary>
    [Benchmark]
    public void SimpleQuery_WithCache_CacheHit()
    {
        QueryAssemblyCacheManager.IsEnabled = true;
        
        // Warm up cache first
        var warmupQuery = InstanceCreator.CompileForExecution(
            _simpleQuery,
            "warmup",
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        // Now measure cache hit
        var compiledQuery = InstanceCreator.CompileForExecution(
            _simpleQuery,
            "cached",
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        var result = compiledQuery.Run();
        var rowCount = result.Count;
    }

    /// <summary>
    /// Baseline: Complex query compilation without caching
    /// </summary>
    [Benchmark]
    public void ComplexQuery_WithoutCache()
    {
        QueryAssemblyCacheManager.IsEnabled = false;
        
        var compiledQuery = InstanceCreator.CompileForExecution(
            _complexQuery,
            Guid.NewGuid().ToString(),
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        var result = compiledQuery.Run();
        var rowCount = result.Count;
    }

    /// <summary>
    /// Test: Complex query compilation with caching (cache hit)
    /// </summary>
    [Benchmark]
    public void ComplexQuery_WithCache_CacheHit()
    {
        QueryAssemblyCacheManager.IsEnabled = true;
        
        // Warm up cache first
        var warmupQuery = InstanceCreator.CompileForExecution(
            _complexQuery,
            "warmup",
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        // Now measure cache hit
        var compiledQuery = InstanceCreator.CompileForExecution(
            _complexQuery,
            "cached",
            _schemaProvider,
            new BenchmarkLoggerResolver());
        
        var result = compiledQuery.Run();
        var rowCount = result.Count;
    }

    /// <summary>
    /// Measures cache performance with repeated identical queries
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    public void RepeatedQueries_WithCache(int repetitions)
    {
        QueryAssemblyCacheManager.IsEnabled = true;
        QueryAssemblyCacheManager.Reset();
        
        for (int i = 0; i < repetitions; i++)
        {
            var compiledQuery = InstanceCreator.CompileForExecution(
                _simpleQuery,
                "repeated",
                _schemaProvider,
                new BenchmarkLoggerResolver());
            
            var result = compiledQuery.Run();
            var rowCount = result.Count;
        }
    }

    /// <summary>
    /// Measures performance without cache for comparison with repeated queries
    /// </summary>
    [Benchmark]
    [Arguments(10)]
    public void RepeatedQueries_WithoutCache(int repetitions)
    {
        QueryAssemblyCacheManager.IsEnabled = false;
        
        for (int i = 0; i < repetitions; i++)
        {
            var compiledQuery = InstanceCreator.CompileForExecution(
                _simpleQuery,
                Guid.NewGuid().ToString(), // Different assembly name each time
                _schemaProvider,
                new BenchmarkLoggerResolver());
            
            var result = compiledQuery.Run();
            var rowCount = result.Count;
        }
    }

    /// <summary>
    /// Tests cache statistics and efficiency
    /// </summary>
    [GlobalCleanup]
    public void CacheStatistics()
    {
        var stats = QueryAssemblyCacheManager.Instance.GetStatistics();
        Console.WriteLine($"""
            
            📊 Assembly Cache Statistics:
            ═══════════════════════════════
            Total Entries: {stats.TotalEntries}
            Valid Entries: {stats.ValidEntries}
            Expired Entries: {stats.ExpiredEntries}
            Cache Efficiency: {stats.CacheEfficiency:P1}
            Max Size: {stats.MaxSize}
            """);
    }
}