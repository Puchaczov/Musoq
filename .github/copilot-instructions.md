# Musoq: SQL Query Engine Development Guide

Musoq is a SQL query engine that compiles SQL queries into executable .NET code at runtime, enabling SQL queries over diverse data sources (files, git, APIs, etc.) with nearly 1000 built-in methods.

**Always reference these instructions first** and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Mandatory Planning Rule

Every time you generate a plan for a task, the **penultimate step** (second-to-last) must always be:

> **Re-read `copilot-instructions.md` and verify that all created or modified code follows every rule defined in it** — especially the Code Quality & Maintainability Standards section. If any violations are found, fix them before marking the task complete.

This is non-negotiable. Delivering working code that violates project standards is not acceptable. The final step of any plan should be running the relevant tests; the step immediately before that is the compliance check against these instructions.

## What is Musoq?

**Core Concept**: Musoq transforms SQL queries into compiled C# code that executes against arbitrary data sources. It's designed for developers who want SQL's declarative power for everyday scripting tasks (file processing, git analysis, data transformation) instead of writing throwaway scripts.

**Key Architecture**: SQL text → AST → Generated C# code → Compiled .NET assembly → Execution

## Working Effectively

### Prerequisites and Environment Setup
- **Required**: .NET 8.0 SDK (specified in [global.json](global.json))
- **Recommended**: Visual Studio or VS Code with C# extension
- **OS**: Works on Windows, Linux, and macOS
- **Package Management**: All projects generate NuGet packages on build (version 7.0.0)

### Core Development Workflow
Bootstrap, build, and test the repository:
```bash
# 1. Initial setup - takes ~30 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet restore src/dotnet/Musoq.sln

# 2. Build solution - takes ~20 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore

# 3. Run full test suite - takes ~2.1 minutes. NEVER CANCEL. Set timeout to 180+ seconds.
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --verbosity normal

# 4. Clean when needed - takes ~1 second
dotnet clean src/dotnet/Musoq.sln

# 5. Package for distribution - takes ~2 seconds
dotnet pack src/dotnet/Musoq.sln --configuration Release --no-build
```

### Project Structure and Key Components
Musoq is organized into these modules, all located in `src/dotnet/`:
- **Musoq.Parser**: SQL syntax parsing and AST generation using recursive descent parser
- **Musoq.Evaluator**: Query execution engine and runtime - compiles and executes generated code
- **Musoq.Converter**: Code generation and compilation (contains `InstanceCreator`, the main API entry point)
- **Musoq.Schema**: Type system and data source abstraction via `ISchema` and `ISchemaProvider`
- **Musoq.Plugins**: Built-in SQL functions library (~1000 methods for string, math, aggregation, etc.)
- **Musoq.Playground**: Interactive testing project for experimenting with queries
- **Musoq.*.Tests**: Test projects for each module (1467 tests total)
- **Musoq.Tests.Common**: Shared test utilities and base classes (e.g., `BasicEntityTestBase`)
- **Musoq.Benchmarks**: Performance benchmarks using BenchmarkDotNet

The solution file [Musoq.sln](src/dotnet/Musoq.sln) is located in `src/dotnet/` with all projects as siblings.

**Important Files**:
- **API Entry**: [InstanceCreator.cs](src/dotnet/Musoq.Converter/InstanceCreator.cs) - Main compilation interface
- **Core Interfaces**: [ISchema.cs](src/dotnet/Musoq.Schema/ISchema.cs), [ISchemaProvider.cs](src/dotnet/Musoq.Schema/ISchemaProvider.cs)
- **Query Result**: [CompiledQuery.cs](src/dotnet/Musoq.Evaluator/CompiledQuery.cs) with `Run()` method
- **Architecture Docs**: [docs/architecture.md](docs/architecture.md) - Detailed architecture guide
- **Specifications**: [specs/](specs/) - Language specifications and proposals

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

## Code Quality & Maintainability Standards

**Your job is not just to deliver working features — it is to deliver MAINTAINABLE code.** Every human who reads your output must be able to understand, modify, and extend it without a guide. Code is read ~10x more than it's written, so optimize for the reader, not the writer. Always ask: "Would a teammate new to this codebase understand what I just wrote without asking me?"

