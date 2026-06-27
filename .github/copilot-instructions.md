# Musoq: SQL Query Engine Development Guide

Musoq is a SQL query engine that lowers SQL queries through logical and physical query plans, lowers physical plans into Execution IR, renders that IR into executable .NET code at runtime, and runs it over diverse data sources (files, git, APIs, etc.) with nearly 1000 built-in methods.

**Always reference these instructions first** and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Mandatory Planning Rule

Every time you generate a plan for a task, the **penultimate step** (second-to-last) must always be:

> **Re-read `copilot-instructions.md` and verify that all created or modified code follows every rule defined in it** — especially the Code Quality & Maintainability Standards section. If any violations are found, fix them before marking the task complete.

This is non-negotiable. Delivering working code that violates project standards is not acceptable. The final step of any plan should be running the relevant tests; the step immediately before that is the compliance check against these instructions.

## What is Musoq?

**Core Concept**: Musoq transforms SQL queries through typed intermediate plans into compiled C# code that executes against arbitrary data sources. It's designed for developers who want SQL's declarative power for everyday scripting tasks (file processing, git analysis, data transformation) instead of writing throwaway scripts.

**Key Architecture**: SQL text → typed AST → logical query plan → query planning decisions/properties → physical query plan → Execution IR → generated C# code → compiled .NET assembly → execution

## How These Instruction Files Fit Together

Musoq's guidance is layered. Read the most specific file that applies, then fall back to the broader ones:

| File(s) | Role | Audience |
|---------|------|----------|
| [.github/copilot-instructions.md](.github/copilot-instructions.md) | Standalone root guide with the full rule set | GitHub Copilot |
| [CLAUDE.md](CLAUDE.md) | Root guide for Claude Code; mirrors this file and `@`-imports the rule modules | Claude Code |
| `.claude/rules/*.md` | Canonical rule modules (`architecture`, `code-quality`, `multi-session`, `troubleshooting`, `validation`); also auto-loaded as workspace instruction files | All agents in this workspace |
| `src/dotnet/<Project>/copilot-instructions.md` | Per-project deep dives (internal structure, key classes, workflows) | Anyone editing that project |
| [musoq_enchanced_architecture.md](musoq_enchanced_architecture.md) | Authoritative logical/physical planner and IR renderer reference | Anyone touching IR, planner, or renderer code |

When two files appear to disagree, the more specific one wins for its scope: per-project files override the root guide for that project, and `musoq_enchanced_architecture.md` is authoritative for planner/IR ownership.

### Where Does My Change Belong?

| I want to… | Start in | Notes |
|------------|----------|-------|
| Add or change a built-in SQL function | `Musoq.Plugins/Lib/` | One partial `LibraryBase` per category; see the Plugins per-project file |
| Change SQL lexing, parsing, or AST nodes | `Musoq.Parser` | Lexer is direct character scanning; AST nodes drive everything downstream |
| Add a data source or change the schema contract | `Musoq.Schema` | `ISchema`/`ISchemaProvider`; keep public source APIs stable |
| Change what a query *means* (relational semantics) | `Musoq.Evaluator/IR/Logical` | Logical plan, not strategy |
| Choose an execution strategy (join/aggregate/window/paging) | `Musoq.Evaluator/IR/Planning` + `IR/Physical` | Strategy decisions live in the planner, not the renderer |
| Change executable operations or runtime metadata | `Musoq.Evaluator/IR/Execution` | Lowering coordinators + Execution IR records |
| Change only generated C# syntax | `IR/Execution/Rendering` or `IR/CodeGeneration` | Faithful emission only; never invent strategy here |
| Change compilation orchestration or the public API | `Musoq.Converter` | `InstanceCreator` is the public entry point |
| Add or adjust a performance benchmark | `Musoq.Benchmarks` | Establish a baseline before optimizing |

## Working Effectively

### Prerequisites and Environment Setup
- **Required**: .NET 10.0.300 SDK or newer 10.0 feature band (pinned in [global.json](global.json) with `rollForward: latestFeature`)
- **Recommended**: Visual Studio or VS Code with C# extension
- **OS**: Works on Windows, Linux, and macOS
- **Package Management**: All projects generate NuGet packages on build (versions vary per module)

