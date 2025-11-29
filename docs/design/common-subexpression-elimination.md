# Common Subexpression Elimination (CSE) Design Document

## Overview

Common Subexpression Elimination is a compiler optimization that identifies expressions computed multiple times and replaces subsequent occurrences with a cached result from the first computation.

### Problem Statement

In Musoq, users often write queries like:

```sql
SELECT SomeExpensiveMethod(Column1, Column2)
FROM #source.data()
WHERE SomeExpensiveMethod(Column1, Column2) > 100
```

Currently, `SomeExpensiveMethod(Column1, Column2)` is computed **twice per row**:
1. Once in the WHERE clause for filtering
2. Once in the SELECT clause for the result

For expensive computations (database lookups, complex calculations, string manipulations), this doubles the execution time unnecessarily.

### Goals

1. **Identify duplicate expressions** across query clauses (SELECT, WHERE, HAVING, ORDER BY)
2. **Compute each unique expression once** per row
3. **Cache and reuse** the computed value
4. **Maintain correctness** - only cache pure/deterministic expressions
5. **Zero overhead** for queries without duplicate expressions

### Non-Goals

1. Cross-row caching (memoization) - out of scope
2. Caching across different queries - out of scope
3. Automatic parallelization of cached expression computation - handled separately

---

## Architecture

### Current Pipeline

```
SQL Query
    ↓
[Parser] → AST (RawQueryTree)
    ↓
[ExtractRawColumnsVisitor] → Column references
    ↓
[BuildMetadataAndInferTypesVisitor] → Typed AST
    ↓
[RewriteQueryVisitor] → Normalized AST
    ↓
[ToCSharpRewriteTreeVisitor] → C# Syntax Tree
    ↓
[Roslyn Compiler] → Executable Assembly
```

### Proposed Pipeline (with CSE)

```
SQL Query
    ↓
[Parser] → AST (RawQueryTree)
    ↓
[ExtractRawColumnsVisitor] → Column references
    ↓
[BuildMetadataAndInferTypesVisitor] → Typed AST
    ↓
[RewriteQueryVisitor] → Normalized AST
    ↓
[CommonSubexpressionAnalysisVisitor] → Expression frequency map    ← NEW
    ↓
[CommonSubexpressionRewriteVisitor] → AST with cached expressions  ← NEW
    ↓
[ToCSharpRewriteTreeVisitor] → C# Syntax Tree (with caching)
    ↓
[Roslyn Compiler] → Executable Assembly
```

---

## Design Details

### 1. Expression Identity

Musoq AST nodes already have an `Id` property that provides structural identity:

```csharp
// AccessMethodNode.cs
Id = $"{nameof(AccessMethodNode)}{alias}{functionToken.Value}{args.Id}";

// AccessColumnNode.cs  
Id = $"{nameof(AccessColumnNode)}{name}{alias}{span.Start}{span.End}";
```

**Key insight**: Two expressions with the same `Id` are structurally identical.

### 2. Expression Classification

Not all expressions can be cached. We classify expressions into:

| Category | Cacheable | Examples |
|----------|-----------|----------|
| **Pure functions** | ✅ Yes | `Length(str)`, `Abs(x)`, `Substring(s,1,5)` |
| **Column access** | ✅ Yes | `column.Name`, `a.Value` |
| **Literals** | ❌ No (trivial) | `42`, `'hello'`, `true` |
| **Aggregates** | ❌ No | `Count()`, `Sum(x)`, `Avg(x)` |
| **Non-deterministic** | ❌ No | `NewId()`, `Random()`, `Now()` |
| **Side-effecting** | ❌ No | Functions marked with `[SideEffect]` |

### 3. New Node Types

#### CachedExpressionNode

A new AST node that wraps a cacheable expression:

```csharp
public class CachedExpressionNode : Node
{
    public string CacheKey { get; }           // Unique identifier for this cache slot
    public Node OriginalExpression { get; }   // The expression to compute and cache
    public bool IsFirstOccurrence { get; }    // True = compute & store, False = just load
    
    public override string Id => $"{nameof(CachedExpressionNode)}{CacheKey}";
    public override Type ReturnType => OriginalExpression.ReturnType;
}
```

