using System;
using BenchmarkDotNet.Attributes;
using Musoq.Schema.Performance;

namespace Musoq.Benchmarks.Components;

/// <summary>
/// Simple benchmarks for Phase 3 memory management optimization
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class MemoryManagementBenchmark
{
    [GlobalSetup]
    public void Setup()
    {
        Console.WriteLine("Setting up Memory Management Benchmarks...");
        
        // Pre-warm memory pools
        MemoryPoolManager.PreWarmPools(tableCapacity: 20, resolverCapacity: 200);
    }

    [Benchmark]
    public void Memory_Pool_Statistics()
    {
        MemoryPoolManager.IsEnabled = true;
        
        // Get statistics (this exercises the statistics collection)
        var stats = MemoryPoolManager.GetStatistics();
        var efficiency = stats.TableCacheEfficiency + stats.ResolverCacheEfficiency;
    }

    [Benchmark]
    public void Memory_Pool_PreWarming()
    {
        MemoryPoolManager.IsEnabled = true;
        
        // Pre-warm pools
        MemoryPoolManager.PreWarmPools(tableCapacity: 10, resolverCapacity: 50);
    }
}