### Core Development Workflow
Bootstrap, build, and test the repository:
```bash
# 1. Initial setup - takes ~30 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet

# 2. Build solution - takes ~20 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
dotnet build src/dotnet/Musoq.sln --configuration Release --no-restore --nologo --verbosity quiet

# 3. Run full test suite - takes ~2.1 minutes. NEVER CANCEL. Set timeout to 180+ seconds.
dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --logger "trx"

# 4. Clean when needed - takes ~1 second
dotnet clean src/dotnet/Musoq.sln --nologo --verbosity quiet

# 5. Package for distribution - takes ~2 seconds
dotnet pack src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet
```

### Token-Friendly Command Output
- Prefer quiet success output for routine validation: add `--nologo --verbosity quiet` to `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet pack`, and `dotnet clean` unless diagnosing command setup.
- For `dotnet test`, pair quiet MSBuild output with useful test summaries: `--logger "console;verbosity=minimal"`. Add `--logger "trx"` when preserving a full result file helps later inspection.
- When tests fail, first rerun the smallest failing project or test filter with `--logger "console;verbosity=normal"`. Use `--verbosity detailed`, `--logger "console;verbosity=detailed"`, or diagnostic output only after the narrowed run still lacks enough context.
- For benchmark runs, build once with quiet output, use `dotnet run --no-build`, prefer `--filter` and `--job short`, redirect BenchmarkDotNet console output to a log file, and inspect the generated report in `BenchmarkDotNet.Artifacts/results/`.

### Project Structure and Key Components
Musoq is organized into these modules, all located in `src/dotnet/`:
- **Musoq.Parser**: SQL syntax parsing and AST generation using recursive descent parser
- **Musoq.Evaluator**: Query execution engine and runtime - owns AST transformations, Expression IR, logical plans, `QueryPlanner`, physical query plans, Execution IR, IR-based C# rendering, and runtime support
- **Musoq.Converter**: Compilation orchestration (contains `InstanceCreator`, the main API entry point) - wires parsing, semantic transformations, logical plan construction, `QueryPlanner`, Execution IR lowering, IR rendering, and Roslyn compilation
- **Musoq.Schema**: Type system and data source abstraction via `ISchema` and `ISchemaProvider`
- **Musoq.Plugins**: Built-in SQL functions library (~1000 methods for string, math, aggregation, etc.)
- **Musoq.Playground**: Interactive testing project for experimenting with queries
- **Musoq.*.Tests**: Test projects for each module; exact counts drift with active planner work, so trust current test output and `.copilot_session_summary.md`
- **Musoq.Tests.Common**: Shared test utilities and base classes (e.g., `BasicEntityTestBase`)
- **Musoq.Benchmarks**: Performance benchmarks using BenchmarkDotNet

The solution file [Musoq.sln](src/dotnet/Musoq.sln) is located in `src/dotnet/` with all projects as siblings.

**Important Files**:
- **API Entry**: [InstanceCreator.cs](src/dotnet/Musoq.Converter/InstanceCreator.cs) - Main compilation interface
- **Pipeline Orchestration**: [TransformTree.cs](src/dotnet/Musoq.Converter/Build/TransformTree.cs) - Runs AST transforms, builds logical/physical plans, lowers to Execution IR, and renders via the IR renderer
- **Build State**: [BuildItems.cs](src/dotnet/Musoq.Converter/Build/BuildItems.cs) - Carries `LogicalPlan`, `PhysicalPlan`, Execution IR inspection data, compilation metadata, and generated artifacts
- **Logical Plan Builder**: [LogicalPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Logical/LogicalPlanBuilder.cs) - Lowers the normalized typed AST into relational operators
- **Query Planner**: [QueryPlanner.cs](src/dotnet/Musoq.Evaluator/IR/Planning/QueryPlanner.cs) - Derives plan properties, including internal source-aware metadata and diagnostics, records `PlanningDecision` diagnostics, and routes logical plans into physical plan construction
- **Physical Plan Builder**: [PhysicalPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Physical/PhysicalPlanBuilder.cs) - Constructs physical nodes from planner-owned strategy/property decisions
- **Execution Plan Builder**: [PhysicalToExecutionPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.cs) - Lowers physical plan strategies into explicit executable operations and runtime metadata
- **Execution IR Nodes**: [ExecutionNode.cs](src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionNode.cs) and [ExecutionExpression.cs](src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionExpression.cs) - Model executable table, row, join, aggregate, window, and expression operations
- **IR Renderer**: [CSharpRenderer.cs](src/dotnet/Musoq.Evaluator/IR/CodeGeneration/CSharpRenderer.cs), [ExecutionCSharpRenderer.cs](src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionCSharpRenderer.cs), and [RenderContext.cs](src/dotnet/Musoq.Evaluator/IR/CodeGeneration/RenderContext.cs) - Render Execution IR to Roslyn syntax
- **Expression IR**: [ExpressionConverter.cs](src/dotnet/Musoq.Evaluator/IR/Expressions/ExpressionConverter.cs) - Converts parser expressions into typed IR expressions used by plans and renderers
- **Core Interfaces**: [ISchema.cs](src/dotnet/Musoq.Schema/ISchema.cs), [ISchemaProvider.cs](src/dotnet/Musoq.Schema/ISchemaProvider.cs)
- **Query Result**: [CompiledQuery.cs](src/dotnet/Musoq.Evaluator/CompiledQuery.cs) with `Run()` method
- **Planner Architecture Reference**: [musoq_enchanced_architecture.md](musoq_enchanced_architecture.md) - Authoritative logical/physical planner and IR renderer reference for this branch
- **Enhanced Architecture**: [musoq_enchanced_architecture.md](musoq_enchanced_architecture.md) - Broader architecture guide; verify against the current IR planner before relying on generated-code examples
- **Specifications**: [specs/](specs/) - Language specifications and proposals