### Control Flow
- **Fail fast / return early.** Validate at the top and bail out. Don't wrap the entire method body in a success branch — the happy path should have the least indentation.
- **No `else` after `return` / `throw` / `continue`.** It's redundant nesting. The early exit already handled the branch.
- **Ternary is for simple assignments only.** If the ternary has side effects, method calls, or nests another ternary — use `if`/`else` instead.
- **Prefer `switch` expressions over `switch` statements** when the result is an assignment or return. They're more concise and exhaustiveness-checked by the compiler.
- **Use pattern matching** (`is`, `when`, property patterns) instead of type-check-then-cast chains. Write `if (obj is Foo foo)` not `if (obj is Foo) { var foo = (Foo)obj; ... }`.

### Methods / Functions
- **If you need to scroll to read a method, it's two methods.** Split on natural abstraction boundaries, not arbitrary line counts.
- **A method that does X *and* Y should be two methods.** "And" in a method name is a code smell — `ValidateAndSave`, `ParseAndTransform` — split them.
- **Boolean parameters are a red flag.** They usually mean the method has two personalities. Prefer two clearly named methods or an enum parameter.
- **More than 3-4 parameters → you're missing a concept.** Introduce a class, record, or parameter object to group related values.
- **Keep cyclomatic complexity low.** If a method has more than ~3 independent branch paths, it likely needs decomposition into smaller focused methods.
- **Prefer pure functions where possible.** A function that takes input and returns output with no side effects is trivially testable and easy to reason about.

### Naming
- **If you need a comment to explain what a variable or method does, the name is wrong.** Fix the name, delete the comment.
- **Don't encode the type in the name.** No `customerList`, `isFlag`, `strName` — the type system already carries that information.
- **Avoid meaningless noise words:** `Manager`, `Helper`, `Processor`, `Handler`, `Utils` — these usually mean you haven't identified what the thing actually *is* or *does*.
- **Abbreviations are not names.** `mgr`, `ctx`, `svc`, `impl` are unclear. Use full words unless the abbreviation is universally understood in the domain (e.g., `AST`, `SQL`).
- **Follow existing project conventions.** Before introducing a new naming pattern, study the neighbors. Consistency across the codebase beats local cleverness.

### State & Side Effects
- **Command-Query Separation (CQS).** A method should either compute and return something OR mutate state — not both. If it does both, split it.
- **No `out` parameters.** Return a tuple, a result type, or a dedicated return object instead. `out` params are a legacy pattern that hurts readability.
- **Null is not a valid business value.** If something is absent, model it explicitly — nullable reference types with clear intent, `Optional<T>`, or a `Result<T>` pattern. Never use `null` to mean "not found" or "not applicable" without making that intent obvious.

### Error Handling
- **Use specific exception types**, not bare `Exception`. Throw `ArgumentNullException`, `InvalidOperationException`, `FormatException`, etc., so callers can handle failures precisely.
- **Never swallow exceptions silently.** Empty `catch { }` blocks hide bugs. At minimum, log the error. Prefer letting exceptions propagate unless you have a concrete recovery strategy.
- **Guard at public boundaries.** Use `ArgumentNullException.ThrowIfNull()` and similar guard clauses at the entry points of public methods. Fail loudly and immediately with a clear message.
- **Throw early, catch late.** Detect errors as close to their source as possible. Handle them at the level that has enough context to do something meaningful.

### Types & Abstractions
- **Prefer composition over inheritance.** Inheritance creates tight coupling. Use interfaces and delegation unless there's a genuine "is-a" relationship.
- **Seal classes by default** unless a class is explicitly designed and documented for extension. Unsealed classes are an implicit promise of extensibility you may not intend.
- **Small interfaces over large ones (ISP).** If a consumer only needs 2 of 8 methods on an interface, the interface is too wide. Split it.
- **Use records for pure data carriers.** If a type has no behavior and just holds values, prefer `record` or `record struct` — you get value equality, `ToString`, and deconstruction for free.
- **Don't over-abstract.** An interface with a single implementation is noise unless you have a concrete reason (testability with mocking, plugin extensibility). Wait for the second use case before extracting an abstraction.

