# Repository Guidelines

## Project Structure & Module Organization

Musoq is a .NET SQL query engine. The solution is `src/dotnet/Musoq.sln`; production and test projects are siblings under `src/dotnet/`. Core boundaries include `Musoq.Parser` (SQL syntax), `Musoq.Schema` (data-source contracts), `Musoq.Evaluator` (planning and execution), `Musoq.Converter` (compilation), `Musoq.Plugins` (built-in functions), and `Musoq.Targets.*` (execution targets and renderers). Shared test helpers are in `Musoq.Tests.Common`; benchmarks are in `Musoq.Benchmarks`. Documentation, specifications, scripts, release notes, and image assets are in `docs/`, `specs/`, `scripts/`, `release-notes/`, and `images/`.

## Instruction Guides

Start with the repo-wide [Copilot guide](.github/copilot-instructions.md). Before editing a covered module, read its dedicated guide: [Parser](src/dotnet/Musoq.Parser/copilot-instructions.md), [Evaluator](src/dotnet/Musoq.Evaluator/copilot-instructions.md), [Converter](src/dotnet/Musoq.Converter/copilot-instructions.md), [Schema](src/dotnet/Musoq.Schema/copilot-instructions.md), [Plugins](src/dotnet/Musoq.Plugins/copilot-instructions.md), [Playground](src/dotnet/Musoq.Playground/copilot-instructions.md), or [Benchmarks](src/dotnet/Musoq.Benchmarks/copilot-instructions.md). `Musoq.Targets.*`, `Musoq.Tests.Common`, and examples currently follow the repo-wide guide. For planner, Execution IR, or renderer work, also read the [architecture rules](.claude/rules/architecture.md).

## Engineering Constraints

- Reflection is prohibited by default in implementation, tests, and test helpers. Prefer typed APIs, visitors, pattern matching, and explicit fixtures or registries. Existing uses are not precedent; unavoidable exceptions require an explicit, documented justification. Follow the [reflection policy](.claude/rules/code-quality.md#reflection-policy).
- Start every wave with scoped cleanup and refactoring that supports its upcoming work. Performance and maintainability are acceptance criteria in every wave, not deferred cleanup tasks. Record when no preparatory refactoring is needed and avoid unrelated churn. Follow the [implementation-wave workflow](.claude/rules/multi-session.md#implementation-wave-workflow) and [performance and maintainability criteria](.claude/rules/code-quality.md#performance-and-maintainability).

## Build, Test, and Development Commands

Use the .NET SDK pinned by `global.json` (10.0.401 or a compatible newer 10.0 feature band):

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

For focused feedback, test a project such as `src/dotnet/Musoq.Parser.Tests` or use `--filter "FullyQualifiedName~TestName"`. Run benchmarks with `dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build --` and a narrow BenchmarkDotNet filter.

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

## Commit & Pull Request Guidelines

Use imperative Conventional-Commit-style subjects, optionally scoped: `feat(evaluator): ...`, `fix(parser): ...`, `test: ...`, or `chore(release): ...`. Keep commits focused. Pull requests should explain the behavior and affected modules, link the relevant issue when one exists, and report the exact build/test commands run; include screenshots only for user-facing visual changes. Keep release and package changes aligned with the [Publish workflow](.github/workflows/publish.yml) and [release scripts](scripts/release/).
