# Musoq.Playground

Interactive console application for testing and experimenting with Musoq SQL queries. Not a production project — used by developers to quickly validate queries, test new features, and explore the query engine behavior.

## Internal Structure

```
Musoq.Playground/
├── Program.cs                  # Entry point — modify to run your test queries
├── Library.cs                  # Custom function library for playground queries
├── NonEquiEntity.cs            # Entity for non-equijoin testing
├── NonEquiSchema.cs            # Schema for non-equijoin testing
├── NonEquiSchemaProvider.cs    # Schema provider for non-equijoin testing
├── NonEquiTable.cs             # Table definition for non-equijoin testing
├── ExpensiveCteCounter.cs      # Counter for expensive CTE performance tests
├── ExpensiveRowSource.cs       # Slow row source for performance testing
├── MyLoggerResolver.cs         # Logger resolver implementation
└── NoOpLogger.cs               # No-op logger implementation
```

## How to Use

1. Open `Program.cs`
2. Write your SQL query and set up the schema provider
3. Run with `dotnet run --project src/dotnet/Musoq.Playground`

```csharp
// Example: test a query in Program.cs
var query = "SELECT Name, Age FROM #test.data() WHERE Age > 25";
var compiled = InstanceCreator.CompileForExecution(
    query,
    Guid.NewGuid().ToString(),
    schemaProvider,
    new MyLoggerResolver());
var results = compiled.Run();
```

## Key Details

- **Target framework**: net10.0, matching the production projects
- **No automated tests**: this is a manual testing tool
- **Not NuGet-packaged**: not distributed as a library
- **NonEqui* files**: complete schema implementation for testing non-equijoin scenarios — can serve as a reference for implementing new schemas

## Dependencies

```
Musoq.Playground
├── Musoq.Converter   (InstanceCreator API)
├── Musoq.Evaluator   (CompiledQuery runtime)
├── Musoq.Schema      (ISchema, ISchemaProvider)
└── Musoq.Plugins     (built-in functions)
```