### Per-Project Instructions

Each non-test project has its own `copilot-instructions.md` with project-specific architecture, key classes, internal structure, and development workflow. **Read the relevant per-project file when working on that module.**

| Project | Instructions | What's Inside |
|---------|-------------|---------------|
| **Musoq.Parser** | [copilot-instructions.md](src/dotnet/Musoq.Parser/copilot-instructions.md) | Lexer internals, token and AST node types, GenericFunctionRegex, interpretation schema nodes; parser output feeds the typed AST and planner pipeline |
| **Musoq.Evaluator** | [copilot-instructions.md](src/dotnet/Musoq.Evaluator/copilot-instructions.md) | AST visitor pipeline, Expression IR, logical/physical plans, IR renderers, runtime optimization, known planner/codegen patterns |
| **Musoq.Converter** | [copilot-instructions.md](src/dotnet/Musoq.Converter/copilot-instructions.md) | Build chain pattern, InstanceCreator API, plan construction, IR rendering orchestration |
| **Musoq.Schema** | [copilot-instructions.md](src/dotnet/Musoq.Schema/copilot-instructions.md) | ISchema/ISchemaProvider interfaces, data source abstraction, method resolution pipeline, planner-consumed schema metadata, how to implement new data sources |
| **Musoq.Plugins** | [copilot-instructions.md](src/dotnet/Musoq.Plugins/copilot-instructions.md) | ~1000 built-in functions, LibraryBase partial classes, aggregation/window function patterns, how to add new SQL functions |
| **Musoq.Playground** | [copilot-instructions.md](src/dotnet/Musoq.Playground/copilot-instructions.md) | Interactive query testing console app |
| **Musoq.Benchmarks** | [copilot-instructions.md](src/dotnet/Musoq.Benchmarks/copilot-instructions.md) | 20+ benchmark suites, running/interpreting benchmarks, baseline workflow |

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

## Performance Changes — Mandatory Baseline Rule

**Before implementing ANY performance optimization**, you MUST:

1. **Identify the owning layer** — Performance behavior must be selected in the physical plan or represented in Execution IR before it reaches the C# renderer. Renderer-only changes are acceptable only for faithful syntax emission of existing strategy metadata or local syntax cleanup.
2. **Establish a baseline** — Run the relevant benchmarks on the **unmodified code** and record the results. This is non-negotiable. Without a baseline, you cannot prove an optimization is actually faster.
3. **Run benchmarks after changes** — Run the same benchmarks on the modified code under identical conditions.
4. **Compare and report** — Present a before/after comparison table showing the metric, baseline value, optimized value, and percentage change. Flag any regressions.

Generated C# samples are acceptance evidence for strategy choices. Do not treat generated C# as the primary optimization surface; if a sample looks slow, first ask what physical strategy or Execution IR metadata should have prevented that shape.

For benchmark commands, result interpretation thresholds, baseline workflow, and when to add new benchmarks, see the [Musoq.Benchmarks copilot-instructions.md](src/dotnet/Musoq.Benchmarks/copilot-instructions.md).

## Validation

