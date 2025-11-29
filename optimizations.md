# Musoq Performance Optimizations

This document tracks performance optimizations made to the Musoq query engine with benchmark results.

## Optimization 1: Regex Caching for LIKE/RLIKE Operators

### Problem
The `Like` and `RLike` operators in `Operators.cs` were creating new `Regex` objects for every row processed. This caused:
- Massive CPU overhead from regex compilation
- Excessive memory allocations and GC pressure
- Linear performance degradation with data size

### Solution
Implemented `ConcurrentDictionary`-based caching for compiled regex patterns:

```csharp
private static readonly ConcurrentDictionary<string, Regex> LikePatternCache = new();
private static readonly ConcurrentDictionary<string, Regex> RLikePatternCache = new();
private static readonly Regex EscapePattern = new(@"[.*+?^${}()|[\]\\]", RegexOptions.Compiled);
```

Patterns are now cached with `RegexOptions.Compiled` flag for optimal matching performance.

### Benchmark Results

**Environment:**
- BenchmarkDotNet v0.15.4
- Windows 11 (10.0.26200.7171)
- Intel Core Ultra 9 285K 3.70GHz
- .NET 8.0.13

#### Pre-Optimization (Baseline)

| Method | Mean | Allocated | Notes |
|--------|------|-----------|-------|
| Baseline_EqualityFilter_1000Rows | 228.6 us | 286.52 KB | Reference baseline |
| Like_SinglePattern_1000Rows | **2,955.1 us** | **9,485.62 KB** | 12.93x slower, 33x more memory |
| Like_SinglePattern_10000Rows | **27,050.0 us** | **87,903.64 KB** | 118.3x slower |
| RLike_Pattern_1000Rows | **1,066.1 us** | **3,243.57 KB** | 4.66x slower |
| Like_MultiplePatterns_1000Rows | **6,528.9 us** | **21,691.02 KB** | 28.6x slower |

#### Post-Optimization

| Method | Mean | Allocated | Notes |
|--------|------|-----------|-------|
| Baseline_EqualityFilter_1000Rows | 341.3 us | 286.52 KB | Reference baseline |
| Like_SinglePattern_1000Rows | **314.8 us** | **199.45 KB** | 0.92x (8% faster than baseline!) |
| Like_SinglePattern_10000Rows | **995.8 us** | **569.21 KB** | 2.92x baseline (vs 118x before) |
| RLike_Pattern_1000Rows | **221.9 us** | **199.39 KB** | 0.65x (35% faster than baseline!) |
| Like_MultiplePatterns_1000Rows | **318.7 us** | **318.61 KB** | 0.93x (7% faster than baseline!) |

### Performance Improvement Summary

| Scenario | Before | After | Speedup | Memory Reduction |
|----------|--------|-------|---------|------------------|
| LIKE (1K rows) | 2,955 us | 315 us | **9.4x faster** | **47.5x less** (9,486 KB → 199 KB) |
| LIKE (10K rows) | 27,050 us | 996 us | **27.2x faster** | **154x less** (87,904 KB → 569 KB) |
| RLIKE (1K rows) | 1,066 us | 222 us | **4.8x faster** | **16.3x less** (3,244 KB → 199 KB) |
| LIKE Multiple (1K rows) | 6,529 us | 319 us | **20.5x faster** | **68x less** (21,691 KB → 319 KB) |

### Key Observations

1. **LIKE operator is now faster than simple equality** for cached patterns (0.92x ratio)
2. **RLIKE is 35% faster than equality baseline** due to efficient compiled regex matching
3. **Memory usage reduced by 47-154x** depending on dataset size
4. **Pattern caching eliminates per-row regex compilation overhead**
5. **Multiple pattern queries benefit most** (20.5x speedup) as each pattern is compiled once

### Code Changes

**File:** `Musoq.Evaluator/Operators.cs`

```csharp
// Added static caches
private static readonly ConcurrentDictionary<string, Regex> LikePatternCache = new();
private static readonly ConcurrentDictionary<string, Regex> RLikePatternCache = new();
private static readonly Regex EscapePattern = new(@"[.*+?^${}()|[\]\\]", RegexOptions.Compiled);

// Like method now uses cache
public static bool Like(string content, string pattern)
{
    if (content == null || pattern == null)
        return false;

    var regex = LikePatternCache.GetOrAdd(pattern, p =>
    {
        var escapedPattern = EscapePattern.Replace(p, "\\$&");
        var regexPattern = "^" + escapedPattern.Replace("%", ".*").Replace("_", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    });

    return regex.IsMatch(content);
}

// RLike method now uses cache
public static bool RLike(string content, string pattern)
{
    if (content == null || pattern == null)
        return false;

    var regex = RLikePatternCache.GetOrAdd(pattern, p =>
        new Regex(p, RegexOptions.IgnoreCase | RegexOptions.Compiled));

    return regex.IsMatch(content);
}
```

---

## Optimization 2: String Contains Performance

### Problem
The `Contains` method in `LibraryBaseStrings.cs` was using `CultureInfo.CurrentCulture.CompareInfo.IndexOf` for case-insensitive string searching:

```csharp
return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
```

This culture-aware comparison is extremely slow compared to simple string operations, causing:
- **14.19x slower** than simple equality operations
- Unnecessary culture-aware overhead for most use cases
- Heavy CPU utilization for string matching in queries

### Solution
Replaced with direct `string.Contains` using `StringComparison.OrdinalIgnoreCase`:

```csharp
return content.Contains(what, StringComparison.OrdinalIgnoreCase);
```

This uses ordinal (byte-by-byte) comparison which is highly optimized in .NET.

