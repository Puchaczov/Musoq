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
| [CLAUDE.md](CLAUDE.md) | Root guide for Claude Code; mirrors the Copilot guide and `@`-imports the rule modules | Claude Code |
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
- **Package Management**: Packages are generated explicitly with `dotnet pack`; release package versions live in [scripts/Versions.props](scripts/Versions.props). Publishing is tag-driven only; see [RELEASING.md](RELEASING.md).

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

# 5. Package release projects for validation - takes ~2 seconds
pwsh scripts/release/Pack-Release.ps1 -AllPackages -OutputPath artifacts/ci-nupkgs
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
- **Musoq.*.Tests**: Test projects for each module; use current test output for authoritative counts
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

@.claude/rules/multi-session.md

@.claude/rules/code-quality.md

## Performance Changes — Mandatory Baseline Rule

**Before implementing ANY performance optimization**, you MUST:

1. **Identify the owning layer** — Performance behavior must be selected in the physical plan or represented in Execution IR before it reaches the C# renderer. Renderer-only changes are acceptable only for faithful syntax emission of existing strategy metadata or local syntax cleanup.
2. **Establish a baseline** — Run the relevant benchmarks on the **unmodified code** and record the results. This is non-negotiable. Without a baseline, you cannot prove an optimization is actually faster.
3. **Run benchmarks after changes** — Run the same benchmarks on the modified code under identical conditions.
4. **Compare and report** — Present a before/after comparison table showing the metric, baseline value, optimized value, and percentage change. Flag any regressions.

Generated C# samples are acceptance evidence for strategy choices. Do not treat generated C# as the primary optimization surface; if a sample looks slow, first ask what physical strategy or Execution IR metadata should have prevented that shape.

Planner diagnostics may change behavior only after `QueryPlanner` promotes them into explicit internal strategy records in `PlanProperties` or `ExecutionStrategyPlan`. Execution builders lower and defensively validate those records; renderers emit the resulting Execution IR. Do not put query-level strategy choices directly in `PhysicalToExecutionPlanBuilder`, renderers, generated C#, plugins, row sources, or public source APIs. `BoundaryRowShapePlan` remains diagnostic row-shape metadata; selected sort/top/top-offset opportunities become behavior-consuming only through `RowWidthPruningPlan`. Aggregate, window, set operation, hash-join build, and CTE materialization pruning remain diagnostic-only in v1.

For benchmark commands, result interpretation thresholds, baseline workflow, and when to add new benchmarks, see the [Musoq.Benchmarks copilot-instructions.md](src/dotnet/Musoq.Benchmarks/copilot-instructions.md).

@.claude/rules/validation.md

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

@.claude/rules/architecture.md

@.claude/rules/troubleshooting.md
