# Development and Debugging Guide

## Development Environment Setup

### Prerequisites

```bash
# Required software
- .NET 8.0 SDK or later
- Git
- Visual Studio 2022 or VS Code with C# extension
- Optional: dotMemory, PerfView for performance analysis
```

### Local Development Setup

```bash
# Clone the repository
git clone https://github.com/Puchaczov/Musoq.git
cd Musoq

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests to verify setup
dotnet test --verbosity normal
```

### IDE Configuration

#### Visual Studio 2022
- Enable nullable reference types warnings
- Configure code analysis rules
- Set up debugging for dynamic assemblies

#### VS Code
```json
// .vscode/settings.json
{
    "dotnet.defaultSolution": "Musoq.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "csharp.semanticHighlighting.enabled": true
}
```

## Development Workflow

### Component Development Cycle

```mermaid
graph LR
    A[Identify Component] --> B[Write Tests]
    B --> C[Implement Feature]
    C --> D[Run Unit Tests]
    D --> E[Run Integration Tests]
    E --> F[Performance Testing]
    F --> G[Code Review]
    G --> H[Merge]
```

### Making Changes

#### 1. Parser Module Changes

When modifying SQL parsing logic:

```bash
# Run parser-specific tests
dotnet test Musoq.Parser.Tests --verbosity detailed

# Test with real queries
dotnet test Musoq.Evaluator.Tests --filter "TestCategory=Parser"
```

**Key areas to test:**
- Token recognition for new keywords
- AST node generation
- Operator precedence
- Error reporting

#### 2. Schema Module Changes

When adding new schema features:

```bash
# Test schema functionality
dotnet test Musoq.Schema.Tests

# Test integration with evaluator
dotnet test Musoq.Evaluator.Tests --filter "TestCategory=Schema"
```

**Key considerations:**
- Method resolution
- Type inference
- Runtime context handling
- Performance implications

#### 3. Converter Module Changes

When modifying code generation:

```bash
# Test code generation
dotnet test Musoq.Converter.Tests

# Verify generated code compiles
dotnet test Musoq.Evaluator.Tests --filter "TestCategory=CodeGeneration"
```

**Debugging generated code:**
```csharp
// Enable code inspection in tests
var buildItems = InstanceCreator.CreateForAnalyze(query, ...);
var generatedCode = buildItems.BuildedCode;
Console.WriteLine(generatedCode); // Inspect generated C#
```

#### 4. Evaluator Module Changes

When modifying execution logic:

```bash
# Test query execution
dotnet test Musoq.Evaluator.Tests

# Run performance benchmarks
dotnet run --project Musoq.Benchmarks --configuration Release
```

## Debugging Techniques

### 1. AST Debugging

```csharp
// Debug parser output
public void DebugParseTree()
{
    var lexer = new Lexer("SELECT Name FROM #test.data()");
    var parser = new Parser(lexer);
    var rootNode = parser.ComposeAll();
    
    // Set breakpoint and inspect AST structure
    PrintAST(rootNode, 0);
}

private void PrintAST(Node node, int depth)
{
    var indent = new string(' ', depth * 2);
    Console.WriteLine($"{indent}{node.GetType().Name}");
    
    foreach (var child in node.Children)
    {
        PrintAST(child, depth + 1);
    }
}
```

### 2. Code Generation Debugging

```csharp
// Inspect generated C# code
public void DebugCodeGeneration()
{
    var query = "SELECT Name, Count(*) FROM #test.data() GROUP BY Name";
    var buildItems = InstanceCreator.CreateForAnalyze(
        query, 
        Guid.NewGuid().ToString(), 
        schemaProvider, 
        loggerResolver);
    
    // Generated code available in buildItems
    var generatedCode = buildItems.BuildedCode;
    File.WriteAllText("debug_generated.cs", generatedCode);
    
    // Compile and inspect assembly
    var compiledQuery = InstanceCreator.CompileForExecution(query, ...);
    var assembly = compiledQuery.GetType().Assembly;
}
```

### 3. Query Execution Debugging

```csharp
// Debug query execution with logging
public void DebugQueryExecution()
{
    var loggerResolver = new TestsLoggerResolver();
    var logger = loggerResolver.GetLogger("Debug");
    
    var compiledQuery = InstanceCreator.CompileForExecution(
        query, 
        Guid.NewGuid().ToString(), 
        schemaProvider, 
        loggerResolver);
    
    // Enable execution tracing
    using var cts = new CancellationTokenSource();
    var results = compiledQuery.Run(cts.Token);
    
    // Inspect results
    foreach (var row in results)
    {
        logger.LogInformation($"Row: {string.Join(", ", row)}");
    }
}
```

### 4. Schema Resolution Debugging