### Dead Weight
- **Delete commented-out code on sight.** That's what git history is for. Commented-out code rots, misleads, and clutters.
- **If a variable is assigned and immediately returned, just return the expression directly** — unless the variable name adds meaningful documentation.
- **Remove `else` branches that only contain a `throw` or `return`.** Restructure so the happy path flows linearly without unnecessary nesting.
- **Remove unused `using` directives, parameters, and local variables.** Dead code is a maintenance tax and a source of confusion.
- **Don't comment method bodies.** If a method's implementation is unclear, refactor it or add a descriptive name instead of commenting.

### Tests
- **One logical assertion per test.** Test one behavior, not the whole feature. If a test fails, you should know exactly what broke from the test name alone.
- **Test names describe the scenario and expected outcome.** Use a consistent pattern like `WhenSomething_OrSomethingElse_ShouldFail` or a clear descriptive sentence.
- **Arrange-Act-Assert structure.** Every test should have a clearly visible setup, a single action, and focused verification. No test logic — no `if`, `for`, or `while` inside tests.
- **No garbage assertions.** Every assertion must validate actual expected behavior. `Assert.IsNotNull(result)` is meaningless if you don't also assert *what* the result contains. No shortcuts.
- **Tests are documentation.** A new contributor should be able to read your tests and understand how the system behaves without reading a single line of production code.

### C#-Specific Conventions
- **Prefer `readonly` fields and properties** where a value doesn't change after construction. Immutability narrows the space of possible bugs.
- **Use target-typed `new`** (`List<int> items = new()`) when the type is obvious from context. Avoid when it hurts clarity.
- **Use collection expressions** (`[1, 2, 3]`) where the compiler supports them and it improves readability.
- **LINQ is for transforms, not side effects.** Never use `.Select()` or `.Where()` to mutate state. Use `foreach` for side effects.
- **`var` is fine when the type is obvious** from the right-hand side (`var list = new List<int>()`). Avoid `var` when the type isn't immediately clear.
- **String interpolation over concatenation.** Prefer `$"Hello {name}"` over `"Hello " + name`. Use `string.Empty` over `""` for clarity of intent.

### General Hygiene
- **Leave the campsite cleaner than you found it** — but scope your improvements to what you touch. Improve the file you're editing, not the entire subsystem. Refactoring unrelated code in the same PR creates noise and merge risk.
- **The copy-paste threshold is two.** If you're about to paste something a second time, stop and extract it. Three copies means three bugs to fix instead of one.
- **Magic numbers and strings are a tax on the next reader.** Name them with constants or well-named variables that explain *why* that value exists.
- **Overview your changes before committing.** Ask yourself: is there anything that looks similar that we could extract? We must not duplicate code. No shortcuts.

## Validation

### Manual Testing and Validation Scenarios
- **ALWAYS run the full test suite** after making changes: `dotnet test --configuration Release`
- **The test suite validates core functionality**: 1467 tests total (1465 passing, 2 skipped) cover parsing, evaluation, compilation, and schema resolution
- **For targeted testing**, run specific modules:
  ```bash
  # Test parser changes - takes ~1.5 seconds (148 tests)
  dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build
  
  # Test evaluator changes - takes ~90 seconds (largest test suite)
  dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build
  
  # Test converter changes - takes ~4 seconds (2 tests)
  dotnet test src/dotnet/Musoq.Converter.Tests --configuration Release --no-build
  
  # Test schema changes - takes ~1.5 seconds (85 tests)
  dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build
  
  # Test plugins changes - takes ~1.7 seconds (421 tests)
  dotnet test src/dotnet/Musoq.Plugins.Tests --configuration Release --no-build
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
- **NuGet packages are generated**: Build produces 12 .nupkg files for all distributable modules
- **No build-time dependencies**: Only requires .NET 8.0 SDK

### Performance and Benchmarks Validation
- **Benchmarks validate functionality**: Run `dotnet run --project src/dotnet/Musoq.Benchmarks --configuration Release` to verify core query engine works
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

## Module-Specific Development

### Parser Module Changes
When modifying SQL parsing logic:
```bash
# Validate syntax parsing
dotnet test src/dotnet/Musoq.Parser.Tests --verbosity detailed

