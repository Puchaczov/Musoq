# Repository Guidelines

## Project Structure & Module Organization

Musoq is a .NET SQL query engine. The solution is `src/dotnet/Musoq.sln`; production and test projects are siblings under `src/dotnet/`. Core boundaries include `Musoq.Parser` (SQL syntax), `Musoq.Schema` (data-source contracts), `Musoq.Evaluator` (planning and execution), `Musoq.Converter` (compilation), `Musoq.Plugins` (built-in functions), and `Musoq.Targets.*` (execution targets and renderers). Shared test helpers are in `Musoq.Tests.Common`; benchmarks are in `Musoq.Benchmarks`. Documentation, specifications, scripts, release notes, and image assets are in `docs/`, `specs/`, `scripts/`, `release-notes/`, and `images/`.

## Instruction Guides

Start with the repo-wide [Copilot guide](.github/copilot-instructions.md). Before editing a covered module, read its dedicated guide: [Parser](src/dotnet/Musoq.Parser/copilot-instructions.md), [Evaluator](src/dotnet/Musoq.Evaluator/copilot-instructions.md), [Converter](src/dotnet/Musoq.Converter/copilot-instructions.md), [Schema](src/dotnet/Musoq.Schema/copilot-instructions.md), [Plugins](src/dotnet/Musoq.Plugins/copilot-instructions.md), [Playground](src/dotnet/Musoq.Playground/copilot-instructions.md), or [Benchmarks](src/dotnet/Musoq.Benchmarks/copilot-instructions.md). `Musoq.Targets.*`, `Musoq.Tests.Common`, and examples currently follow the repo-wide guide. For planner, Execution IR, or renderer work, also read the [architecture rules](.claude/rules/architecture.md).

## Build, Test, and Development Commands

Use the .NET SDK pinned by `global.json` (10.0.300 or a compatible 10.0 feature band):

```powershell
dotnet restore src/dotnet/Musoq.sln --nologo --verbosity quiet
dotnet build src/dotnet/Musoq.sln -c Release --no-restore --nologo --verbosity quiet
dotnet test src/dotnet/Musoq.sln -c Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

For focused feedback, test a project such as `src/dotnet/Musoq.Parser.Tests` or use `--filter "FullyQualifiedName~TestName"`. Run benchmarks with `dotnet run --project src/dotnet/Musoq.Benchmarks -c Release --no-build --` and a narrow BenchmarkDotNet filter.

## Coding Style & Naming Conventions

Follow `.editorconfig`: UTF-8, CRLF, spaces, four-space indentation, and no trailing whitespace. Use nullable-enabled, warning-clean C#; public types and members use PascalCase, locals and parameters camelCase, and tests use descriptive scenario names. Preserve existing partial-file and namespace organization. Compiler analyzers and warnings-as-errors are the quality gate.

## Testing Guidelines

Tests use MSTest in `*.Tests` projects. Add regression coverage beside the changed module, use descriptive behavior-based names, and avoid timing- or machine-dependent assertions. Run focused tests first, then the full Release solution test command before submitting.

## Commit & Pull Request Guidelines

Use imperative Conventional-Commit-style subjects, optionally scoped: `feat(evaluator): ...`, `fix(parser): ...`, `test: ...`, or `chore(release): ...`. Keep commits focused. Pull requests should explain the behavior and affected modules, link the relevant issue when one exists, and report the exact build/test commands run; include screenshots only for user-facing visual changes. Keep release and package changes aligned with `RELEASING.md`.
