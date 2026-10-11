# Musoq.Schema

Type system and data source abstraction layer. Defines the interfaces that external data source plugins implement to expose their data to SQL queries. This is the contract between the Musoq query engine and any data source.

## Internal Structure

```
Musoq.Schema/
â”śâ”€â”€ DataSources/                        # Base classes for data sources
â”‚   â”śâ”€â”€ SchemaBase.cs                   # Abstract base for schema implementations
â”‚   â”śâ”€â”€ EntitySource.cs                # Typed entity data source
â”‚   â”śâ”€â”€ RowSource.cs                    # Generic abstract row-producing source
â”‚   â”śâ”€â”€ RowSourceBase.cs               # Generic row source with shared functionality
â”‚   â”śâ”€â”€ ChunkedSource.cs               # Chunked/batched data source
â”‚   â”śâ”€â”€ ChunkEnumerator.cs            # Enumerator for chunked sources
â”‚   â”śâ”€â”€ SingleRowSource.cs            # Source producing a single row
â”‚   â””â”€â”€ SchemaColumn.cs               # Column definition
â”śâ”€â”€ Interpreters/                       # Binary/text data interpretation
â”‚   â”śâ”€â”€ IInterpreter.cs               # Base interpreter interface
â”‚   â”śâ”€â”€ IBytesInterpreter.cs           # Binary data interpreter interface
â”‚   â”śâ”€â”€ ITextInterpreter.cs            # Text data interpreter interface
â”‚   â”śâ”€â”€ BytesInterpreterBase.cs        # Binary interpreter base class
â”‚   â”śâ”€â”€ TextInterpreterBase.cs         # Text interpreter base class
â”‚   â”śâ”€â”€ PartialInterpretResult.cs      # Partial interpretation result
â”‚   â”śâ”€â”€ ParseException.cs             # Interpretation parse error
â”‚   â””â”€â”€ ParseErrorCode.cs             # Error code enum
â”śâ”€â”€ Managers/                           # Method and property resolution
â”‚   â”śâ”€â”€ MethodsManager.cs             # Resolves SQL function calls to C# methods
â”‚   â”śâ”€â”€ MethodsAggregator.cs          # Aggregates methods from multiple sources
â”‚   â”śâ”€â”€ MethodsMetadata.cs            # Method metadata cache
â”‚   â”śâ”€â”€ PropertiesManager.cs          # Resolves column names to entity properties
â”‚   â”śâ”€â”€ ManagerBase.cs                # Shared manager base
â”‚   â”śâ”€â”€ MethodResolutionKey.cs        # Cache key for method lookups
â”‚   â””â”€â”€ ParameterMetadataInfo.cs      # Method parameter metadata
â”śâ”€â”€ Reflection/                         # Reflection-based metadata
â”‚   â”śâ”€â”€ SchemaMethodInfo.cs            # Schema method metadata
â”‚   â””â”€â”€ ConstructorInfo.cs             # Constructor metadata
â”śâ”€â”€ Optimization/                       # Stateless source planning DTOs
â”‚   â”śâ”€â”€ SourcePlanRequest.cs           # ORDER BY/SKIP/TAKE planning proposal
â”‚   â”śâ”€â”€ SourcePlanResult.cs            # Accepted/residual planning result
â”‚   â”śâ”€â”€ SourceExecutionPlan.cs         # Immutable accepted plan passed to execution
â”‚   â””â”€â”€ SourceExecutionContext.cs      # Runtime source context
â”śâ”€â”€ Helpers/                            # Utility functions
â”śâ”€â”€ Diagnostics/                        # Schema-level diagnostic reporting
â”śâ”€â”€ Exceptions/                         # Schema-specific exceptions
â”śâ”€â”€ Attributes/                         # Schema attributes
â”śâ”€â”€ ISchema.cs                          # Core schema interface
â”śâ”€â”€ ISchemaProvider.cs                  # Schema provider interface
â”śâ”€â”€ ISchemaTable.cs                     # Table metadata interface
â”śâ”€â”€ ISchemaColumn.cs                    # Column metadata interface
â”śâ”€â”€ IReadOnlyRow.cs                     # Read-only row interface
â”śâ”€â”€ IReadOnlyTable.cs                   # Read-only table interface
â”śâ”€â”€ SchemaTableMetadata.cs             # Table metadata wrapper
â”śâ”€â”€ SingleRowSchemaTable.cs            # Single-row table implementation
â”śâ”€â”€ DataSourceEventArgs.cs            # Data source event args
â”śâ”€â”€ DataSourceEventHandler.cs          # Data source event handler
â””â”€â”€ DataSourcePhase.cs                 # Data source loading phase enum
```

## Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `ISchema` | Defines a data source â€” provides tables, methods, and data access |
| `ISchemaProvider` | Registry of named schemas â€” maps `#name` references to `ISchema` instances |
| `ISchemaTable` | Table metadata â€” column definitions and table name |
| `ISchemaColumn` | Column metadata â€” name, type, ordinal position |
| `IReadOnlyRow` | Read-only row access interface |
| `IReadOnlyTable` | Read-only table access interface |
| `IInterpreter` | Base interface for binary/text data interpreters |
| `IBytesInterpreter` | Binary data interpreter â€” implements `binary { ... }` schemas |
| `ITextInterpreter` | Text data interpreter â€” implements `text { ... }` schemas |