# Test integration with evaluator
dotnet test src/dotnet/Musoq.Evaluator.Tests --filter TestCategory=Parser
```
Key areas: Token recognition, AST generation, operator precedence, error reporting.

### Schema Module Changes  
When adding new data source types:
```bash
# Test schema functionality
dotnet test src/dotnet/Musoq.Schema.Tests

# Test schema integration
dotnet test src/dotnet/Musoq.Evaluator.Tests --filter TestCategory=Schema
```
Key considerations: Method resolution, type inference, runtime context handling.

### Evaluator Module Changes
When modifying query execution:
```bash
# Test core evaluation engine
dotnet test src/dotnet/Musoq.Evaluator.Tests

# Run benchmarks to check performance impact
dotnet run --project src/dotnet/Musoq.Benchmarks --configuration Release
```
Key areas: Query compilation, runtime execution, memory management.

### Converter Module Changes
When modifying code generation:
```bash
# Test code generation
dotnet test src/dotnet/Musoq.Converter.Tests

# Verify generated code compiles
dotnet test src/dotnet/Musoq.Evaluator.Tests --filter TestCategory=CodeGeneration
```
Key areas: C# code generation, assembly compilation, runtime loading.

## Common Development Tasks

### Building Individual Projects
```bash
# Build specific project
dotnet build src/dotnet/Musoq.Parser --configuration Release

# Build project with dependencies
dotnet build src/dotnet/Musoq.Evaluator --configuration Release
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
- **Architecture documentation**: See [docs/architecture.md](docs/architecture.md) for deep dive into query processing pipeline
- **API usage examples**: Reference project documentation in [docs/](docs/) directory
- **Practical examples**: See [README.md](README.md) for real-world query examples (git analysis, file processing, etc.)
- **Plugin development**: Examine existing plugins in [src/dotnet/Musoq.Plugins](src/dotnet/Musoq.Plugins) directory
- **Specifications**: See [specs/](specs/) for detailed specifications (especially [musoq-interpretation-schemas-spec-v3.md](specs/musoq-interpretation-schemas-spec-v3.md))
- **Test examples**: [ArithmeticTests.cs](src/dotnet/Musoq.Evaluator.Tests/ArithmeticTests.cs) demonstrates test patterns using `BasicEntityTestBase`

## Critical Timing Expectations

### Build Commands - NEVER CANCEL These Operations
- **dotnet restore**: 15-30 seconds depending on cache state (measured: ~17s)
- **dotnet build**: 20-30 seconds for Release configuration (measured: ~24s)
- **dotnet test**: 2-3 minutes for full test suite (measured: ~2m32s for 1467 tests)
- **Individual module tests**: 1-90 seconds depending on module
  - Parser: ~1.5 seconds (148 tests)
  - Schema: ~1.5 seconds (85 tests) 
  - Plugins: ~1.7 seconds (421 tests)
  - Converter: ~4 seconds (2 tests)
  - Evaluator: ~90+ seconds (largest test suite)

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

### Compilation Pipeline Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              MUSOQ COMPILATION PIPELINE                              │
└─────────────────────────────────────────────────────────────────────────────────────┘

                              ┌──────────────────┐
                              │   SQL Query      │
                              │   (string)       │
                              └────────┬─────────┘
                                       │
                    ╔══════════════════▼══════════════════╗
                    ║        1. LEXING & PARSING          ║
                    ║        (Musoq.Parser)               ║
                    ╚══════════════════╤══════════════════╝
                                       │
        ┌──────────────────────────────┼──────────────────────────────┐
        │                              │                              │
        ▼                              ▼                              ▼
┌───────────────┐           ┌──────────────────┐           ┌──────────────────┐
│    Lexer      │─────────▶│     Parser       │─────────▶│    RootNode      │
│ (Tokenizer)   │  tokens   │ (Recursive       │   AST     │    (AST Root)    │
│               │           │  Descent)        │           │                  │
└───────────────┘           └──────────────────┘           └────────┬─────────┘
                                                                    │
                    ╔══════════════════▼══════════════════╗
                    ║     2. AST TRANSFORMATION           ║
                    ║     (Visitor Pipeline)              ║
                    ╚══════════════════╤══════════════════╝
                                       │
    ┌──────────────────────────────────┼──────────────────────────────────┐
    │                                  │                                  │
    ▼                                  ▼                                  ▼