### Manual Testing and Validation Scenarios
- **ALWAYS run the full test suite** after making changes: `dotnet test src/dotnet/Musoq.sln --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --logger "trx"`
- **The test suite validates core functionality** across parsing, semantic analysis, logical/physical planning, IR rendering, compilation, execution, plugins, and schema resolution. Test counts change as the planner grows; use the current command output as authoritative.
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
- **Plan construction tests** live under [src/dotnet/Musoq.Evaluator.Tests/IR](src/dotnet/Musoq.Evaluator.Tests/IR) and cover Expression IR, logical nodes, physical nodes, builders, renderers, and end-to-end IR pipeline behavior.
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
- **NuGet packages are generated**: Build produces 12 .nupkg files for all distributable modules
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
- **Architecture documentation**: See [musoq_enchanced_architecture.md](musoq_enchanced_architecture.md) for the query processing pipeline and optimizer ownership model
- **API usage examples**: Reference [README.md](README.md), [specs/](specs/), and focused tests for current examples
- **Practical examples**: See [README.md](README.md) for real-world query examples (git analysis, file processing, etc.)
- **Plugin development**: Examine existing plugins in [src/dotnet/Musoq.Plugins](src/dotnet/Musoq.Plugins) directory
- **Specifications**: See [specs/](specs/) for detailed specifications (especially [musoq-binary-text-spec.md](specs/musoq-binary-text-spec.md) for interpretation schemas)
- **Test examples**: [ArithmeticTests.cs](src/dotnet/Musoq.Evaluator.Tests/ArithmeticTests.cs) demonstrates test patterns using `BasicEntityTestBase`

## Critical Timing Expectations

### Build Commands - NEVER CANCEL These Operations
- **dotnet restore**: 15-30 seconds depending on cache state (measured: ~17s)
- **dotnet build**: 20-30 seconds for Release configuration (measured: ~24s)
- **dotnet test**: full solution testing can take a few minutes depending on cache state and current test count
- **Individual module tests**: varies by module
   - Parser, Schema, Plugins, and Converter are usually quick
   - Evaluator is the largest suite because it covers query planning, rendering, compilation, and runtime execution

### Memory and Performance
- **The system generates and compiles C# code at runtime**
- **Memory usage increases during compilation phases**
- **Performance tests validate query execution times**
- **Benchmarks project provides performance measurement tools**

## Architecture Understanding

### Query Processing Pipeline
1. **Parse**: SQL text → `RootNode` parser AST
2. **Normalize and type**: AST visitors rewrite syntax, infer schema metadata, bind methods, and produce a normalized typed AST
3. **Build logical plan**: `LogicalPlanBuilder` + `LogicalPlanBuildTraverseVisitor` lower the typed AST into a `LogicalNode` tree that describes what the query means
4. **Plan query**: `QueryPlanner` derives properties, records planning diagnostics, and invokes physical plan construction
5. **Build physical plan**: `PhysicalPlanBuilder.Lower()` maps logical operators to `PhysicalNode` execution strategies selected by planner-owned rules
6. **Build Execution IR**: `PhysicalToExecutionPlanBuilder` lowers physical strategies into explicit executable operations and metadata
7. **Render C#**: `CSharpRenderer` and `ExecutionCSharpRenderer` walk Execution IR and emit Roslyn syntax
8. **Compile**: `TurnQueryIntoRunnableCode` compiles the generated C# into an in-memory assembly
9. **Execute**: `CompiledQuery.Run()` executes against data sources from schema providers and returns table results

### Current Pipeline

```text
SQL text
   -> Musoq.Parser lexer/parser
   -> RootNode AST
   -> TransformTree visitor pipeline
          DistinctToGroupBy
          SubqueryToCte
          ExtractRawColumns
          BuildMetadataAndInferTypes
          ConstantFolding
          DeadCteEliminator
          RewriteQueryVisitor
   -> LogicalPlanBuilder
   -> LogicalNode tree with OutputSchema
   -> QueryPlanner with PlanProperties and PlanningDecision diagnostics
   -> PhysicalPlanBuilder
   -> PhysicalNode tree with execution strategies
   -> PhysicalToExecutionPlanBuilder
   -> ExecutionNode tree with executable operations and metadata
   -> CSharpRenderer + ExecutionCSharpRenderer + RenderContext
   -> Roslyn CompilationUnitSyntax
   -> CSharpCompilation
   -> in-memory assembly
   -> CompiledQuery.Run()
```

### IR Planner Concepts