```csharp
// Debug schema and method resolution
public void DebugSchemaResolution()
{
    var schema = new MyCustomSchema();
    var context = new RuntimeContext();
    
    // Test table resolution
    var table = schema.GetTableByName("test", context, "param1", "param2");
    
    // Test method resolution
    var methodFound = schema.TryResolveMethod(
        "CustomMethod", 
        new[] { typeof(string), typeof(int) }, 
        typeof(MyEntity), 
        out var methodInfo);
    
    if (methodFound)
    {
        Console.WriteLine($"Resolved method: {methodInfo.Name}");
    }
}
```

## Testing Strategies

### Unit Testing Patterns

#### Parser Tests
```csharp
[TestClass]
public class CustomParserTests
{
    [TestMethod]
    public void Should_Parse_New_Syntax()
    {
        // Arrange
        var query = "SELECT * FROM #new.syntax('param') WITH OPTIONS";
        var lexer = new Lexer(query);
        var parser = new Parser(lexer);
        
        // Act
        var rootNode = parser.ComposeAll();
        
        // Assert
        Assert.IsInstanceOfType(rootNode, typeof(RootNode));
        // Additional assertions for AST structure
    }
}
```

#### Schema Tests
```csharp
[TestClass]
public class CustomSchemaTests
{
    [TestMethod]
    public void Should_Resolve_Custom_Method()
    {
        // Arrange
        var schema = new CustomSchema();
        
        // Act
        var resolved = schema.TryResolveMethod(
            "ProcessText", 
            new[] { typeof(string) }, 
            typeof(TextEntity), 
            out var methodInfo);
        
        // Assert
        Assert.IsTrue(resolved);
        Assert.AreEqual("ProcessText", methodInfo.Name);
    }
}
```

#### Integration Tests
```csharp
[TestClass]
public class FullQueryIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void Should_Execute_Complex_Query()
    {
        // Arrange
        var data = CreateTestData();
        var query = @"
            WITH processed AS (
                SELECT Name, ProcessText(Description) as CleanText
                FROM #test.data()
            )
            SELECT Name, Count(*) as WordCount
            FROM processed
            WHERE Length(CleanText) > 10
            GROUP BY Name
            ORDER BY WordCount DESC";
        
        // Act
        var vm = CreateAndRunVirtualMachine(query, data);
        var results = vm.Run();
        
        // Assert
        Assert.IsTrue(results.Count > 0);
        // Verify result structure and content
    }
}
```

### Performance Testing

#### Benchmark Setup
```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class QueryPerformanceBenchmark
{
    private ISchemaProvider _schemaProvider;
    private string _complexQuery;
    
    [GlobalSetup]
    public void Setup()
    {
        _schemaProvider = CreateSchemaProvider();
        _complexQuery = "SELECT ... FROM ... WHERE ... GROUP BY ... ORDER BY ...";
    }
    
    [Benchmark]
    public Table ExecuteComplexQuery()
    {
        var compiledQuery = InstanceCreator.CompileForExecution(
            _complexQuery, 
            Guid.NewGuid().ToString(), 
            _schemaProvider, 
            new TestsLoggerResolver());
        
        return compiledQuery.Run();
    }
}
```

#### Memory Analysis
```csharp
[TestMethod]
public void Should_Not_Leak_Memory()
{
    var initialMemory = GC.GetTotalMemory(true);
    
    for (int i = 0; i < 1000; i++)
    {
        var compiledQuery = InstanceCreator.CompileForExecution(query, ...);
        var results = compiledQuery.Run();
        // Process results
    }
    
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();
    
    var finalMemory = GC.GetTotalMemory(true);
    var memoryIncrease = finalMemory - initialMemory;
    
    Assert.IsTrue(memoryIncrease < 100_000_000, "Memory increase should be minimal");
}
```

## Build and CI/CD

### Build Scripts

#### Local Build Script
```bash
#!/bin/bash
# build.sh

echo "Starting Musoq build process..."

# Clean previous builds
dotnet clean

# Restore dependencies
echo "Restoring dependencies..."
dotnet restore

# Build solution
echo "Building solution..."
dotnet build --configuration Release --no-restore

# Run tests
echo "Running tests..."
dotnet test --configuration Release --no-build --verbosity normal

# Pack NuGet packages
echo "Packing NuGet packages..."
dotnet pack --configuration Release --no-build

echo "Build completed successfully!"
```

#### Windows Build Script
```powershell
# build.ps1

Write-Host "Starting Musoq build process..." -ForegroundColor Green

# Clean previous builds
dotnet clean

# Restore dependencies
Write-Host "Restoring dependencies..." -ForegroundColor Yellow
dotnet restore

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --configuration Release --no-build --verbosity normal

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

# Pack NuGet packages
Write-Host "Packing NuGet packages..." -ForegroundColor Yellow
dotnet pack --configuration Release --no-build

Write-Host "Build completed successfully!" -ForegroundColor Green
```

### Continuous Integration

