# Musoq Join Optimization Report

## 1. Current State Analysis

### Generated Code Strategy
The current execution engine generates C# code that implements a **Nested Loop Join**.

```csharp
// Simplified logic of the current generated code
foreach (var outerRow in outerSource) {
    foreach (var innerRow in innerSource) {
        if (outerRow.Key == innerRow.Key) {
            // Match found
        }
    }
}
```

### Performance Benchmark
**Scenario**: Joining 3 tables (A, B, C) with 100 rows each.
`SELECT * FROM A JOIN B on A.Id = B.Id JOIN C on B.Id = C.Id`

| Metric | Result | Notes |
| :--- | :--- | :--- |
| **Execution Time** | **1.140 ms** | For only 100 rows. Complexity is Cubic $O(N^3)$ for 3 joins. |
| **Memory Allocated** | **2.24 MB** | High allocation due to intermediate table materialization. |

### Identified Bottlenecks
1.  **Algorithmic Complexity**: Nested Loop Join is $O(N \times M)$. For large datasets, this is inefficient.
2.  **Memory Pressure**: Intermediate results (e.g., result of A join B) are fully materialized into a `Table` object before the next join starts.
3.  **Boxing/Unboxing**: Values are accessed as `object` and cast (e.g., `(System.Int32)(row["Id"])`), causing CPU overhead and memory traffic.

## 2. Optimization Plan

We will address these issues in the following order to maximize impact.

### Step 1: Implement Hash Join (Completed)
**Goal**: Change complexity from $O(N \times M)$ to $O(N + M)$.
**Strategy**:
*   Build a `Dictionary<Key, List<Row>>` from the inner (smaller) relation.
*   Stream the outer relation and look up matches in the dictionary.
**Status**: Implemented and verified with tests.
**Benchmarks**:
*   **1000 Rows Join**:
    *   Nested Loop: **25.02 ms**
    *   Hash Join: **1.16 ms**
    *   **Speedup: ~21.6x**
*   **100 Rows Join (3 tables)**:
    *   Nested Loop: **1.17 ms**
    *   Hash Join: **0.87 ms**
    *   **Speedup: ~1.3x** (Overhead dominates for small N)
**Heuristics**: Hash Join is now enabled by default for all Inner Joins where equality keys can be extracted. It can be explicitly disabled via `CompilationOptions(useHashJoin: false)`.

### Step 2: Implement Pipelining (Lazy Evaluation)
**Goal**: Reduce memory usage and latency.
**Strategy**:
*   Replace `Table` materialization with `IEnumerable<Row>` (iterators).
*   The second join should start processing as soon as the first join yields a row.
**Expected Impact**: Constant memory usage relative to the stream size (excluding the Hash Table overhead), instead of linear/polynomial memory usage.

### Step 3: Reduce Boxing/Unboxing
**Goal**: Micro-optimization for CPU and memory.
**Strategy**:
*   Use generic types or strongly typed accessors where possible.
*   Avoid creating `object[]` arrays for every row if not strictly necessary.
**Expected Impact**: Reduced GC pressure and faster row processing.
