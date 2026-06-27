# Musoq.Plugins

Built-in SQL functions library providing ~1000 methods for string manipulation, math, aggregation, date/time, cryptography, compression, JSON, networking, and more. All functions available in Musoq SQL queries are defined here.

## Internal Structure

```
Musoq.Plugins/
├── Lib/                                # Function implementations (~120 partial class files)
│   ├── LibraryBase.cs                  # Abstract base — all function libraries extend this
│   ├── LibraryBaseStrings.cs           # String functions: Substring, Replace, Trim, PadLeft, etc.
│   ├── LibraryBaseMath.cs             # Math functions: Abs, Round, Ceiling, Floor, Power, etc.
│   ├── LibraryBaseDate.cs             # Date functions: Year, Month, Day, DateDiff, etc.
│   ├── LibraryBaseDateTimeOffset.cs   # DateTimeOffset functions
│   ├── LibraryBaseTimeSpan.cs         # TimeSpan functions
│   ├── LibraryBaseJson.cs             # JSON parsing and access
│   ├── LibraryBaseCount.cs            # Count aggregation
│   ├── LibraryBaseSum.cs              # Sum aggregation
│   ├── LibraryBaseSumIncome.cs        # Sum income aggregation
│   ├── LibraryBaseSumOutcome.cs       # Sum outcome aggregation
│   ├── LibraryBaseMin.cs              # Min aggregation
│   ├── LibraryBaseMax.cs              # Max aggregation
│   ├── LibraryBaseAvg.cs              # Average aggregation
│   ├── LibraryBaseStDev.cs            # Standard deviation aggregation
│   ├── LibraryBaseDistinctAggregates.cs # Distinct aggregate variants
│   ├── LibraryBaseAggregateValues.cs  # Aggregate value collection
│   ├── LibraryBaseWindow.cs           # Window function support
│   ├── LibraryBaseWindowFunctions.cs  # Window function implementations
│   ├── LibraryBaseGeneric.cs          # Generic utility functions
│   ├── LibraryBaseConverting.cs       # Type conversion functions
│   ├── LibraryBaseConvertingFromBytes.cs  # Byte array → typed value conversions
│   ├── LibraryBaseConvertingToBytes.cs    # Typed value → byte array conversions
│   ├── LibraryBaseConvertingToHex.cs      # Hex string conversions
│   ├── LibraryBaseStrictConversions.cs    # Strict type conversions (throw on failure)
│   ├── LibraryBaseCrypto.cs           # Cryptographic functions (SHA, MD5, etc.)
│   ├── LibraryBaseHashes.cs           # Hash functions
│   ├── LibraryBaseCompression.cs      # Compression/decompression
│   ├── LibraryBaseBytes.cs            # Byte array operations
│   ├── LibraryBaseBitsOperations.cs   # Bitwise operations
│   ├── LibraryBaseDataUtils.cs        # Data utility functions
│   ├── LibraryBaseNetworkUtils.cs     # Network utility functions
│   ├── LibraryBaseValidation.cs       # Validation functions (IsNull, Coalesce, etc.)
│   ├── LibraryBaseDiff.cs             # Diff/comparison functions
│   ├── LibraryBaseToChar.cs           # ToChar conversions
│   ├── LibraryBaseToDateTime.cs       # ToDateTime conversions
│   ├── LibraryBaseToDecimal.cs        # ToDecimal conversions
│   ├── LibraryBaseToDouble.cs         # ToDouble conversions
│   ├── LibraryBaseToFloat.cs          # ToFloat conversions
│   ├── LibraryBaseToInt32.cs          # ToInt32 conversions
│   ├── LibraryBaseToInt64.cs          # ToInt64 conversions
│   ├── LibraryBaseToString.cs         # ToString conversions
│   ├── LibraryBaseToTimeSpan.cs       # ToTimeSpan conversions
│   ├── RuntimeOperators/             # Runtime operator implementations
│   └── TypeConversion/               # Type conversion infrastructure
├── Attributes/                         # Attribute types for method binding
│   ├── BindableMethodAttribute.cs     # Marks method as callable from SQL
│   ├── BindableClassAttribute.cs      # Marks class as containing bindable methods
│   ├── BindablePropertyAsTableAttribute.cs # Exposes property as virtual table
│   ├── AggregationMethodAttribute.cs  # Base marker for aggregate metadata
│   ├── AggregateFunctionAttribute.cs  # Declares a typed runtime-v2 aggregate kernel
│   ├── AggregateParentAttribute.cs    # Marks aggregate parent-depth metadata
│   ├── WindowFunctionAttribute.cs     # Marks method as a window function
│   ├── InjectSourceAttribute.cs       # Injects data source context
│   ├── InjectSpecificSourceAttribute.cs   # Injects specific named source
│   ├── InjectTypeAttribute.cs         # Injects type information
│   ├── InjectQueryStatsAttribute.cs   # Injects query statistics
│   ├── MethodCategoryAttribute.cs     # Categorizes methods for documentation
│   ├── MethodCategories.cs            # Category constants
│   ├── NonDeterministicAttribute.cs   # Marks non-deterministic functions
│   └── DynamicObjectProperty*Attribute.cs # Dynamic object property type hints
├── Helpers/                            # Utility functions
├── LibraryMethodResolver.cs            # Resolves SQL function calls to C# methods
├── ILibraryMethodResolver.cs           # Method resolver interface
├── IWindowFunction.cs                  # Window function interface
├── IQueryStats.cs                      # Query statistics interface
├── QueryStats.cs                       # Query statistics implementation
├── Constants.cs                        # Shared constants
├── Soundex.cs                          # Soundex algorithm implementation
├── PrimitiveTypeEntity.cs             # Wrapper for primitive type entities
├── DiffSegmentEntity.cs               # Diff segment data entity
├── Friends.cs                          # InternalsVisibleTo declarations
└── Assembly.cs                         # Assembly-level attributes
```