### Benchmark Results

**Environment:**
- BenchmarkDotNet v0.15.4
- Windows 11 (10.0.26200.7171)
- Intel Core Ultra 9 285K 3.70GHz
- .NET 8.0.13

#### Pre-Optimization (Baseline)

| Method | Mean | Allocated | Ratio vs Baseline |
|--------|------|-----------|-------------------|
| Baseline_EqualityFilter_1000Rows | 1,174.0 us | 286.52 KB | 1.00x |
| Contains_StringSearch_1000Rows | **16,662.7 us** | **286.52 KB** | **14.19x slower** |
| StartsWith_StringSearch_1000Rows | 988.0 us | 286.52 KB | 0.84x |
| EndsWith_StringSearch_1000Rows | 1,029.7 us | 286.52 KB | 0.88x |
| Multiple_StringOperations_1000Rows | **16,435.0 us** | **286.56 KB** | **14.00x slower** |

#### Post-Optimization

| Method | Mean | Allocated | Ratio vs Baseline |
|--------|------|-----------|-------------------|
| Baseline_EqualityFilter_1000Rows | 1,200.9 us | 286.52 KB | 1.00x |
| Contains_StringSearch_1000Rows | **862.4 us** | **286.52 KB** | **0.72x (faster than baseline!)** |
| StartsWith_StringSearch_1000Rows | 1,048.2 us | 286.52 KB | 0.87x |
| EndsWith_StringSearch_1000Rows | 1,056.2 us | 286.52 KB | 0.88x |
| Multiple_StringOperations_1000Rows | **848.4 us** | **286.56 KB** | **0.71x (faster than baseline!)** |

### Performance Improvement Summary

| Scenario | Before | After | Speedup |
|----------|--------|-------|---------|
| Contains (1K rows) | 16,663 us | 862 us | **19.3x faster** |
| Multiple String Ops | 16,435 us | 848 us | **19.4x faster** |
| Ratio vs Baseline | 14.19x slower | 0.72x faster | **Now faster than equality!** |

### Key Observations

1. **Contains is now 28% faster than simple equality** (0.72x ratio vs 14.19x before)
2. **19.3x performance improvement** from a single-line change
3. **Zero memory impact** - same allocations as before
4. **All string operations (Contains, StartsWith, EndsWith) now faster than equality baseline**
5. **Culture-aware string comparison was the bottleneck** - ordinal comparison is highly optimized

### Code Changes

**File:** `Musoq.Plugins/Lib/LibraryBaseStrings.cs`

```csharp
// Before (slow):
public static bool Contains(string content, string what)
{
    if (content == null || what == null)
        return false;
    return CultureInfo.CurrentCulture.CompareInfo.IndexOf(content, what, CompareOptions.IgnoreCase) >= 0;
}

// After (fast):
public static bool Contains(string content, string what)
{
    if (content == null || what == null)
        return false;
    return content.Contains(what, StringComparison.OrdinalIgnoreCase);
}
```

### Impact Analysis

This optimization has a significant impact on any query using `Contains()`:

```sql
-- These queries are now 19.3x faster:
SELECT * FROM #data.source() WHERE Contains(Name, 'john')
SELECT * FROM #data.source() WHERE Name LIKE '%john%'  -- if using Contains internally
SELECT * FROM #data.source() WHERE Contains(Description, 'error') AND Contains(Log, 'warning')
```

---

## Optimization 3: Regex Plugin Caching (Minor Impact)

### Problem
The `Match`, `RegexReplace`, and `RegexMatches` methods in `LibraryBase` were using static `Regex` methods which have internal caching but not with `RegexOptions.Compiled`.

### Solution
Added explicit `ConcurrentDictionary`-based caching with `RegexOptions.Compiled` for all regex plugin methods.

### Benchmark Results
This optimization showed **minimal impact** because .NET's static `Regex` methods already have internal caching:

| Method | Pre-Opt Mean | Post-Opt Mean | Speedup | Memory Savings |
|--------|-------------|---------------|---------|----------------|
| Match | 545.6 us | 537.1 us | ~1.6% | 19% less |
| RegexReplace | 515.7 us | 526.2 us | (variance) | 5% less |
| RegexMatches | 579.4 us | 562.5 us | ~3% | ~0.2% less |

**Conclusion**: The caching was kept as it provides slight memory reduction and ensures consistent compiled regex usage, but the major performance wins come from LIKE/RLIKE and Contains optimizations.

---

## Optimization Summary

| Optimization | Impact | Speedup | Memory Reduction |
|--------------|--------|---------|------------------|
| **LIKE/RLIKE Regex Caching** | ⭐⭐⭐ CRITICAL | 9.4x-27.2x | 47x-154x |
| **Contains String Search** | ⭐⭐⭐ CRITICAL | 19.3x | (no change) |
| **Regex Plugin Caching** | ⭐ MINOR | ~1-3% | 5-19% |

**Total lines changed:** ~50 lines of code
**Total tests passing:** 2,476 tests

---

## Future Optimizations

### High Priority
1. **Compile-time index resolution** - Generate direct array index access instead of dictionary lookups for column access
2. **Row batch processing** - Process rows in batches to reduce virtual method call overhead
3. **Strongly-typed row classes** - Eliminate boxing for value types

### Medium Priority
1. **Pre-compiled operators** for common expression patterns
2. **Incremental compilation** to cache shared query components
3. **Lock-free Table collections** to reduce contention in parallel execution

### Under Consideration
1. **Expression tree optimization** to fuse filter/projection operations
2. **Memory pooling** for row/table allocations
3. **SIMD-accelerated operations** for bulk numeric/string processing
