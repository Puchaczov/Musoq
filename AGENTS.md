# Repository Guidelines

## Project Structure & Module Organization

Musoq is a .NET SQL query engine. The solution is `src/dotnet/Musoq.sln`; production and test projects are siblings under `src/dotnet/`. Core boundaries include `Musoq.Parser` (SQL syntax), `Musoq.Schema` (data-source contracts), `Musoq.Evaluator` (planning and execution), `Musoq.Converter` (compilation), `Musoq.Plugins` (built-in functions), and `Musoq.Targets.*` (execution targets and renderers). Shared test helpers are in `Musoq.Tests.Common`; benchmarks are in `Musoq.Benchmarks`. Documentation, specifications, scripts, release notes, and image assets are in `docs/`, `specs/`, `scripts/`, `release-notes/`, and `images/`.

The query pipeline is SQL text -> typed AST -> logical plan -> planner decisions -> physical plan -> Execution IR -> generated C# -> compiled .NET assembly -> execution. `Musoq.Evaluator.Tests.Common`, `Musoq.Converter.Tests.Common`, and `Musoq.Plugins.Tests.Common` supply module-specific fixtures without test discovery; `Musoq.Tests.Common` supplies lightweight shared helpers. Converter tests are divided into Compilation, Inspection, and Targets projects; Plugins tests are divided into Conversions and Functions projects. See [test project ownership and preservation](docs/testing/test-project-split.md).

## Instruction Guides

[AGENTS.md](AGENTS.md) is the sole repository-wide instruction entry point for coding agents. Maintain shared requirements here and in the rule files below. Links are references, not guaranteed automatic imports: read the applicable documents explicitly.