## How to Add New SQL Functions

### Simple Function

Add a method to the appropriate `LibraryBase*` partial class in `Lib/`:

```csharp
// In LibraryBaseStrings.cs
[BindableMethod]
public string Reverse([InjectSource] string value)
{
    return new string(value.Reverse().ToArray());
}
```

- Mark with `[BindableMethod]` to make it callable from SQL
- Use `[InjectSource]` for the implicit source parameter
- The method name becomes the SQL function name

### Aggregation Function

Runtime-v2 aggregate functions should expose a static typed kernel through `[AggregateFunction]`. The declaration method is used for SQL binding and result typing; generated hot paths call the kernel directly. The old object-backed plugin `Group` state is deleted and must not be reintroduced.

```csharp
[AggregateFunction(typeof(PositiveTotalAggregate), Name = "PositiveTotal", Inline = true)]
public decimal? PositiveTotal(decimal? value, [AggregateParent] int parent = 0)
{
    return AggregateFunction.NotInvoked<decimal?>();
}

public static class PositiveTotalAggregate
{
    public struct State
    {
        public bool HasValue;
        public decimal Value;
    }

    public static void Set(ref State state, decimal? value)
    {
        if (!value.HasValue || value.Value < 0m)
            return;

        state.Value = state.HasValue
            ? checked(state.Value + value.Value)
            : value.Value;
        state.HasValue = true;
    }

    public static decimal? Get(in State state)
    {
        return state.HasValue ? state.Value : null;
    }

    public static void Merge(ref State target, in State source)
    {
        if (!source.HasValue)
            return;

        target.Value = target.HasValue
            ? checked(target.Value + source.Value)
            : source.Value;
        target.HasValue = true;
    }
}
```

- `[AggregateFunction(typeof(...), Name = ...)]` advertises the runtime-v2 kernel for a SQL aggregate declaration.
- Plain `[AggregationMethod]` declarations are metadata-only compatibility and are not executable runtime-v2 aggregates.
- The kernel must expose a concrete `State` type, `Set(ref State, args...)`, `Get(in State)`, and optional `Merge(ref State, in State)`.
- `Set` arguments are the aggregate value arguments only. `[AggregateParent]` is metadata and is excluded from the kernel call.
- For multiple aggregate arguments, declare separate `Set` parameters. Planning also records tuple-shaped input metadata, but generated code should pass the concrete arguments directly.
- Use nullable result types for aggregates that can have no qualifying values. `Count` is the normal exception and returns `long`.
- `Merge` is required only when the aggregate can participate in merge/parallel strategies.
- The legacy aggregate get/set attributes and group injection marker are deleted. Do not add object-backed group state for normal runtime-v2 aggregate work.

### Window Function

Implement `IWindowFunction` and mark with `[WindowFunctionAttribute]`:

```csharp
[WindowFunction]
public class RowNumber : IWindowFunction
{
    public void PartitionStart()
    {
    }

    public void AccumulateValue(object? value)
    {
    }

    public object? GetCurrentValue()
    {
        // Store and return state from a real implementation.
        return null;
    }
}
```

## Key Classes

| Class | Purpose |
|-------|---------|
| `LibraryBase` | Abstract base class — all function categories are partial classes of this |
| `LibraryMethodResolver` | Resolves SQL function names to C# `MethodInfo` objects |
| `IWindowFunction` | Interface for window function implementations |
| `QueryStats` | Provides query-level statistics (row count, etc.) |

## Dependencies

```
Musoq.Plugins (leaf project — no dependencies)
    ↑ depended on by: Musoq.Schema, Musoq.Evaluator
```

## Development Workflow

### Testing

```bash
# Run plugins tests (4,362 tests, ~1 second)
dotnet test src/dotnet/Musoq.Plugins.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Common Modifications

**Adding a new function category:**
1. Create a new partial class file `LibraryBase{Category}.cs` in `Lib/`
2. Declare it as `public partial class LibraryBase`
3. Add methods with `[BindableMethod]` attribute
4. Add tests in `Musoq.Plugins.Tests`

**Adding a new attribute:**
1. Create attribute class in `Attributes/`
2. The attribute is detected by `LibraryMethodResolver` during method resolution
3. Update resolution logic if the attribute changes binding behavior

### Impact of Changes

Plugin changes affect all SQL queries that use the modified functions:
- Run plugins tests: `dotnet test src/dotnet/Musoq.Plugins.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- If new methods are added, test them via SQL queries in evaluator tests too
- Method signature changes are breaking — existing queries may fail
