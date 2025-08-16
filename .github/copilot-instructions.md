# Musoq: SQL Query Engine Development Guide

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Environment Setup
- **Required**: .NET 8.0 SDK (specified in global.json)
- **Recommended**: Visual Studio or VS Code with C# extension
- **OS**: Works on Windows, Linux, and macOS

### Core Development Workflow
Bootstrap, build, and test the repository:
```bash
# 1. Initial setup - takes ~30 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet restore

# 2. Build solution - takes ~20 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet build --configuration Release --no-restore

# 3. Run full test suite - takes ~2.1 minutes. NEVER CANCEL. Set timeout to 180+ seconds.
dotnet test --configuration Release --no-build --verbosity normal

# 4. Clean when needed - takes ~1 second
dotnet clean

# 5. Package for distribution - takes ~2 seconds
dotnet pack --configuration Release --no-build
```

### Project Structure and Key Components
Musoq is organized into these core modules:
- **Musoq.Parser**: SQL syntax parsing and AST generation
- **Musoq.Evaluator**: Query execution engine and runtime
- **Musoq.Converter**: Code generation and compilation
- **Musoq.Schema**: Type system and data source abstraction
- **Musoq.Plugins**: Built-in functions and operations
- **Musoq.Benchmarks**: Performance measurement tools

Each module has corresponding test projects (*.Tests) with comprehensive coverage.

## Validation

### Manual Testing and Validation Scenarios
- **ALWAYS run the full test suite** after making changes: `dotnet test --configuration Release`
- **The test suite validates core functionality**: 1183 tests cover parsing, evaluation, compilation, and schema resolution
- **For targeted testing**, run specific modules:
  ```bash
  # Test parser changes - takes ~1.5 seconds
  dotnet test Musoq.Parser.Tests --configuration Release --no-build
  
  # Test evaluator changes - takes ~90 seconds
  dotnet test Musoq.Evaluator.Tests --configuration Release --no-build
  
  # Test converter changes - takes ~15 seconds  
  dotnet test Musoq.Converter.Tests --configuration Release --no-build
  ```

### Query Engine Validation
- **The system compiles SQL queries to executable .NET code**
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
- **NuGet packages are generated**: Build produces .nupkg files for all distributable modules
- **No build-time dependencies**: Only requires .NET 8.0 SDK

## Module-Specific Development

### Parser Module Changes
When modifying SQL parsing logic:
```bash
# Validate syntax parsing
dotnet test Musoq.Parser.Tests --verbosity detailed

# Test integration with evaluator
dotnet test Musoq.Evaluator.Tests --filter TestCategory=Parser
```
Key areas: Token recognition, AST generation, operator precedence, error reporting.

### Schema Module Changes  
When adding new data source types:
```bash
# Test schema functionality
dotnet test Musoq.Schema.Tests

# Test schema integration
dotnet test Musoq.Evaluator.Tests --filter TestCategory=Schema
```
Key considerations: Method resolution, type inference, runtime context handling.

### Evaluator Module Changes
When modifying query execution:
```bash
# Test core evaluation engine
dotnet test Musoq.Evaluator.Tests

# Run benchmarks to check performance impact
dotnet run --project Musoq.Benchmarks --configuration Release
```
Key areas: Query compilation, runtime execution, memory management.

### Converter Module Changes
When modifying code generation:
```bash
# Test code generation
dotnet test Musoq.Converter.Tests

# Verify generated code compiles
dotnet test Musoq.Evaluator.Tests --filter TestCategory=CodeGeneration
```
Key areas: C# code generation, assembly compilation, runtime loading.

## Common Development Tasks

### Building Individual Projects
```bash
# Build specific project
dotnet build Musoq.Parser --configuration Release

# Build project with dependencies
dotnet build Musoq.Evaluator --configuration Release
```

### Running Specific Test Categories
```bash
# Run unit tests only
dotnet test --filter TestCategory=Unit

# Run integration tests
dotnet test --filter TestCategory=Integration

# Run performance tests (takes longer)
dotnet test --filter TestCategory=Performance
```

### Documentation and Examples
- **Architecture documentation**: See `ARCHITECTURE.md` and `.copilot/README.md`
- **API usage examples**: Reference `.copilot/api-usage-examples.md`
- **Practical examples**: See project README.md for real-world query examples
- **Plugin development**: Examine existing plugins in `Musoq.Plugins` directory

## Critical Timing Expectations

### Build Commands - NEVER CANCEL These Operations
- **dotnet restore**: 30-60 seconds depending on cache state
- **dotnet build**: 20-30 seconds for Release configuration  
- **dotnet test**: 2-3 minutes for full test suite (1183 tests)
- **Individual module tests**: 1-90 seconds depending on module

### Memory and Performance
- **The system generates and compiles C# code at runtime**
- **Memory usage increases during compilation phases**
- **Performance tests validate query execution times**
- **Benchmarks project provides performance measurement tools**

## Architecture Understanding

### Query Processing Pipeline
1. **Parse**: SQL text → Abstract Syntax Tree (AST)
2. **Convert**: AST → Generated C# code  
3. **Compile**: C# code → Executable assembly
4. **Execute**: Assembly runs against data sources via schema providers

### Plugin System
- **Schema providers**: Define how to access different data sources
- **Function libraries**: Built-in SQL functions in Musoq.Plugins
- **Extensible**: New data sources added through ISchema implementation

### Key Classes to Understand
- **InstanceCreator**: Main API entry point for compilation and execution
- **ISchemaProvider**: Interface for data source access
- **CompiledQuery**: Represents executable query with Run() method
- **BuildItems**: Contains compilation artifacts and metadata

## Troubleshooting

### Common Issues
- **Build failures**: Usually missing .NET 8.0 SDK or corrupted package cache
- **Test failures**: Often related to environment-specific paths or test data
- **Memory issues during development**: Expected due to runtime code generation

### Performance Considerations  
- **Compilation is expensive**: First query execution includes compilation overhead
- **Runtime execution is fast**: Compiled queries execute as native .NET code
- **Schema provider performance matters**: Data source access is often the bottleneck

Always validate changes with the comprehensive test suite before considering the work complete.