# Current Progress on Aggregate Window Functions

## Issue Analysis

### Fixed Issues ✅
1. **"Mixing aggregate and non aggregate methods is not implemented yet"** 
   - Fixed by allowing mixed aggregate/non-aggregate fields in RewriteQueryVisitor
   - Window functions can now coexist with regular columns without GROUP BY

### Current Issue 🔧
**"Window function AVG with argument types [Decimal] cannot be resolved"**

**Root Cause**: Method resolution for generic aggregate window functions is failing.

**Details**:
- Query: `select Population, AVG(Population) OVER (PARTITION BY Country ORDER BY Population) as AvgByCountry`
- Expected method: `public double Avg<T>(T value, [InjectQueryStats] QueryStats info) where T : struct`
- Problem: Method resolution can't find `Avg` method for Decimal argument type
- Population column type: Decimal
- Expected call: `Avg<decimal>(populationValue, queryStats)`

### Working vs Failing Functions

**✅ Working (Basic Window Functions)**:
- `Rank()`: `public int Rank([InjectQueryStats] QueryStats info)` - 0 arguments
- `DenseRank()`: `public int DenseRank([InjectQueryStats] QueryStats info)` - 0 arguments
- `Lag()`, `Lead()`: Take value arguments but working properly

**❌ Failing (Aggregate Window Functions)**:
- `Avg()`: `public double Avg<T>(T value, [InjectQueryStats] QueryStats info)` - 1 generic argument
- `Sum()`, `Count()`: Similar generic signatures

### Method Resolution Process
1. BuildMetadataAndInferTypesVisitor.Visit(WindowFunctionNode)
2. TryResolveMethod() for "AVG" with [Decimal] argument types
3. Schema can't find matching method (generic type resolution issue)
4. TryResolveRawMethod() also fails
5. Exception thrown

### Next Steps
1. Debug method resolution for generic methods in schema
2. Check if TryResolveMethod handles generic type matching correctly
3. Potentially modify argument type inference for window functions
4. Test with other aggregate window functions (SUM, COUNT)

### Test Status
- Basic Window Functions: 9/9 passing ✅
- Aggregate Window Functions: 0/4 passing ❌
- Total: 9/13 passing (69%)

The core infrastructure is working, just need to fix generic method resolution.