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

## Multi-Session Communication

**Critical**: When working across multiple copilot work units or sessions, you MUST use `.copilot_session_summary.md` to communicate progress and coordinate work.

### Session Summary Protocol
- **Always check**: Read `.copilot_session_summary.md` at the start of any work unit to understand previous progress
- **Always update**: Write to `.copilot_session_summary.md` at the end of each work unit with:
  - **What was completed**: Specific tasks, files modified, tests run, issues resolved
  - **What needs to be done**: Remaining tasks, known issues, next steps
  - **Current state**: Build status, test results, any blocking issues
  - **Context for next session**: Important findings, decisions made, approach taken

### Communication Format
```markdown
# Copilot Session Summary

## Last Updated
[Timestamp and session identifier]

## Completed Tasks
- [List of completed work items]
- [Files modified with brief description]
- [Tests run and results]

## Current Status
- Build status: [Success/Failed/Not tested]
- Test status: [Pass/Fail counts and any critical failures]
- Known issues: [Any problems discovered]

## Next Steps
- [Prioritized list of remaining tasks]
- [Any specific approaches or constraints to consider]
- [Dependencies or prerequisites for next work]

## Context Notes
- [Important decisions made]
- [Approaches that didn't work]
- [Key insights for future sessions]
```

### Best Practices
- **Update frequently**: Write to session summary after each significant milestone
- **Be specific**: Include file paths, command results, error messages
- **Think ahead**: Consider what the next copilot session will need to know
- **Preserve context**: Don't assume the next session has access to previous conversation history

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
- **Comprehensive documentation**: See [.copilot Documentation](#copilot-documentation-reference) section below for complete reference

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

## .copilot Documentation Reference

The `.copilot/` directory contains comprehensive documentation specifically designed for AI agents and developers working with the Musoq codebase. This documentation provides deep technical insights and practical guidance beyond the quick reference above.

### Documentation Structure

#### 📖 [.copilot/INDEX.md](.copilot/INDEX.md)
**Navigation hub** - Start here to understand all available documentation and choose the right guide for your needs.

#### 📖 [.copilot/README.md](.copilot/README.md) 
**Main technical documentation** - Comprehensive overview covering architecture, components, development workflow, and testing strategies. Essential for understanding the complete Musoq system.

**Key sections:**
- Quick start and architecture overview
- Core component deep dive (Parser, Schema, Converter, Evaluator, Plugins)
- Plugin development basics
- Query processing pipeline
- API usage patterns
- Testing strategies with practical examples
- Build and deployment guidelines
- Troubleshooting guide with debugging techniques

#### 🏗️ [.copilot/architecture-deep-dive.md](.copilot/architecture-deep-dive.md)
**Detailed technical architecture** - In-depth analysis of internal architecture, design patterns, and component integration.

**Essential for:**
- Understanding AST node hierarchy and parser internals
- Schema module abstractions and data flow
- Converter build pipeline and code generation
- Evaluator compilation and execution details
- Plugin system architecture and integration patterns
- Performance considerations and memory management

#### 🔌 [.copilot/plugin-development-guide.md](.copilot/plugin-development-guide.md)
**Complete plugin development reference** - Step-by-step guide for creating data sources and function libraries.

**Covers:**
- Data source plugin creation (schema, row source, entities)
- Advanced row source patterns (chunking, parameterization)
- Dynamic schema support and runtime column discovery
- Function library development (basic, generic, aggregation)
- Complex function examples (JSON, HTTP/Web APIs)
- Plugin registration and deployment strategies
- Testing plugin development and best practices

#### 🛠️ [.copilot/development-debugging-guide.md](.copilot/development-debugging-guide.md)
**Development environment and debugging** - Comprehensive guide for setup and debugging techniques.

**Includes:**
- Development environment setup for different IDEs
- Component-specific development cycles and workflows
- Debugging techniques (AST inspection, code generation, execution tracing)
- Testing strategies (unit, integration, performance)
- Build and CI/CD configuration details
- Troubleshooting common issues (parse errors, schema failures, compilation errors)
- Performance optimization and profiling techniques

#### 💻 [.copilot/api-usage-examples.md](.copilot/api-usage-examples.md)
**Practical API usage and examples** - Real-world examples and integration patterns.

**Features:**
- Core API overview and entry points
- Schema provider implementation patterns
- Data source examples (in-memory, REST API, database)
- Advanced usage patterns (parameterization, dynamic registration, streaming)
- Query analysis and optimization techniques
- Error handling and logging strategies
- Testing API usage with practical examples

### When to Use Each Document

**For New Contributors:**
1. Start with [.copilot/README.md](.copilot/README.md) - Quick Start section
2. Review [.copilot/architecture-deep-dive.md](.copilot/architecture-deep-dive.md) - Component Architecture
3. Follow [.copilot/development-debugging-guide.md](.copilot/development-debugging-guide.md) - Development Setup

**For Plugin Development:**
1. Read [.copilot/README.md](.copilot/README.md) - Plugin Development section
2. Follow [.copilot/plugin-development-guide.md](.copilot/plugin-development-guide.md) - Complete guide
3. Reference [.copilot/api-usage-examples.md](.copilot/api-usage-examples.md) - Data source examples

**For API Integration:**
1. Study [.copilot/api-usage-examples.md](.copilot/api-usage-examples.md) - Core API and practical examples
2. Reference [.copilot/README.md](.copilot/README.md) - API Usage Patterns
3. Use [.copilot/development-debugging-guide.md](.copilot/development-debugging-guide.md) - Testing strategies

**For Debugging Issues:**
1. Check [.copilot/development-debugging-guide.md](.copilot/development-debugging-guide.md) - Troubleshooting section
2. Review [.copilot/README.md](.copilot/README.md) - Troubleshooting Guide
3. Use [.copilot/architecture-deep-dive.md](.copilot/architecture-deep-dive.md) - Component internals

The .copilot documentation is designed to provide comprehensive technical depth while the GitHub Copilot instructions above offer quick reference and essential workflows.