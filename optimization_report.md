# Musoq Performance Optimization Report

## Executive Summary

After analyzing the Musoq query engine codebase, I've identified several critical optimization opportunities that could dramatically improve query execution performance. The analysis focused on code generation patterns, runtime data structures, hot paths, and existing benchmark data.

**Key findings:**
- Hash join optimization already shows **50-150x improvement** over nested loops (per benchmarks)
- Major opportunities exist in regex compilation, memory allocation, and data access patterns
- Current architecture has good foundations but runtime overhead could be reduced significantly

---

## Table of Contents

1. [Critical Optimizations (High Impact)](#1-critical-optimizations-high-impact)
2. [Structural Optimizations (Medium Impact)](#2-structural-optimizations-medium-impact)
3. [Code Generation Improvements](#3-code-generation-improvements)
4. [Memory and Allocation Optimizations](#4-memory-and-allocation-optimizations)
5. [Parallelization Improvements](#5-parallelization-improvements)
6. [Implementation Roadmap](#6-implementation-roadmap)

---

## 1. Critical Optimizations (High Impact)

### 1.1 Regex Compilation for LIKE/RLIKE Operators ⚠️ CRITICAL

**Current State:**
```csharp
// Musoq.Evaluator/Operators.cs - Line 9-18
public bool Like(string content, string searchFor)
{
    return new Regex(@"\A" + new Regex(@"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\")
        .Replace(searchFor, ch => @"\" + ch).Replace('_', '.')
        .Replace("%", ".*") + @"\z", RegexOptions.Singleline).IsMatch(content);
}

public bool RLike(string content, string pattern)
{
    return new Regex(pattern).IsMatch(content);
}
```

**Problem:** Creates a new `Regex` object for **every single row**. Regex compilation is expensive.

**Solution:**
```csharp
public class Operators
{
    // Cache compiled regexes per pattern
    private static readonly ConcurrentDictionary<string, Regex> _likeCache = new();
    private static readonly ConcurrentDictionary<string, Regex> _rlikeCache = new();
    
    public bool Like(string content, string searchFor)
    {
        var regex = _likeCache.GetOrAdd(searchFor, pattern =>
        {
            var escaped = Regex.Replace(pattern, @"\.|\$|\^|\{|\[|\(|\||\)|\*|\+|\?|\\", m => @"\" + m.Value);
            var sqlPattern = escaped.Replace('_', '.').Replace("%", ".*");
            return new Regex(@"\A" + sqlPattern + @"\z", 
                RegexOptions.Singleline | RegexOptions.Compiled);
        });
        return regex.IsMatch(content);
    }
    
    public bool RLike(string content, string pattern)
    {
        var regex = _rlikeCache.GetOrAdd(pattern, p => 
            new Regex(p, RegexOptions.Compiled));
        return regex.IsMatch(content);
    }
}
```

**Even better - generate pattern at compile time:**
When the LIKE pattern is a constant (e.g., `WHERE email LIKE '%.co.uk'`), generate the compiled regex as a static field in the generated code:

```csharp
// Generated code should include:
private static readonly Regex _likePattern_0 = new Regex(@"\A.*\.co\.uk\z", 
    RegexOptions.Singleline | RegexOptions.Compiled);

// Then in the loop:
if (_likePattern_0.IsMatch(emailValue)) { ... }
```

**Expected Improvement:** 10-100x faster for queries with LIKE/RLIKE, depending on pattern complexity.

---

### 1.2 Eliminate Dictionary Lookups in Hot Paths

**Current State:**
```csharp
// Musoq.Evaluator/Tables/RowResolver.cs
object IObjectResolver.this[string name]
{
    get
    {
        if (!nameToIndexMap.TryGetValue(name, out var value))
            throw new Exception($"Column with name {name} does not exist in the row.");
        return row[value];
    }
}
```

**Problem:** Every column access by name requires a dictionary lookup in the hot loop.

**Solution - Generate direct index access at compile time:**

When generating code, resolve column names to indices during compilation:
```csharp
// Instead of generating:
var city = (string)row["City"];  // Dictionary lookup

// Generate:
var city = (string)row[0];  // Direct array access
```

The `ToCSharpRewriteTreeVisitor` should resolve column indices during code generation when the schema is known.

**Expected Improvement:** 2-5x faster column access.

---

### 1.3 Avoid Boxing for Value Types

**Current State:**
```csharp
// Musoq.Evaluator/Tables/ObjectsRow.cs
public override object this[int columnNumber] => _values[columnNumber];

// And in generated code:
var select = new object[] { value1, value2, value3 };
```

**Problem:** All values are boxed as `object[]`, causing GC pressure for value types.

**Solution - Generate strongly-typed row classes:**

The codebase already has `GenerateRowClass` in `ToCSharpRewriteTreeVisitor.cs`. Enhance it to:

```csharp
// Generated strongly-typed row:
public class Query1Row
{
    public string City;      // No boxing
    public int Population;   // No boxing
    public decimal Revenue;  // No boxing
    
    // Implement IReadOnlyRow with direct field access
    public object this[int index] => index switch
    {
        0 => City,
        1 => Population,  // Only boxes when accessed through indexer
        2 => Revenue,
        _ => throw new IndexOutOfRangeException()
    };
}
```

**Expected Improvement:** 20-50% reduction in GC pressure for queries with many value-type columns.

---

## 2. Structural Optimizations (Medium Impact)

### 2.1 Replace Lock-Based Table with Lock-Free Collection

**Current State:**
```csharp
// Musoq.Evaluator/Tables/Table.cs
public void Add(Row value)
{
    lock (_guard)  // Contention point in parallel execution
    {
        // validation and add
        Rows.Add(value);
    }
}
```

**Problem:** Lock contention kills parallel performance.

**Solution:**
```csharp
public class Table : IndexedList<Key, Row>, IEnumerable<Row>
{
    private readonly ConcurrentBag<Row> _pendingRows = new();
    private volatile bool _finalized = false;
    
    public void Add(Row value)
    {
        if (_finalized) throw new InvalidOperationException();
        _pendingRows.Add(value);  // Lock-free
    }
    
    public void Finalize()
    {
        _finalized = true;
        foreach (var row in _pendingRows)
            Rows.Add(row);  // Single-threaded finalization
    }
}
```

Or use `ConcurrentQueue<Row>` for ordered insertion.

**Expected Improvement:** 2-10x for parallel queries, eliminating lock contention.

---

### 2.2 Streaming/Lazy Evaluation for Non-Aggregating Queries

**Current State:** Results are materialized into a `Table` before returning.

**Solution:** For queries without GROUP BY, ORDER BY, or set operations:
```csharp
public IEnumerable<Row> RunStreaming(CancellationToken token)
{
    foreach (var row in source.Rows)
    {
        token.ThrowIfCancellationRequested();
        if (whereCondition(row))
            yield return TransformRow(row);
    }
}
```

This enables:
- Pipeline parallelism
- Early termination with LIMIT
- Reduced memory for large result sets

**Expected Improvement:** Memory usage from O(n) to O(1) for streaming cases.

---

### 2.3 Span<T> for String Operations

**Current State:** String operations create new allocations.

**Solution:** Use `ReadOnlySpan<char>` and `Span<T>` where applicable:
```csharp
// For substring comparisons in WHERE clauses
public bool StartsWith(ReadOnlySpan<char> content, ReadOnlySpan<char> prefix)
{
    return content.StartsWith(prefix, StringComparison.Ordinal);
}
```

**Expected Improvement:** 20-40% faster string operations, reduced GC pressure.

---

## 3. Code Generation Improvements

### 3.1 Inline Small Methods

**Current State:** Generated code calls helper methods for operations:
```csharp
// Generated:
Operators.Like(content, pattern)
```

**Solution:** For simple operations, inline the code:
```csharp
// Generated (after regex optimization):
_compiledPattern.IsMatch(content)
```

### 3.2 Generate SIMD-Vectorized Comparisons

For array operations like `IN (...)`:
```csharp
// Instead of:
public bool Contains<T>(T value, T[] values) => values.Contains(value);

// Generate vectorized search for numeric types:
public bool ContainsInt(int value, ReadOnlySpan<int> values)
{
    return Vector128.EqualsAny(
        Vector128.Create(value), 
        Vector128.LoadUnsafe(ref MemoryMarshal.GetReference(values)));
}
```

### 3.3 Constant Folding

**Current State:** Arithmetic on constants is computed at runtime.

**Solution:** Detect and evaluate constant expressions during code generation:
```sql
SELECT value * 2 + 10 FROM ...
-- When 'value' is constant 5, generate: SELECT 20 FROM ...
```

### 3.4 Predicate Push-Down at Code Level

For expressions like `WHERE a > 5 AND b < 10`:
```csharp
// Generate separate early-exit checks:
if (!(a > 5)) continue;  // Fail fast on first condition
if (!(b < 10)) continue; // Then second
```

Short-circuit evaluation ordering by selectivity when statistics are available.

---

## 4. Memory and Allocation Optimizations

### 4.1 Object Pooling for Rows

```csharp
public class RowPool
{
    private readonly ObjectPool<ObjectsRow> _pool = 
        new DefaultObjectPool<ObjectsRow>(new RowPolicy());
    
    public ObjectsRow Rent(int columnCount) => _pool.Get();
    public void Return(ObjectsRow row) => _pool.Return(row);
}
```

### 4.2 ArrayPool for Temporary Arrays

```csharp
// Instead of:
var select = new object[] { v1, v2, v3 };

// Use:
var select = ArrayPool<object>.Shared.Rent(3);
try { /* use array */ }
finally { ArrayPool<object>.Shared.Return(select); }
```

### 4.3 Pre-allocate Result Table

When query has `LIMIT N`, pre-size the result table:
```csharp
var result = new Table(name, columns, capacity: limitValue);
```

### 4.4 Struct-Based Intermediate Results

For hash join keys:
```csharp
// Instead of Tuple or anonymous types (heap allocation)
public readonly struct JoinKey : IEquatable<JoinKey>
{
    public readonly int Key1;
    public readonly string Key2;
    // Stack allocated for single-key joins
}
```

---

## 5. Parallelization Improvements

### 5.1 Partition-Aware Parallel Processing

**Current State:** Uses `Parallel.ForEach` with default partitioning.

**Solution:**
```csharp
// For known source sizes, use range partitioning
Parallel.ForEach(
    Partitioner.Create(0, rowCount, rowCount / Environment.ProcessorCount),
    range => {
        for (int i = range.Item1; i < range.Item2; i++)
            ProcessRow(rows[i]);
    });
```

### 5.2 Parallel Hash Join Build Phase

The hash join already exists but ensure the build phase uses parallel insertion:
```csharp
var hashTable = new ConcurrentDictionary<TKey, List<IObjectResolver>>();
Parallel.ForEach(buildRows, row =>
{
    var key = GetKey(row);
    hashTable.AddOrUpdate(key, 
        _ => new List<IObjectResolver> { row },
        (_, list) => { lock(list) list.Add(row); return list; });
});
```

### 5.3 Parallel Aggregation with Local State

```csharp
// Each thread has local accumulators
Parallel.ForEach(source.Rows,
    () => new LocalAggregates(),  // Thread-local state
    (row, state, local) => {
        local.Sum += GetValue(row);
        local.Count++;
        return local;
    },
    local => {
        // Merge into global result (lock-free with Interlocked)
        Interlocked.Add(ref globalSum, local.Sum);
        Interlocked.Increment(ref globalCount);
    });
```

---

## 6. Implementation Roadmap

### Phase 1: Quick Wins (1-2 weeks)
| Priority | Optimization | Effort | Impact |
|----------|-------------|--------|--------|
| P0 | Regex caching in Operators.cs | 2 hours | Very High |
| P0 | Compile-time regex for constant patterns | 1 day | Very High |
| P1 | Pre-resolve column indices | 2 days | High |
| P1 | Replace lock with ConcurrentBag | 1 day | High |

### Phase 2: Structural Changes (2-4 weeks)
| Priority | Optimization | Effort | Impact |
|----------|-------------|--------|--------|
| P1 | Strongly-typed generated row classes | 3 days | High |
| P2 | Streaming evaluation mode | 1 week | Medium |
| P2 | Object pooling for rows | 3 days | Medium |

### Phase 3: Advanced Optimizations (1-2 months)
| Priority | Optimization | Effort | Impact |
|----------|-------------|--------|--------|
| P2 | SIMD vectorized operations | 1 week | Medium |
| P3 | Span-based string operations | 1 week | Medium |
| P3 | Parallel aggregation | 2 weeks | Medium |

---

## Benchmark Baseline Reference

From existing benchmarks in `BenchmarkDotNet.Artifacts/results/`:

### Hash Join Impact (already implemented)
| Rows | Without Hash Join | With Hash Join | Speedup |
|------|------------------|----------------|---------|
| 2000 | 100.5 ms | 2.0 ms | **50x** |
| 5000 | 632.7 ms | 4.2 ms | **150x** |

### Memory Improvement (Hash Join)
| Rows | Without Hash Join | With Hash Join | Reduction |
|------|------------------|----------------|-----------|
| 2000 | 338 MB | 2.14 MB | **158x** |
| 5000 | 2104 MB | 4.82 MB | **436x** |

These numbers demonstrate the impact that algorithmic improvements can have. The optimizations proposed here could achieve similar multiplicative improvements in their respective areas.

---

## Conclusion

The Musoq query engine has solid foundations with intelligent code generation and existing optimizations like hash joins. The most impactful improvements would be:

1. **Regex caching** - Easy fix with massive impact for LIKE/RLIKE queries
2. **Compile-time index resolution** - Eliminates runtime dictionary lookups
3. **Lock-free concurrent collections** - Enables true parallel scaling
4. **Streaming evaluation** - Reduces memory for large result sets

Implementing Phase 1 optimizations alone could provide 5-20x improvements for common query patterns, with minimal risk to existing functionality.

---

*Report generated: 2025-11-25*
*Based on analysis of: Musoq codebase, feature/optimizations_v2 branch*
