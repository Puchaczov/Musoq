# Musoq.Schema

Type system and data source abstraction layer. Defines the interfaces that external data source plugins implement to expose their data to SQL queries. This is the contract between the Musoq query engine and any data source.

## Internal Structure

```
Musoq.Schema/
├── DataSources/                        # Base classes for data sources
│   ├── SchemaBase.cs                   # Abstract base for schema implementations
│   ├── EntitySource.cs                # Typed entity data source
│   ├── RowSource.cs                    # Generic abstract row-producing source
│   ├── RowSourceBase.cs               # Generic row source with shared functionality
│   ├── ChunkedSource.cs               # Chunked/batched data source
│   ├── ChunkEnumerator.cs            # Enumerator for chunked sources
│   ├── SingleRowSource.cs            # Source producing a single row
│   └── SchemaColumn.cs               # Column definition
├── Interpreters/                       # Binary/text data interpretation
│   ├── IInterpreter.cs               # Base interpreter interface
│   ├── IBytesInterpreter.cs           # Binary data interpreter interface
│   ├── ITextInterpreter.cs            # Text data interpreter interface
│   ├── BytesInterpreterBase.cs        # Binary interpreter base class
│   ├── TextInterpreterBase.cs         # Text interpreter base class
│   ├── PartialInterpretResult.cs      # Partial interpretation result
│   ├── ParseException.cs             # Interpretation parse error
│   └── ParseErrorCode.cs             # Error code enum
├── Managers/                           # Method and property resolution
│   ├── MethodsManager.cs             # Resolves SQL function calls to C# methods
│   ├── MethodsAggregator.cs          # Aggregates methods from multiple sources
│   ├── MethodsMetadata.cs            # Method metadata cache
│   ├── PropertiesManager.cs          # Resolves column names to entity properties
│   ├── ManagerBase.cs                # Shared manager base
│   ├── MethodResolutionKey.cs        # Cache key for method lookups
│   └── ParameterMetadataInfo.cs      # Method parameter metadata
├── Reflection/                         # Reflection-based metadata
│   ├── SchemaMethodInfo.cs            # Schema method metadata
│   └── ConstructorInfo.cs             # Constructor metadata
├── Optimization/                       # Stateless source planning DTOs
│   ├── SourcePlanRequest.cs           # ORDER BY/SKIP/TAKE planning proposal
│   ├── SourcePlanResult.cs            # Accepted/residual planning result
│   ├── SourceExecutionPlan.cs         # Immutable accepted plan passed to execution
│   └── SourceExecutionContext.cs      # Runtime source context
├── Helpers/                            # Utility functions
├── Diagnostics/                        # Schema-level diagnostic reporting
├── Exceptions/                         # Schema-specific exceptions
├── Attributes/                         # Schema attributes
├── ISchema.cs                          # Core schema interface
├── ISchemaProvider.cs                  # Schema provider interface
├── ISchemaTable.cs                     # Table metadata interface
├── ISchemaColumn.cs                    # Column metadata interface
├── IReadOnlyRow.cs                     # Read-only row interface
├── IReadOnlyTable.cs                   # Read-only table interface
├── SchemaTableMetadata.cs             # Table metadata wrapper
├── SingleRowSchemaTable.cs            # Single-row table implementation
├── DataSourceEventArgs.cs            # Data source event args
├── DataSourceEventHandler.cs          # Data source event handler
└── DataSourcePhase.cs                 # Data source loading phase enum
```

## Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `ISchema` | Defines a data source — provides tables, methods, and data access |
| `ISchemaProvider` | Registry of named schemas — maps `#name` references to `ISchema` instances |
| `ISchemaTable` | Table metadata — column definitions and table name |
| `ISchemaColumn` | Column metadata — name, type, ordinal position |
| `IReadOnlyRow` | Read-only row access interface |
| `IReadOnlyTable` | Read-only table access interface |
| `IInterpreter` | Base interface for binary/text data interpreters |
| `IBytesInterpreter` | Binary data interpreter — implements `binary { ... }` schemas |
| `ITextInterpreter` | Text data interpreter — implements `text { ... }` schemas |

## Implementing a New Data Source

To create a new data source plugin:

1. **Implement `ISchema`** — define table schemas, available methods, and how to create typed data sources
2. **Implement `ISchemaProvider`** — register the schema under a name (e.g., `#mydata`)
3. **Create a concrete row type** — expose source data through properties or a deliberate dynamic type
4. **Create a `RowSource<T>`** (extend `RowSourceBase<T>` or `EntitySource<T>`) — produce typed rows from the data source
5. **Define column metadata** — implement `ISchemaTable` with `ISchemaColumn` definitions and authoritative `SchemaTableMetadata.TableEntityType`

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

    public override RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object[] parameters)
        => EnsureSourceType<T, MyEntity>(name, new MyRowSource(executionContext));
}

public sealed class MyRowSource(SourceExecutionContext executionContext) : RowSourceBase<MyEntity>
{
    protected override void CollectChunks(IChunkWriter<MyEntity> writer, CancellationToken token)
    {
        writer.Write(LoadRows(executionContext.Plan).ToArray(), token);
    }
}
```

Runtime v2 calls `GetRowSource<T>()` with the entity type from table metadata, then iterates ordered `source.Chunks` directly. Dynamic/object-like schemas should choose a concrete row type such as `IReadOnlyDictionary<string, object>`, `ExpandoObject`, or a plugin-defined dynamic row type.

## Method Resolution Pipeline

When a SQL function is called, the method resolution pipeline finds the corresponding C# method:

```
SQL: SomeFunction(column1, column2)
  → MethodsManager.TryResolve(name, argTypes)
    → MethodsAggregator.GetMethods(name)
      → MethodsMetadata (cached method info from LibraryBase + schema methods)
        → Best matching C# method
```

- `MethodsManager`: Entry point for method resolution — finds the best-matching method by name and parameter types
- `MethodsAggregator`: Collects methods from all registered sources (built-in library + schema-specific methods)
- `MethodsMetadata`: Caches reflected method information for performance
- `PropertiesManager`: Similar pipeline for resolving column names to entity properties

## Dependencies

```
Musoq.Schema
└── Musoq.Plugins  (LibraryBase, function attributes, IWindowFunction)
    ↑ depended on by: Musoq.Evaluator, Musoq.Converter, Musoq.Playground
```

## Development Workflow

### Testing

```bash
# Run schema tests (457 tests, ~0.2 seconds)
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Common Modifications

**Adding a new data source base class:**
1. Create a generic class extending `RowSourceBase<T>` or `RowSource<T>` in `DataSources/`
2. Implement chunk production through `Chunks` or `CollectChunks(IChunkWriter<T>, CancellationToken)`
3. Ensure proper `SourceExecutionContext` and immutable `SourceExecutionPlan` handling

**Modifying method resolution:**
1. Changes in `Managers/` affect how SQL functions are resolved to C# methods
2. `MethodsManager` is the entry point — changes here affect all function calls
3. After changes, run both schema tests and evaluator tests

**Adding interpreter support:**
1. Implement `IBytesInterpreter` or `ITextInterpreter` in `Interpreters/`
2. The interpreter base classes provide common functionality
3. Interpreters are generated at compile time by `InterpreterCodeGenerator` in Musoq.Evaluator

### Impact of Changes

Schema is a core contract module — changes here ripple through:
- All data source plugins (external schemas)
- Method resolution in the evaluator
- Type inference during compilation
- Run schema tests: `dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- Run evaluator tests for integration: `dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