## Implementing a New Data Source

To create a new data source plugin:

1. **Implement `ISchema`** â€” define table schemas, available methods, and how to create typed data sources
2. **Implement `ISchemaProvider`** â€” register the schema under a name (e.g., `#mydata`)
3. **Create a concrete row type** â€” expose source data through properties or a deliberate dynamic type
4. **Create a `RowSource<T>`** (extend `RowSourceBase<T>` or `EntitySource<T>`) â€” produce typed rows from the data source
5. **Define column metadata** â€” implement `ISchemaTable` with `ISchemaColumn` definitions and authoritative `SchemaTableMetadata.TableEntityType`

```csharp
// Schema provider maps #name to schema
public class MySchemaProvider : ISchemaProvider
{
    public ISchema GetSchema(string schema) => schema == "mydata"
        ? new MySchema()
        : throw new SchemaNotFoundException(schema);
}

// Schema defines tables and their structure
public class MySchema : SchemaBase
{
    public override ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object[] parameters)
        => new MyTable(new SchemaTableMetadata(typeof(MyEntity)));

    public override SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object[] parameters)
        => SourcePlanResult.RejectAll(request);

    public override RowSource<T> GetRowSource<T, TArguments>(string name, SourceExecutionContext executionContext, TArguments parameters)
        => EnsureSourceType<T, MyEntity>(name, new MyRowSource(executionContext));
}

public sealed class MyRowSource(SourceExecutionContext executionContext) : RowSourceBase<MyEntity>
{
    protected override void CollectChunks(IChunkWriter<MyEntity> writer)
    {
        writer.CancellationToken.ThrowIfCancellationRequested();
        writer.Write(LoadRows(executionContext.Plan).ToArray());
    }
}
```

Runtime v2 calls `GetRowSource<T, TArguments>()` with the concrete entity type from table metadata, then iterates ordered `source.Chunks` directly. TArguments is a constrained struct implementing ISourceArguments; read constructor slots through GetValue<T> without object arrays. Metadata reflection stays at compilation; SchemaBase does not reflect constructors during execution. Runtime-shaped schemas must declare authoritative concrete column types and use native query-scoped materialization. Named dictionary fields require concrete value types such as `IReadOnlyDictionary<string, double?>`. Object-valued fields, raw object-array transfer and runtime-dynamic query values receive source-contract diagnostics; do not add an erased execution fallback.

### Stability and replayability contracts

Columns are stable by default for compatibility, but providers must mark any
getter whose value, side effects, access count, exception timing, or timing is
observable as `ColumnStability.Volatile` (or use `[NonDeterministic]` on the
entity property). Unmarked bindable functions are stable only when they are
pure for the bound row and independent of injected query statistics, runtime
type context, and mutable settings. `RowStreamReplayability` is independent:
stable columns do not make a row stream replayable, and `Unknown` must block
reuse across a second enumeration.

Portable `SourceComputedProjection` requests are accepted only when the
provider advertises the required intrinsic capability and returns an exact
accepted/residual partition. Accepted values are materialized once per source
row. Missing accepted fields, malformed partitions, and shape mismatches are
deterministic source-contract failures, never silent recomputation.

## Method Resolution Pipeline

When a SQL function is called, the method resolution pipeline finds the corresponding C# method:

```
SQL: SomeFunction(column1, column2)
  â†’ MethodsManager.TryResolve(name, argTypes)
    â†’ MethodsAggregator.GetMethods(name)
      â†’ MethodsMetadata (cached method info from LibraryBase + schema methods)
        â†’ Best matching C# method
```

- `MethodsManager`: Entry point for method resolution â€” finds the best-matching method by name and parameter types
- `MethodsAggregator`: Collects methods from all registered sources (built-in library + schema-specific methods)
- `MethodsMetadata`: Caches reflected method information for performance
- `PropertiesManager`: Similar pipeline for resolving column names to entity properties

## Dependencies

```
Musoq.Schema
â””â”€â”€ Musoq.Plugins  (LibraryBase, declaration attributes, typed aggregate/window kernels)
    â†‘ depended on by: Musoq.Evaluator, Musoq.Converter, Musoq.Playground
```

## Development Workflow

### Testing

```bash
# Run schema tests
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Common Modifications

**Adding a new data source base class:**
1. Create a generic class extending `RowSourceBase<T>` or `RowSource<T>` in `DataSources/`
2. Implement chunk production through `Chunks` or `CollectChunks(IChunkWriter<T>, CancellationToken)`
3. Ensure proper `SourceExecutionContext` and immutable `SourceExecutionPlan` handling

**Modifying method resolution:**
1. Changes in `Managers/` affect how SQL functions are resolved to C# methods
2. `MethodsManager` is the entry point â€” changes here affect all function calls
3. After changes, run both schema tests and evaluator tests

**Adding interpreter support:**
1. Implement `IBytesInterpreter` or `ITextInterpreter` in `Interpreters/`
2. The interpreter base classes provide common functionality
3. Interpreters are generated at compile time by `InterpreterCodeGenerator` in Musoq.Evaluator

### Impact of Changes

Schema is a core contract module â€” changes here ripple through:
- All data source plugins (external schemas)
- Method resolution in the evaluator
- Type inference during compilation
- Run schema tests: `dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- Run evaluator tests for integration: `dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