#### CachedExpressionReferenceNode

A lightweight node that references a previously cached value:

```csharp
public class CachedExpressionReferenceNode : Node
{
    public string CacheKey { get; }           // References the cache slot
    public Type CachedType { get; }           // Type of the cached value
    
    public override string Id => $"{nameof(CachedExpressionReferenceNode)}{CacheKey}";
    public override Type ReturnType => CachedType;
}
```

### 4. Analysis Phase

The `CommonSubexpressionAnalysisVisitor` performs a single pass to:

1. **Collect all expressions** with their `Node.Id`
2. **Count occurrences** of each expression
3. **Determine cacheability** based on expression type
4. **Build a frequency map**: `Dictionary<string, ExpressionInfo>`

```csharp
public class ExpressionInfo
{
    public Node FirstOccurrence { get; set; }
    public int Count { get; set; }
    public bool IsCacheable { get; set; }
    public QueryPart FirstSeenIn { get; set; }  // WHERE, SELECT, HAVING, ORDER BY
}

public enum QueryPart
{
    Where,
    Select,
    Having,
    OrderBy,
    GroupBy
}
```

### 5. Rewrite Phase

The `CommonSubexpressionRewriteVisitor` transforms the AST:

1. **For expressions with Count > 1 and IsCacheable = true**:
   - First occurrence → `CachedExpressionNode` (compute + store)
   - Subsequent occurrences → `CachedExpressionReferenceNode` (load)

2. **Expression evaluation order**:
   - WHERE is evaluated first → cache slots populated here
   - SELECT evaluated second → cache slots reused here
   - HAVING evaluated after GROUP BY
   - ORDER BY evaluated last

### 6. Code Generation

The `ToCSharpRewriteTreeVisitor` generates code for cached expressions:

#### Cache Storage

```csharp
// Generated at method level
var _exprCache = new object[{cacheSlotCount}];
```

#### CachedExpressionNode → Compute and Store

```csharp
// For: CachedExpressionNode { CacheKey = "expr_0", Expression = SomeMethod(x) }
_exprCache[0] = SomeMethod(x);
var expr_0_result = (ReturnType)_exprCache[0];
```

#### CachedExpressionReferenceNode → Load

```csharp
// For: CachedExpressionReferenceNode { CacheKey = "expr_0" }
var expr_0_result = (ReturnType)_exprCache[0];
```

### 7. Evaluation Order Guarantee

The current execution model guarantees:

```
foreach (row in source) {
    // 1. WHERE clause evaluated
    if (!whereCondition) continue;
    
    // 2. SELECT clause evaluated  
    result.Add(selectValues);
}
```

This means WHERE expressions are **always evaluated before** SELECT for the same row, making caching safe.

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `CachedExpressionNode` and `CachedExpressionReferenceNode` in `Musoq.Parser`
2. Create `ExpressionCacheabilityChecker` utility class
3. Create `CommonSubexpressionAnalysisVisitor` and traverser

### Phase 2: AST Rewriting

4. Create `CommonSubexpressionRewriteVisitor` and traverser
5. Integrate into `TransformTree.cs` pipeline

### Phase 3: Code Generation

6. Add `Visit(CachedExpressionNode)` to `ToCSharpRewriteTreeVisitor`
7. Add `Visit(CachedExpressionReferenceNode)` to `ToCSharpRewriteTreeVisitor`
8. Generate cache array at method level

### Phase 4: Testing & Optimization

9. Unit tests for all visitor classes
10. Integration tests for end-to-end query execution
11. Benchmark tests comparing with/without CSE
12. Edge case handling (nulls, exceptions, type conversions)

---

## Test Cases

### Basic Functionality

```sql
-- TC1: Same method in WHERE and SELECT
SELECT Expensive(x) FROM source WHERE Expensive(x) > 10

-- TC2: Same column access multiple times
SELECT a.Name, Length(a.Name), Upper(a.Name) FROM source a

-- TC3: Nested expressions
SELECT Outer(Inner(x)) FROM source WHERE Outer(Inner(x)) > 0

-- TC4: Same expression in ORDER BY
SELECT x, Compute(x) FROM source ORDER BY Compute(x)
```

