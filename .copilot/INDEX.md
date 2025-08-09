# .copilot Documentation Index

This directory contains comprehensive documentation for AI agents working with the Musoq SQL query engine codebase.

## Documentation Files

### 📖 [README.md](./README.md)
**Main documentation entry point** - Comprehensive overview covering architecture, components, development workflow, and testing strategies. Start here for a complete understanding of the Musoq system.

**Key Topics:**
- Quick start and essential understanding
- Architecture overview and design principles  
- Core component deep dive (Parser, Schema, Converter, Evaluator, Plugins)
- Plugin development basics
- Query processing pipeline
- Development workflow and project structure
- API usage patterns
- Testing strategies
- Build and deployment
- Troubleshooting guide
- Key files reference

### 🏗️ [architecture-deep-dive.md](./architecture-deep-dive.md)
**Detailed technical architecture** - In-depth analysis of each component's internal architecture, design patterns, and integration strategies.

**Key Topics:**
- Component architecture details
- Parser module internals (AST, lexing, precedence)
- Schema module abstractions (ISchema, RowSource, data flow)
- Converter build chain pipeline
- Evaluator compilation and execution
- Plugin system architecture
- Integration patterns
- Performance considerations
- Memory management and optimization

### 🔌 [plugin-development-guide.md](./plugin-development-guide.md)
**Complete plugin development reference** - Step-by-step guide for creating data sources and function libraries.

**Key Topics:**
- Data source plugin creation (schema, row source, entities)
- Advanced row source patterns (chunking, parameterization)
- Dynamic schema support and runtime column discovery
- Function library development (basic, generic, aggregation)
- Complex function examples (JSON, HTTP/Web)
- Plugin registration and deployment
- Testing plugin development
- Best practices for performance and error handling

### 🛠️ [development-debugging-guide.md](./development-debugging-guide.md)
**Development environment and debugging** - Comprehensive guide for setting up development environment and debugging techniques.

**Key Topics:**
- Development environment setup
- Component-specific development cycles
- Debugging techniques (AST, code generation, execution, schema resolution)
- Testing strategies (unit, integration, performance)
- Build and CI/CD configuration
- Troubleshooting common issues (parse errors, schema failures, compilation errors)
- Performance optimization and profiling

### 💻 [api-usage-examples.md](./api-usage-examples.md)
**Practical API usage and examples** - Real-world examples and usage patterns for integrating Musoq.

**Key Topics:**
- Core API overview (InstanceCreator, analysis API)
- Schema provider implementation patterns
- Data source examples (in-memory, REST API, database)
- Advanced usage patterns (parameterization, dynamic registration, streaming)
- Query analysis and optimization
- Error handling and logging
- Testing API usage

## Quick Navigation

### For New Contributors
1. Start with [README.md](./README.md) - Quick Start section
2. Read [architecture-deep-dive.md](./architecture-deep-dive.md) - Component Architecture
3. Follow [development-debugging-guide.md](./development-debugging-guide.md) - Development Environment Setup

### For Plugin Developers
1. Review [README.md](./README.md) - Plugin Development section
2. Follow [plugin-development-guide.md](./plugin-development-guide.md) - Complete guide
3. Reference [api-usage-examples.md](./api-usage-examples.md) - Data source examples

### For API Integration
1. Study [api-usage-examples.md](./api-usage-examples.md) - Core API and examples
2. Reference [README.md](./README.md) - API Usage Patterns
3. Check [development-debugging-guide.md](./development-debugging-guide.md) - Testing strategies

### For Debugging Issues
1. Check [development-debugging-guide.md](./development-debugging-guide.md) - Troubleshooting section
2. Review [README.md](./README.md) - Troubleshooting Guide
3. Use [architecture-deep-dive.md](./architecture-deep-dive.md) - Component internals

### For Performance Optimization
1. Review [architecture-deep-dive.md](./architecture-deep-dive.md) - Performance Considerations
2. Check [development-debugging-guide.md](./development-debugging-guide.md) - Performance Optimization
3. Study [plugin-development-guide.md](./plugin-development-guide.md) - Best Practices

## Key Concepts Quick Reference

### Core Architecture
- **Parser**: SQL → AST (Abstract Syntax Tree)
- **Schema**: Data source abstraction and contracts
- **Converter**: AST → C# code generation
- **Evaluator**: Dynamic compilation and execution
- **Plugins**: Extensible function library

### Data Flow
```
SQL Query → Lexer → Parser → AST → Converter → C# Code → Compiler → Assembly → Executor → Results
```

### Essential Classes
- `Parser` - Main parsing logic
- `ISchema` - Plugin interface for data sources
- `RowSource` - Data iteration abstraction
- `CompiledQuery` - Executable query wrapper
- `InstanceCreator` - Main API entry point

### Window Functions
- `RowNumber()` - Sequential row numbering
- `Rank()` - Row ranking functionality
- `DenseRank()` - Dense ranking without gaps
- `Lag<T>(value, offset, default)` - Previous row values
- `Lead<T>(value, offset, default)` - Next row values

### Plugin Pattern
```csharp
ISchema → SchemaBase → YourSchema
                  ↓
RowSource → YourRowSource
                  ↓
Your Entity Classes
```

### Testing Pattern
```csharp
[TestClass]
public class YourTests : BasicEntityTestBase
{
    [TestMethod]
    public void Should_Test_Feature()
    {
        var vm = CreateAndRunVirtualMachine(query, data);
        var results = vm.Run();
        // Assertions
    }
}
```

---

This documentation is designed specifically for AI agents and provides comprehensive coverage of the Musoq codebase for development, debugging, and extension purposes.