┌────────────────────┐    ┌────────────────────────┐    ┌─────────────────────────┐
│DistinctToGroupBy   │    │ExtractRawColumns       │    │BuildMetadataAndInferTypes│
│Visitor             │    │Visitor                 │    │Visitor                   │
│(Query Rewriting)   │    │(Column Discovery)      │    │(Type Inference + Schema) │
└─────────┬──────────┘    └──────────┬─────────────┘    └───────────┬─────────────┘
          │                          │                              │
          └──────────────────────────┼──────────────────────────────┘
                                     │
                                     ▼
                    ┌────────────────────────────────┐
                    │     RewriteQueryVisitor        │
                    │   (Semantic Transformations)   │
                    └───────────────┬────────────────┘
                                    │
                    ╔═══════════════▼═══════════════╗
                    ║      3. CODE GENERATION       ║
                    ║   (ToCSharpRewriteTreeVisitor)║
                    ╚═══════════════╤═══════════════╝
                                    │
    ┌───────────────────────────────┼───────────────────────────────┐
    │                               │                               │
    ▼                               ▼                               ▼
┌──────────────┐         ┌──────────────────┐         ┌──────────────────────┐
│ QueryEmitter │         │ ClassEmitter     │         │ SetOperationEmitter  │
│ (SELECT,     │         │ (Row classes,    │         │ (UNION, EXCEPT,      │
│  WHERE, etc) │         │  result types)   │         │  INTERSECT)          │
└──────┬───────┘         └────────┬─────────┘         └──────────┬───────────┘
       │                          │                              │
       └──────────────────────────┼──────────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────────┐
                    │   Roslyn SyntaxTree         │
                    │   (Generated C# Code)       │
                    └────────────┬────────────────┘
                                 │
                    ╔════════════▼════════════════╗
                    ║     4. COMPILATION          ║
                    ║  (TurnQueryIntoRunnableCode)║
                    ╚════════════╤════════════════╝
                                 │
                                 ▼
                    ┌─────────────────────────────┐
                    │   Compiled Assembly         │
                    │   (DLL + PDB in memory)     │
                    └────────────┬────────────────┘
                                 │
                    ╔════════════▼════════════════╗
                    ║      5. EXECUTION           ║
                    ║    (CompiledQuery.Run())    ║
                    ╚════════════╤════════════════╝
                                 │
                                 ▼
                    ┌─────────────────────────────┐
                    │        Table Result         │
                    │   (IEnumerable<Row>)        │
                    └─────────────────────────────┘
```

### Build Chain Pattern
The compilation uses a **Chain of Responsibility** pattern in `Musoq.Converter.Build`:

```
CreateTree → CompileInterpretationSchemas → TransformTree → TurnQueryIntoRunnableCode
```

- **CreateTree**: Lexer + Parser → RootNode AST
- **CompileInterpretationSchemas**: Handles `DEFINE SCHEMA` statements (binary/text parsing schemas)
- **TransformTree**: Runs all visitor transformations
- **TurnQueryIntoRunnableCode**: Roslyn compilation → DLL bytes

### Visitor System Overview

All visitors implement `IExpressionVisitor` from [Musoq.Parser](src/dotnet/Musoq.Parser/IExpressionVisitor.cs). Key patterns:

| Visitor Type | Purpose | Pattern |
|--------------|---------|---------|
| **Traverse Visitor** | Controls AST traversal order | Calls `node.Accept(innerVisitor)` for children |
| **Clone Visitor** | Creates modified AST copy | Pops children from stack, creates new nodes |
| **Rewrite Visitor** | In-place semantic changes | Modifies node properties or replaces nodes |

#### Phase 1: Pre-Processing Visitors
| Visitor | Location | Purpose |
|---------|----------|---------|
| `DistinctToGroupByVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/DistinctToGroupByVisitor.cs) | Rewrites `SELECT DISTINCT` as `GROUP BY` for unified handling |
| `ExtractRawColumnsVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/ExtractRawColumnsVisitor.cs) | Collects all column references before type inference |

#### Phase 2: Metadata & Type Inference
| Visitor | Location | Purpose |
|---------|----------|---------|
| `BuildMetadataAndInferTypesVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/BuildMetadataAndInferTypesVisitor.cs) | **Core semantic analysis**: resolves schemas, infers types, validates methods, builds symbol tables |
| `SchemaDefinitionVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/SchemaDefinitionVisitor.cs) | Extracts `DEFINE SCHEMA` definitions for interpretation schemas |

#### Phase 3: Query Rewriting
| Visitor | Location | Purpose |
|---------|----------|---------|
| `RewriteQueryVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/RewriteQueryVisitor.cs) | **Main AST transformer**: normalizes query structure, resolves aliases, prepares for code gen |
| `RewriteWhereExpressionToPassItToDataSourceVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/RewriteWhereExpressionToPassItToDataSourceVisitor.cs) | Predicate pushdown - extracts WHERE conditions safe for data source filtering |
| `RewritePartsWithProperNullHandlingVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/RewritePartsWithProperNullHandlingVisitor.cs) | Adds proper null type information to NullNode |
| `RewritePartsToUseJoinTransitionTable` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/RewritePartsToUseJoinTransitionTable.cs) | Rewrites JOINs to use intermediate transition tables |
| `CloneQueryVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/CloneQueryVisitor.cs) | Base class for creating modified AST copies |

#### Phase 4: Code Generation
| Visitor | Location | Purpose |
|---------|----------|---------|
| `ToCSharpRewriteTreeVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/ToCSharpRewriteTreeVisitor.cs) | **Main code emitter**: transforms AST to Roslyn SyntaxTree (C# code) |
| `CommonSubexpressionAnalysisVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/CommonSubexpressionAnalysisVisitor.cs) | CSE optimization - identifies repeated expressions for caching |
| `GetSelectFieldsVisitor` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/GetSelectFieldsVisitor.cs) | Extracts output column definitions for result schema |
| `InterpreterCodeGenerator` | [Visitors/](src/dotnet/Musoq.Evaluator/Visitors/InterpreterCodeGenerator.cs) | Generates C# parser classes from `DEFINE SCHEMA` (binary/text) |

#### Code Generation Emitters (in `Visitors/CodeGeneration/`)
| Emitter | Purpose |
|---------|---------|
| `QueryEmitter` | SELECT, WHERE, GROUP BY, ORDER BY, SKIP, TAKE |
| `ClassEmitter` | Row classes and result types |
| `SetOperationEmitter` | UNION, EXCEPT, INTERSECT |
| `JoinEmitter` | JOIN and APPLY operations |
| `GroupByEmitter` | Grouping and aggregation |
| `CteEmitter` | Common Table Expressions |
| `WhereEmitter` | WHERE clause filtering |
| `DescStatementEmitter` | DESC schema/table commands |

### Traverse vs Inner Visitor Pattern
Most visitors come in pairs:
- **TraverseVisitor** (e.g., `RewriteQueryTraverseVisitor`): Controls **when** and **in what order** child nodes are visited
- **InnerVisitor** (e.g., `RewriteQueryVisitor`): Performs the actual **transformation logic**

```csharp
// Example: TransformTree.cs orchestration
var rewriter = new RewriteQueryVisitor();
var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, scopeWalker);
queryTree.Accept(rewriteTraverser);  // Traverse drives, Rewriter transforms
queryTree = rewriter.RootScript;     // Get transformed tree
```

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
- **Package conflicts**: Use `dotnet clean` then rebuild if dependency issues occur

### Development Environment Issues  
- **"Permission denied" during benchmarks**: This is normal - benchmarks will run but without high priority
- **Temp file conflicts**: Delete `/tmp/Musoq` folder if compilation conflicts occur
- **Assembly loading errors**: Restart development session if assembly conflicts persist

### Debugging Failed Tests
```bash
# Run specific failing test with detailed output
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --verbosity detailed --filter "TestMethodName"

# Check for test data dependencies
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --verbosity detailed --logger "console;verbosity=diagnostic"

# Run tests in isolation to identify environment conflicts
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --verbosity normal --collect:"XPlat Code Coverage"
```

