using System;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Musoq.Evaluator;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Performance;
using Musoq.Evaluator.Tables;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Benchmarks for Phase 3 memory management optimization
/// Measures memory allocation reduction and pooling effectiveness
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryManagementBenchmark : BenchmarkBase
{
    private const int DataSize = 1000;
    private readonly Random _random = new(42); // Fixed seed for consistency

    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("🧠 Setting up Memory Management Benchmarks...");
        
        // Pre-warm memory pools
        MemoryPoolManager.PreWarmPools(tableCapacity: 20, resolverCapacity: 200);
        
        Console.WriteLine($"Memory pools pre-warmed. Status: {MemoryPoolManager.GetEfficiencySummary()}");
    }

    [Benchmark]
    public void Table_Creation_WithoutPooling()
    {
        MemoryPoolManager.IsEnabled = false;
        
        using var tracker = CreatePerformanceTracker("Table_Creation_WithoutPooling");
        
        // Create multiple tables without pooling
        for (int i = 0; i < 50; i++)
        {
            var columns = new Musoq.Evaluator.Tables.Column[3];
            columns[0] = new Musoq.Evaluator.Tables.Column($"col_0", typeof(int), 0);
            columns[1] = new Musoq.Evaluator.Tables.Column($"col_1", typeof(int), 1);
            columns[2] = new Musoq.Evaluator.Tables.Column($"col_2", typeof(int), 2);
            
            using var table = new Table($"test_table_{i}", columns);
            
            // Add some data
            for (int j = 0; j < 20; j++)
            {
                var row = new Musoq.Evaluator.Tables.Row([_random.Next(1000), _random.Next(1000), _random.Next(1000)]);
                table.Add(new Musoq.Evaluator.Tables.Key([i, j]), row);
            }
        }
        
        GC.Collect(); // Force collection to measure allocation impact
    }

    [Benchmark]
    public void Table_Creation_WithPooling()
    {
        MemoryPoolManager.IsEnabled = true;
        
        using var tracker = CreatePerformanceTracker("Table_Creation_WithPooling");
        
        // Create multiple tables with pooling
        for (int i = 0; i < 50; i++)
        {
            using var table = MemoryPool.RentTable($"test_table_{i}", Array.Empty<ISchemaColumn>());
            
            // Add some data using pooled resolvers
            for (int j = 0; j < 20; j++)
            {
                var resolver = table.AddPooledRow();
                resolver[$"col_{j}"] = _random.Next(1000);
            }
        }
        
        GC.Collect(); // Force collection to measure allocation impact
    }

    [Benchmark]
    public void Resolver_Creation_WithoutPooling()
    {
        MemoryPoolManager.IsEnabled = false;
        
        using var tracker = CreatePerformanceTracker("Resolver_Creation_WithoutPooling");
        
        // Create many resolvers without pooling
        for (int i = 0; i < 1000; i++)
        {
            var values = new Dictionary<string, object>();
            
            // Add some data
            for (int j = 0; j < 5; j++)
            {
                values[$"field_{j}"] = _random.Next(100);
            }
            
            // Simulate usage
            var sum = 0;
            for (int j = 0; j < 5; j++)
            {
                sum += (int)(values[$"field_{j}"] ?? 0);
            }
        }
        
        GC.Collect();
    }

    [Benchmark]
    public void Resolver_Creation_WithPooling()
    {
        MemoryPoolManager.IsEnabled = true;
        
        using var tracker = CreatePerformanceTracker("Resolver_Creation_WithPooling");
        
        // Create many resolvers with pooling
        for (int i = 0; i < 1000; i++)
        {
            using var resolver = MemoryPool.RentResolver();
            
            // Add some data
            for (int j = 0; j < 5; j++)
            {
                resolver[$"field_{j}"] = _random.Next(100);
            }
            
            // Simulate usage
            var sum = 0;
            for (int j = 0; j < 5; j++)
            {
                sum += (int)(resolver[$"field_{j}"] ?? 0);
            }
        }
        
        GC.Collect();
    }

    [Benchmark]
    public void Query_Execution_WithoutMemoryPooling()
    {
        MemoryPoolManager.IsEnabled = false;
        
        using var tracker = CreatePerformanceTracker("Query_Execution_WithoutMemoryPooling");
        
        var query = "SELECT Id, Name FROM #test.entities() WHERE Id > 50 ORDER BY Name";
        ExecuteQuery(query);
    }

    [Benchmark] 
    public void Query_Execution_WithMemoryPooling()
    {
        MemoryPoolManager.IsEnabled = true;
        
        using var tracker = CreatePerformanceTracker("Query_Execution_WithMemoryPooling");
        
        var query = "SELECT Id, Name FROM #test.entities() WHERE Id > 50 ORDER BY Name";
        ExecuteQuery(query);
    }

    [Benchmark]
    public void Repeated_Queries_WithoutMemoryPooling()
    {
        MemoryPoolManager.IsEnabled = false;
        
        using var tracker = CreatePerformanceTracker("Repeated_Queries_WithoutMemoryPooling");
        
        // Execute multiple similar queries
        for (int i = 0; i < 10; i++)
        {
            var query = $"SELECT Id, Name FROM #test.entities() WHERE Id > {i * 10} ORDER BY Name";
            ExecuteQuery(query);
        }
    }

    [Benchmark]
    public void Repeated_Queries_WithMemoryPooling()
    {
        MemoryPoolManager.IsEnabled = true;
        
        using var tracker = CreatePerformanceTracker("Repeated_Queries_WithMemoryPooling");
        
        // Execute multiple similar queries
        for (int i = 0; i < 10; i++)
        {
            var query = $"SELECT Id, Name FROM #test.entities() WHERE Id > {i * 10} ORDER BY Name";
            ExecuteQuery(query);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        var stats = MemoryPoolManager.GetStatistics();
        Console.WriteLine($"\n🎯 Memory Management Benchmark Results:");
        Console.WriteLine($"Final Pool Statistics: {stats}");
        Console.WriteLine($"Pool Effectiveness: {(MemoryPoolManager.IsEffective() ? "✅ Effective" : "❌ Not Effective")}");
        
        // Calculate potential memory savings
        var totalRequests = stats.TableHits + stats.TableMisses + stats.ResolverHits + stats.ResolverMisses;
        var totalHits = stats.TableHits + stats.ResolverHits;
        
        if (totalRequests > 0)
        {
            var overallEfficiency = (double)totalHits / totalRequests;
            var estimatedMemorySavings = overallEfficiency * 100;
            Console.WriteLine($"Estimated Memory Allocation Reduction: {estimatedMemorySavings:F1}%");
        }
        
        MemoryPoolManager.ClearPools();
    }

    private void ExecuteQuery(string query)
    {
        try
        {
            var compiledQuery = InstanceCreator.CompileForExecution(
                query,
                Guid.NewGuid().ToString(),
                CreateSchemaProvider(),
                LoggerResolver);

            var result = compiledQuery.Run();
            var rows = result.ToArray(); // Materialize to measure full execution
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Query execution failed: {ex.Message}");
        }
    }
}