#### GitHub Actions Workflow
```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --no-restore --configuration Release
    
    - name: Test
      run: dotnet test --no-build --configuration Release --verbosity normal
    
    - name: Pack
      run: dotnet pack --no-build --configuration Release
    
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: nuget-packages
        path: '**/*.nupkg'
```

## Troubleshooting Common Issues

### 1. Parse Errors

**Symptom**: Unexpected token errors during parsing
```
Error: Unexpected token 'IDENTIFIER' at position 15
```

**Debug Steps**:
```csharp
// 1. Check lexer output
var lexer = new Lexer(problematicQuery);
var tokens = new List<Token>();
while (lexer.Current().TokenType != TokenType.EndOfFile)
{
    tokens.Add(lexer.Current());
    lexer.Next();
}

// 2. Verify token sequence
foreach (var token in tokens)
{
    Console.WriteLine($"{token.TokenType}: '{token.Value}' at {token.Span}");
}
```

**Common Causes**:
- Missing keywords in lexer
- Incorrect operator precedence
- Invalid character sequences

### 2. Schema Resolution Failures

**Symptom**: Schema or method not found errors
```
Error: Schema 'custom' not found
Error: Method 'ProcessText' could not be resolved
```

**Debug Steps**:
```csharp
// 1. Verify schema registration
var schema = schemaProvider.GetSchema("custom");
if (schema == null)
{
    Console.WriteLine("Schema not registered");
}

// 2. Check method signatures
var methodFound = schema.TryResolveMethod(
    "ProcessText", 
    parameterTypes, 
    entityType, 
    out var methodInfo);

if (!methodFound)
{
    Console.WriteLine("Method signature mismatch");
}
```

**Common Causes**:
- Schema not registered in provider
- Method signature mismatch
- Missing BindableMethod attribute

### 3. Compilation Errors

**Symptom**: Generated code fails to compile
```
Error: CS0103: The name 'unknownVariable' does not exist in the current context
```

**Debug Steps**:
```csharp
// 1. Inspect generated code
var buildItems = InstanceCreator.CreateForAnalyze(query, ...);
File.WriteAllText("debug.cs", buildItems.BuildedCode);

// 2. Try manual compilation
var syntaxTree = CSharpSyntaxTree.ParseText(buildItems.BuildedCode);
var compilation = CSharpCompilation.Create("DebugAssembly")
    .AddSyntaxTrees(syntaxTree)
    .AddReferences(/* required references */);

var diagnostics = compilation.GetDiagnostics();
foreach (var diagnostic in diagnostics)
{
    Console.WriteLine(diagnostic);
}
```

**Common Causes**:
- Missing using statements
- Type inference errors
- Invalid variable names

### 4. Runtime Errors

**Symptom**: Exceptions during query execution
```
Error: Object reference not set to an instance of an object
Error: Invalid cast exception
```

**Debug Steps**:
```csharp
// 1. Enable detailed logging
var loggerResolver = new DetailedLoggerResolver();

// 2. Wrap execution in try-catch
try
{
    var results = compiledQuery.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

// 3. Check data source state
var rowSource = schema.GetRowSource("test", context);
foreach (var row in rowSource.GetRows(CancellationToken.None))
{
    Console.WriteLine($"Row: {row}");
}
```

**Common Causes**:
- Null reference exceptions in data sources
- Type conversion failures
- Resource disposal issues

## Performance Optimization

### Profiling Queries

```csharp
public class QueryProfiler
{
    public static ProfileResult ProfileQuery(string query, ISchemaProvider provider)
    {
        var stopwatch = Stopwatch.StartNew();
        var initialMemory = GC.GetTotalMemory(false);
        
        // Compilation time
        var compileStart = stopwatch.ElapsedMilliseconds;
        var compiledQuery = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString(), provider, new TestsLoggerResolver());
        var compileTime = stopwatch.ElapsedMilliseconds - compileStart;
        
        // Execution time
        var executeStart = stopwatch.ElapsedMilliseconds;
        var results = compiledQuery.Run();
        var executeTime = stopwatch.ElapsedMilliseconds - executeStart;
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        
        return new ProfileResult
        {
            TotalTime = stopwatch.ElapsedMilliseconds,
            CompileTime = compileTime,
            ExecuteTime = executeTime,
            MemoryUsed = finalMemory - initialMemory,
            RowCount = results.Count
        };
    }
}
```

### Optimization Strategies

1. **Query-Level Optimizations**:
   - Use WHERE clauses to filter early
   - Optimize JOIN order
   - Avoid unnecessary SELECT columns

2. **Data Source Optimizations**:
   - Implement efficient data streaming
   - Use appropriate data structures
   - Cache expensive operations

3. **Code Generation Optimizations**:
   - Inline simple operations
   - Use type-specific optimizations
   - Minimize object allocations

This guide provides comprehensive coverage of development and debugging techniques for working with the Musoq codebase effectively.