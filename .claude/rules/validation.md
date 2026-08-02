## Validation

### Manual Testing and Validation Scenarios
- **ALWAYS run the full test suite** after making changes: `dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --logger "trx"`
- **The test suite validates core functionality** across parsing, semantic analysis, logical/physical planning, IR rendering, compilation, execution, plugins, and schema resolution. Test counts change as the planner grows; use the current command output as authoritative.
- **Keep command output token-friendly**: default to `--nologo --verbosity quiet` and `--logger "console;verbosity=minimal"`; rerun only the narrowed failing test with higher console verbosity when the minimal output is not enough.
- **For targeted testing**, run specific modules:
  ```bash
   # Test parser changes
   dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

   # Test evaluator, planner, renderer, and runtime changes - largest suite
   dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

   # Test converter and pipeline orchestration changes
   dotnet test src/dotnet/Musoq.Converter.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

   # Test schema changes
   dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

   # Test plugins changes
   dotnet test src/dotnet/Musoq.Plugins.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
  ```

### IR Planner Validation
- **Plan construction tests** live under `src/dotnet/Musoq.Evaluator.Tests/IR` and cover Expression IR, logical nodes, physical nodes, builders, renderers, and end-to-end IR pipeline behavior.
- For planner shape bugs, run the focused IR tests first, then the evaluator suite:
   ```bash
   dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "FullyQualifiedName~Musoq.Evaluator.Tests.IR" --nologo --verbosity quiet --logger "console;verbosity=minimal"
   dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
   ```
- If a renderer throws `NotSupportedException` from a pipeline decomposition method, inspect the logical and physical plans before changing code generation.

### Query Engine Validation
- **The system compiles SQL queries through logical and physical plans to executable .NET code**
- **Primary API entry point**: `InstanceCreator.CompileForExecution(query, assemblyName, schemaProvider, loggerResolver)`
- **Validation through existing tests**: The test suite includes hundreds of SQL query scenarios
- **Common usage pattern**:
  ```csharp
  var compiledQuery = InstanceCreator.CompileForExecution(
      "SELECT Name, Count(*) FROM #test.data() GROUP BY Name",
      Guid.NewGuid().ToString(),
      schemaProvider,
      loggerResolver);
  var results = compiledQuery.Run();
  ```

### Build Validation
- **Build succeeds without errors**: All projects compile cleanly in Release configuration
- **NuGet packages are generated**: use `dotnet pack` or the release scripts for distributable modules
- **No build-time dependencies**: Only requires the .NET 10.0.300+ SDK

### Performance and Benchmarks Validation
- **Benchmarks validate functionality**: run a focused benchmark with quiet build output, for example `dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet` followed by `dotnet run --project src/dotnet/Musoq.Benchmarks --configuration Release --no-build -- --filter "*RelevantBenchmark*" --job short --exporters json > TestResults/benchmark.log 2>&1`
- **Performance regression testing**: Use benchmarks to measure impact of changes
- **Memory usage validation**: Monitor compilation and execution phases for memory efficiency

### Manual SQL Query Validation Scenarios
After making changes to core components, validate actual SQL functionality:

1. **Basic Query Compilation Test**:
   ```csharp
   // Test that queries compile successfully
   var query = "select 1 from #system.dual()";
   var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), schemaProvider, loggerResolver);
   var results = compiled.Run(); // Should execute without errors
   ```

2. **Arithmetic Operations Test**:
   ```sql
   SELECT 1 + 2 * 3 - 4 / 2 FROM #system.dual()
   -- Should return: 5
   ```

3. **String Operations Test**:
   ```sql
   SELECT 'Hello' + ' ' + 'World' FROM #system.dual()
   -- Should return: "Hello World"
   ```

4. **Cross-Format Number Literals** (if parser changes affect literals):
   ```sql
   SELECT 0xFF + 0b101 + 0o77 FROM #system.dual()
   -- Should return: 327 (255 + 5 + 63)
   ```

5. **Test Error Handling**:
   ```sql
   SELECT invalid_function() FROM #system.dual()
   -- Should fail with clear error message
   ```

**CRITICAL**: Always test at least one complete query execution after making changes to verify the entire pipeline works.

## Common Development Tasks

### Building Individual Projects
```bash
# Build specific project
dotnet build src/dotnet/Musoq.Parser --configuration Release --no-restore --nologo --verbosity quiet

# Build project with dependencies
dotnet build src/dotnet/Musoq.Evaluator --configuration Release --no-restore --nologo --verbosity quiet
```

### Running Specific Test Categories
```bash
# Run unit tests only
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --filter TestCategory=Unit --nologo --verbosity quiet --logger "console;verbosity=minimal"

# Run integration tests
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --filter TestCategory=Integration --nologo --verbosity quiet --logger "console;verbosity=minimal"

# Run performance tests (takes longer)
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --filter TestCategory=Performance --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Documentation and Examples
- **Architecture documentation**: See [architecture.md](architecture.md) for the query processing pipeline and optimizer ownership model
- **API usage examples**: Reference [README.md](../../README.md), [specs/](../../specs/), and focused tests for current examples
- **Practical examples**: See [README.md](../../README.md) for real-world query examples (git analysis, file processing, etc.)
- **Plugin development**: Examine existing plugins in [src/dotnet/Musoq.Plugins](../../src/dotnet/Musoq.Plugins) directory
- **Specifications**: See [specs/](../../specs/) for detailed specifications (especially [musoq-binary-text-spec.md](../../specs/musoq-binary-text-spec.md) for interpretation schemas)
- **Test examples**: [ArithmeticTests.cs](../../src/dotnet/Musoq.Evaluator.Tests/ArithmeticTests.cs) demonstrates test patterns using `BasicEntityTestBase`
