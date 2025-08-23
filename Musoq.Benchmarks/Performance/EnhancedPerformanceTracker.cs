using System;
using System.Diagnostics;
using System.Runtime;

namespace Musoq.Benchmarks.Performance;

/// <summary>
/// Enhanced performance tracker that includes memory allocation and GC tracking
/// for comprehensive performance analysis. This addresses the enhanced monitoring
/// requirement in Phase 1 of the performance optimization roadmap.
/// </summary>
public class EnhancedPerformanceTracker
{
    private readonly string _name;
    private readonly Stopwatch _stopwatch;
    private readonly long _initialMemory;
    private readonly int _initialGen0;
    private readonly int _initialGen1;
    private readonly int _initialGen2;
    private readonly long _initialAllocatedBytes;

    public EnhancedPerformanceTracker(string name)
    {
        _name = name;
        
        // Force garbage collection to get accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        _initialMemory = GC.GetTotalMemory(false);
        _initialGen0 = GC.CollectionCount(0);
        _initialGen1 = GC.CollectionCount(1);
        _initialGen2 = GC.CollectionCount(2);
        _initialAllocatedBytes = GC.GetTotalAllocatedBytes(false);
        
        _stopwatch = Stopwatch.StartNew();
    }

    public PerformanceMetrics Complete()
    {
        _stopwatch.Stop();
        
        var finalMemory = GC.GetTotalMemory(false);
        var finalGen0 = GC.CollectionCount(0);
        var finalGen1 = GC.CollectionCount(1);
        var finalGen2 = GC.CollectionCount(2);
        var finalAllocatedBytes = GC.GetTotalAllocatedBytes(false);
        
        return new PerformanceMetrics
        {
            Name = _name,
            ExecutionTime = _stopwatch.Elapsed,
            MemoryAllocated = finalMemory - _initialMemory,
            TotalAllocatedBytes = finalAllocatedBytes - _initialAllocatedBytes,
            Gen0Collections = finalGen0 - _initialGen0,
            Gen1Collections = finalGen1 - _initialGen1,
            Gen2Collections = finalGen2 - _initialGen2,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a scoped performance tracker that automatically completes when disposed
    /// </summary>
    public static ScopedPerformanceTracker CreateScoped(string name, Action<PerformanceMetrics>? onComplete = null)
    {
        return new ScopedPerformanceTracker(name, onComplete);
    }
}

/// <summary>
/// Scoped performance tracker that automatically completes when disposed
/// </summary>
public class ScopedPerformanceTracker : IDisposable
{
    private readonly EnhancedPerformanceTracker _tracker;
    private readonly Action<PerformanceMetrics>? _onComplete;
    private bool _disposed;

    internal ScopedPerformanceTracker(string name, Action<PerformanceMetrics>? onComplete)
    {
        _tracker = new EnhancedPerformanceTracker(name);
        _onComplete = onComplete;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            var metrics = _tracker.Complete();
            _onComplete?.Invoke(metrics);
            _disposed = true;
        }
    }
}

/// <summary>
/// Comprehensive performance metrics including memory and GC information
/// </summary>
public class PerformanceMetrics
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan ExecutionTime { get; set; }
    public long MemoryAllocated { get; set; }
    public long TotalAllocatedBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the execution time in milliseconds
    /// </summary>
    public double ExecutionTimeMs => ExecutionTime.TotalMilliseconds;

    /// <summary>
    /// Gets memory allocated in KB
    /// </summary>
    public double MemoryAllocatedKB => MemoryAllocated / 1024.0;

    /// <summary>
    /// Gets total allocated bytes in MB
    /// </summary>
    public double TotalAllocatedMB => TotalAllocatedBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets total GC collections across all generations
    /// </summary>
    public int TotalGCCollections => Gen0Collections + Gen1Collections + Gen2Collections;

    public override string ToString()
    {
        return $"{Name}: {ExecutionTimeMs:F2}ms, {MemoryAllocatedKB:F1}KB allocated, {TotalGCCollections} GC collections";
    }

    /// <summary>
    /// Generates a detailed performance report
    /// </summary>
    public string ToDetailedString()
    {
        return $"""
            Performance Report for {Name}
            ═══════════════════════════════════════
            Execution Time: {ExecutionTimeMs:F2}ms
            Memory Allocated: {MemoryAllocatedKB:F1}KB ({MemoryAllocated:N0} bytes)
            Total Allocated: {TotalAllocatedMB:F2}MB ({TotalAllocatedBytes:N0} bytes)
            GC Collections:
              - Gen0: {Gen0Collections}
              - Gen1: {Gen1Collections}
              - Gen2: {Gen2Collections}
              - Total: {TotalGCCollections}
            Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff} UTC
            """;
    }
}