### Edge Cases

```sql
-- TC5: Aggregate functions should NOT be cached
SELECT Sum(x), Sum(x) FROM source  -- Each Sum() is independent

-- TC6: Different aliases, same expression
SELECT a.x + b.x FROM source a, source b WHERE a.x + b.x > 0

-- TC7: Expression in HAVING
SELECT Category, Count(*) FROM source 
GROUP BY Category 
HAVING Count(*) > 5

-- TC8: No duplicates (baseline - no caching)
SELECT a, b, c FROM source WHERE x > 10
```

### Non-Cacheable Expressions

```sql
-- TC9: Non-deterministic functions
SELECT NewGuid(), NewGuid() FROM source  -- Must be different!

-- TC10: Functions with side effects
SELECT Log(x), Log(x) FROM source  -- If Log has side effects
```

---

## Performance Considerations

### Memory Overhead

- **Cache array**: `object[n]` where n = number of cached expressions (typically 1-5)
- **Negligible**: A few dozen bytes per row iteration

### CPU Overhead

- **Analysis pass**: O(n) where n = AST node count
- **Rewrite pass**: O(n) where n = AST node count
- **Runtime**: One array store + one array load per cached expression

### Expected Gains

| Scenario | Without CSE | With CSE | Speedup |
|----------|-------------|----------|---------|
| Simple column access | ~1μs | ~1μs | 1x (no gain, trivial) |
| String operation (e.g., Regex) | ~100μs × 2 | ~100μs × 1 | ~2x |
| Complex computation | ~1ms × 2 | ~1ms × 1 | ~2x |
| Database/IO operation | ~10ms × 2 | ~10ms × 1 | ~2x |

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Incorrect caching of non-deterministic functions | High - Wrong results | Explicit allow-list of cacheable function patterns |
| Exception handling in cached expressions | Medium | Cache the exception, rethrow on reference |
| Null handling | Medium | Null is a valid cached value |
| Type variance | Low | Cache as `object`, cast on retrieval |
| Compilation time increase | Low | Minimal - two linear passes |

---

## Future Enhancements

1. **Cross-row memoization**: Cache results based on input values
2. **Lazy evaluation**: Compute cached expressions only when needed
3. **Parallel cache population**: Pre-compute cache slots in parallel
4. **User hints**: `/*+ CACHE */` and `/*+ NO_CACHE */` hints

---

## Appendix: Example Transformation

### Input Query

```sql
SELECT Expensive(a.Value), a.Name
FROM #source.data() a
WHERE Expensive(a.Value) > 100
ORDER BY Expensive(a.Value) DESC
```

### After CSE Analysis

```
Expression: Expensive(a.Value)
  - Id: "AccessMethodNodeExpensivea.Value"
  - Occurrences: 3 (WHERE, SELECT, ORDER BY)
  - Cacheable: true
  - Cache slot: 0
```

### Transformed AST (conceptual)

```sql
SELECT CachedRef("expr_0"), a.Name
FROM #source.data() a
WHERE Cache("expr_0", Expensive(a.Value)) > 100
ORDER BY CachedRef("expr_0") DESC
```

### Generated C# (simplified)

```csharp
var _exprCache = new object[1];

foreach (var row in source.Rows)
{
    // WHERE - first occurrence, compute and cache
    _exprCache[0] = Expensive((int)row["Value"]);
    var expr_0 = (int)_exprCache[0];
    
    if (!(expr_0 > 100)) continue;
    
    // SELECT - reference cached value
    var select_0 = (int)_exprCache[0];
    var select_1 = (string)row["Name"];
    
    result.Add(new Row(select_0, select_1));
}

// ORDER BY - reference cached value (stored in row)
result.OrderByDescending(r => r[0]);
```

---

## References

- [Wikipedia: Common Subexpression Elimination](https://en.wikipedia.org/wiki/Common_subexpression_elimination)
- [Dragon Book: Compilers - Principles, Techniques, and Tools](https://en.wikipedia.org/wiki/Compilers:_Principles,_Techniques,_and_Tools)
- Musoq Architecture: `ARCHITECTURE.md`
- Musoq Visitor Pattern: `Musoq.Evaluator/Visitors/`