- **IR is the active runtime path**: converter code generation is IR-only on this branch. There is no renderer toggle on `CompilationOptions`; execution never routes back to the deleted `ToCSharpRewriteTreeVisitor`.
- **Expression IR**: parser expressions become immutable `IrExpression` records such as `ColumnRef`, `Literal`, `BinaryOp`, `MethodCall`, `AggregateRef`, and `WindowFunctionRef`.
- **OutputSchema**: every logical and physical plan node carries authoritative column names, aliases, indexes, and types.
- **QueryPlanner**: owns strategy/property decisions between logical planning and physical node construction. It derives required column usage, predicate pushdown, conservative source projection metadata, source interaction contracts, source boundary diagnostics, and predicate placement diagnostics; records `PlanningDecision` diagnostics; and exposes `PlanningText` through query inspection.
- **Source-aware planning boundary**: source-aware planner records are internal diagnostics and metadata. Preserve `ISchema`, `ISchemaColumn`, `ISchemaProvider`, `RuntimeContext`, `QuerySourceInfo`, row-source, and plugin/library public contracts unless a separate public API design explicitly changes them.
- **Physical strategies**: planner-owned rules choose aggregate-only, single-key, value-tuple, hash join, sort-merge join, nested-loop join, top-N/top-offset, and window materialization strategies; `PhysicalPlanBuilder` constructs the corresponding nodes.
- **Execution IR**: physical nodes lower into explicit operations such as scans, table creation, row append, materialization, joins, ranking/window computation, sorting, paging, and projection.
- **Optimization ownership**: `QueryPlanner` chooses safe query-level strategy/property decisions; physical planning builds nodes from those decisions; Execution IR carries executable decisions and runtime metadata such as materialization shape, capacity hints, context liveness, static metadata, typed keys, and precomputed lookup sets. Renderers should not invent global performance strategies.
- **Behavior-consuming planner facts**: planner diagnostics may change behavior only after being promoted to explicit internal strategy records such as `PredicateMovementPlan`, `RowWidthPruningPlan`, set-operation and CTE execution strategies, or source-boundary strategy guards. Execution builders lower and defensively validate those records; renderers only emit the resulting Execution IR. `BoundaryRowShapePlan` remains diagnostic row-shape metadata; selected sort/top/top-offset opportunities become behavior-consuming only through `RowWidthPruningPlan`. Aggregate, window, set operation, hash-join build, and CTE materialization pruning remain diagnostic-only in v1.
- **Where not to put strategy choices**: do not add query-level optimization decisions directly in `PhysicalToExecutionPlanBuilder`, renderers, generated C#, plugins, row sources, or public source APIs. Put the decision in `QueryPlanner` or a planner-owned helper, expose it through `PlanningText`, and preserve existing fallback behavior.
- **RenderContext**: centralized code-generation state for entity metadata, row classes, CTE table indexes, aggregate/window bindings, inferred columns, scope, and current row identifiers.
- **Renderer decomposition shapes**: plain, grouped, and window queries have strict physical and Execution IR shapes. If a renderer rejects a shape, inspect the logical plan, physical plan, and Execution IR before changing rendering code.
- **Debugging helpers**: use `IrExpressionPrinter`, `LogicalPlanPrinter`, `PlanningTextPrinter`, and `PhysicalPlanPrinter` to inspect intermediate representations.

Before touching IR planner, Execution IR, or renderer code, read [musoq_enchanced_architecture.md](musoq_enchanced_architecture.md). For detailed module guidance, see the per-project `copilot-instructions.md` files listed in the [Per-Project Instructions](#per-project-instructions) section above.

## Troubleshooting

### Common Issues
- **Build failures**: Usually missing .NET 10.0.300+ SDK or corrupted package cache
- **Test failures**: Often related to environment-specific paths or test data
- **Memory issues during development**: Expected due to runtime code generation
- **Package conflicts**: Use `dotnet clean` then rebuild if dependency issues occur

### Development Environment Issues  
- **"Permission denied" during benchmarks**: This is normal - benchmarks will run but without high priority
- **Temp file conflicts**: Delete `/tmp/Musoq` folder if compilation conflicts occur
- **Assembly loading errors**: Restart development session if assembly conflicts persist

### Debugging Failed Tests
```bash
# Run a specific failing test with concise but useful failure output
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity quiet --logger "console;verbosity=normal"

# Escalate to detailed output only after the narrowed run is insufficient
dotnet test src/dotnet/Musoq.Parser.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity minimal --logger "console;verbosity=detailed"

# Run tests in isolation to identify environment conflicts
dotnet test src/dotnet/Musoq.Schema.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal" --collect:"XPlat Code Coverage"
```