- Before editing, read the [implementation-wave and session workflow](.claude/rules/multi-session.md). Check `.copilot_session_summary.md` for current work and preserve unrelated changes.
- For implementation, tests, and helpers, read the [code-quality rules](.claude/rules/code-quality.md), including the reflection policy and performance baseline requirement.
- Select validation using the [validation policy](.claude/rules/validation.md#validation-by-change-impact); the essentials are repeated below for clients that load only this entry point.
- Before planner, physical planning, Execution IR, or renderer work, read the [architecture rules](.claude/rules/architecture.md). Query strategy belongs in the planner and explicit IR; renderers faithfully emit its decisions.
- Consult [troubleshooting](.claude/rules/troubleshooting.md) for build, test, runtime compilation, or environment failures.

Before editing a covered module or its tests, read its `AGENTS.md` and the `DEVELOPMENT.md` it requires. Read them explicitly even when the session starts at repository root; nested instruction discovery depends on the tool and working directory:

| Module | Guide |
|--------|-------|
| Parser | [Parser guide](src/dotnet/Musoq.Parser/AGENTS.md) |
| Evaluator | [Evaluator guide](src/dotnet/Musoq.Evaluator/AGENTS.md) |
| Converter | [Converter guide](src/dotnet/Musoq.Converter/AGENTS.md) |
| Schema | [Schema guide](src/dotnet/Musoq.Schema/AGENTS.md) |
| Plugins | [Plugins guide](src/dotnet/Musoq.Plugins/AGENTS.md) |
| Playground | [Playground guide](src/dotnet/Musoq.Playground/AGENTS.md) |
| Benchmarks | [Benchmarks guide](src/dotnet/Musoq.Benchmarks/AGENTS.md) |

`Musoq.Targets.*`, shared test helpers, and examples follow this entry point and the applicable rules. Module guides specialize these requirements; the architecture rules are authoritative for planner/IR/renderer ownership. Descriptions of legacy code are not exemptions from current quality rules.

## Mandatory Planning Rule

In every plan, the penultimate step must be to re-read `AGENTS.md` and the applicable rule/module guides, check compliance, and fix violations. The final step must be the validation selected by change impact. Documentation and instruction-only work finishes with diff, link, formatting, and preservation checks; it does not require solution builds or tests.

## Engineering Constraints

- Reflection is prohibited by default in implementation, tests, and test helpers. Prefer typed APIs, visitors, pattern matching, and explicit fixtures or registries. Existing uses are not precedent; unavoidable exceptions require an explicit, documented justification. Follow the [reflection policy](.claude/rules/code-quality.md#reflection-policy).
- Start every wave with scoped cleanup and refactoring that supports its upcoming work. Performance and maintainability are acceptance criteria in every wave, not deferred cleanup tasks. Record when no preparatory refactoring is needed and avoid unrelated churn. Follow the [implementation-wave workflow](.claude/rules/multi-session.md#implementation-wave-workflow) and [performance and maintainability criteria](.claude/rules/code-quality.md#performance-and-maintainability).

## Generated Files and Cleanup

- Put task-generated scratch files, logs, validation output, test results, packages, and caches under the repository-root `.artifacts/` directory, preferably in a task-specific subdirectory. Do not create a root `artifacts/` directory. Configure or override tool output paths accordingly.
- After a successful implementation and validation, remove files created for the task unless they are required for later work, reproducibility, release qualification, or an explicitly requested deliverable. Record the purpose and location of anything retained in the handoff.
- Track task-owned output before cleanup. Preserve pre-existing and unrelated files; verify resolved paths remain within the intended task directory before deleting or moving anything.

## Build, Test, and Development Commands

Use the .NET SDK pinned by `global.json` (10.0.401 or a compatible newer 10.0 feature band):

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

For focused feedback, test a project such as `src/dotnet/Musoq.Parser.Tests` or use `--filter "FullyQualifiedName~TestName"`. Run benchmarks with `dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build --` and a narrow BenchmarkDotNet filter.

The pinned SDK includes runtime 10.0.12, which fixes intermittent heap corruption under dynamic-code and parallel-GC workloads. Packaging is explicit through `dotnet pack`; tag-driven publication follows the [release guide](docs/releasing.md), [Publish workflow](.github/workflows/publish.yml), and [release scripts](scripts/release/). Clean or package only when required by the task.

### Command Output

- Prefer quiet success output for routine validation: add `--nologo --verbosity quiet` to `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet pack`, and `dotnet clean` unless diagnosing command setup.
- For `dotnet test`, pair quiet MSBuild output with useful test summaries: `--logger "console;verbosity=minimal"`. Add `--logger "trx"` when preserving a full result file helps later inspection.
- When tests fail, first rerun the smallest failing project or test filter with `--logger "console;verbosity=normal"`. Use `--verbosity detailed`, `--logger "console;verbosity=detailed"`, or diagnostic output only after the narrowed run still lacks enough context.
- For benchmark runs, build once with quiet output, use `dotnet run --no-build`, prefer `--filter` and `--job short`, configure BenchmarkDotNet output under `.artifacts/benchmarks/`, redirect console output there, and inspect its `results/` directory.

## Coding Style & Naming Conventions

Follow `.editorconfig`: UTF-8, CRLF, spaces, four-space indentation, and no trailing whitespace. Use nullable-enabled, warning-clean C#; public types and members use PascalCase, locals and parameters camelCase, and tests use descriptive scenario names. Preserve existing partial-file and namespace organization. Compiler analyzers and warnings-as-errors are the quality gate.

## Testing Guidelines

Tests use MSTest in `*.Tests` projects. Add regression coverage beside the changed module, use descriptive behavior-based names, and avoid timing- or machine-dependent assertions. Choose validation by change impact using the [validation policy](.claude/rules/validation.md#validation-by-change-impact):

- Prose, formatting, links, agent instructions, and reordered unchanged examples need diff, link/anchor, formatting, and preservation checks; no solution build or tests are required.
- Added or changed executable examples must be executed and checked against expected results; build only what execution requires.
- Test-only changes require building affected test projects and running affected tests; broaden to affected projects for shared helpers or fixtures.
- Tooling, scripts, CI, and build configuration changes require applicable syntax/configuration checks and exercising affected paths; build or test affected projects when compilation or test execution changes.
- Production code, runtime dependencies, and configuration affecting runtime behavior require focused regression checks, the Release solution build, and full Release solution tests. Preserve release-specific qualification gates.

Classify changes by their effects rather than file extensions or directories. Mixed changes use the combined requirements. Broaden checks when failures or unresolved impact justify it, and report checks performed and any limitations.

Keep tests independent of developer machines and checkout locations. Use synthetic values for parser and in-memory tests and isolated temporary fixtures for filesystem tests; never depend on a developer checkout or another repository being present.

## Commit & Pull Request Guidelines

Use imperative Conventional-Commit-style subjects, optionally scoped: `feat(evaluator): ...`, `fix(parser): ...`, `test: ...`, or `chore(release): ...`. Keep commits focused. Pull requests should explain the behavior and affected modules, link the relevant issue when one exists, and report the exact build/test commands run; include screenshots only for user-facing visual changes. Keep release and package changes aligned with the [Publish workflow](.github/workflows/publish.yml) and [release scripts](scripts/release/).

## Query Pipeline Landmarks

- **API Entry**: [InstanceCreator.cs](src/dotnet/Musoq.Converter/InstanceCreator.cs) - Main compilation interface
- **Pipeline Orchestration**: [TransformTree.cs](src/dotnet/Musoq.Converter/Build/TransformTree.cs) - Runs normalization, planning, Execution IR lowering, and target rendering
- **Build State**: [BuildItems.cs](src/dotnet/Musoq.Converter/Build/BuildItems.cs) - Carries typed phase artifacts and compilation metadata
- **Logical Plan Builder**: [LogicalPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Logical/LogicalPlanBuilder.cs) - Lowers the normalized typed AST into relational operators
- **Query Planner**: [QueryPlanner.cs](src/dotnet/Musoq.Evaluator/IR/Planning/QueryPlanner.cs) - Owns query strategy and property decisions
- **Physical Plan Builder**: [PhysicalPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Physical/PhysicalPlanBuilder.cs) - Constructs physical nodes from planner-owned decisions
- **Execution Plan Builder**: [PhysicalToExecutionPlanBuilder.cs](src/dotnet/Musoq.Evaluator/IR/Execution/PhysicalToExecutionPlanBuilder.cs) - Lowers physical strategies into executable operations and metadata
- **Execution IR Nodes**: [ExecutionNode.cs](src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionNode.cs) and [ExecutionExpression.cs](src/dotnet/Musoq.Evaluator/IR/Execution/ExecutionExpression.cs) - Model executable operations and expressions
- **C# Renderer**: [CSharpRenderer.cs](src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration/CSharpRenderer.cs), [ExecutionCSharpRenderer.cs](src/dotnet/Musoq.Targets.CSharpClr/Rendering/Execution/ExecutionCSharpRenderer.cs), and [RenderContext.cs](src/dotnet/Musoq.Targets.CSharpClr/Rendering/CodeGeneration/RenderContext.cs)
- **Expression IR**: [ExpressionConverter.cs](src/dotnet/Musoq.Evaluator/IR/Expressions/ExpressionConverter.cs) - Converts parser expressions into typed IR expressions
- **Core Interfaces**: [ISchema.cs](src/dotnet/Musoq.Schema/ISchema.cs), [ISchemaProvider.cs](src/dotnet/Musoq.Schema/ISchemaProvider.cs)
- **Query Result**: [CompiledQuery.cs](src/dotnet/Musoq.Evaluator/CompiledQuery.cs) with `Run()` method
- **Architecture Rules**: [architecture.md](.claude/rules/architecture.md) - Current planner, Execution IR, and target ownership rules
- **Specifications**: [specs/](specs/) - Language specifications and proposals

## Maintaining Shared Instructions

Edit `AGENTS.md` for shared entry-point rules and `.claude/rules/*.md` for detailed policies. Keep each module `AGENTS.md` concise and put detailed architecture, workflows and examples in its adjacent `DEVELOPMENT.md`. Preserve the detailed guidance when reorganizing entry points; do not copy long guides into automatically loaded instructions. Update references when moving a rule.
