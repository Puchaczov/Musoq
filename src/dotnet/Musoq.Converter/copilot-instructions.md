# Musoq.Converter

Orchestration layer that wires parsing, AST transformation, logical planning, query planning, Execution IR lowering, IR rendering, and compilation together. Contains `InstanceCreator` - the main public API entry point for compiling and executing SQL queries.

## Internal Structure

```
Musoq.Converter/
├── Build/                              # Build chain steps
│   ├── BuildChain.cs                   # Chain of Responsibility base class
│   ├── BuildItems.cs                   # Compilation artifacts container
│   ├── CreateTree.cs                   # Step 1: Lexer + Parser → RootNode AST
│   ├── CompileInterpretationSchemas.cs # Step 2: binary/text schema compilation
│   ├── TransformTree.cs               # Step 3: AST transforms, staged optimizers, Execution IR lowering, backend rendering
│   ├── TerminalBuildChain.cs          # Inspection endpoint; stops after IR rendering
│   └── TurnQueryIntoRunnableCode.cs   # Step 4: backend-specific finalization -> executable artifact
├── ExecutionTargets/                   # Internal target catalog + CSharpClr composition
├── Exceptions/                         # Compilation-specific exceptions
├── Properties/                         # Assembly information
├── InstanceCreator.cs                  # Main API entry point
├── ILoggerResolver.cs                  # Logger resolver interface
├── BuildResult.cs                      # Build output wrapper
├── QueryInspectionResult.cs            # Logical/physical plan text + generated C# artifacts
└── Musoq.Converter.csproj
```

## Build Chain Pattern

The compilation uses a **Chain of Responsibility** pattern. Each step receives `BuildItems` and passes to the next:

```
CreateTree -> CompileInterpretationSchemas -> TransformTree -> TurnQueryIntoRunnableCode
```

| Step | Class | Purpose |
|------|-------|---------|
| 1 | `CreateTree` | Runs Lexer + Parser to produce `RootNode` AST from SQL text |
| 2 | `CompileInterpretationSchemas` | Extracts and compiles `binary`/`text` schema definitions into runtime interpreters |
| 3 | `TransformTree` | Executes the AST visitor pipeline, builds staged logical/physical/Execution IR artifacts, runs optimizer groups, and renders through `ExecutionTargetCatalog.Render` |
| 4 | `TurnQueryIntoRunnableCode` | Dispatches through `ExecutionTargetCatalog.FinalizeArtifact` to produce executable artifacts |

Inspection builds use the same first three steps but end at `TerminalBuildChain`, so they produce logical plan text, physical plan text, and generated C# source without emitting DLL/PDB bytes.

### TransformTree IR Pipeline

`TransformTree` is the bridge between AST normalization and executable code:

1. Runs pre-plan AST visitors: `DistinctToGroupBy`, `SubqueryToCte`, `ExtractRawColumns`, `BuildMetadataAndInferTypes`, and `RewriteQueryVisitor`. Dead CTE elimination and constant folding now belong to the logical optimizer.
2. Calls `BuildPlans()` to create initial and optimized logical/physical plans, run `QueryPlanner`, and store `BuildItems.PlanningResult`, `BuildItems.PlanningText`, optimizer trace text, and `BuildItems.PhysicalPlan`.
3. Calls `BuildExecutionInspection()` to lower the physical plan into initial Execution IR, run `ExecutionIrOptimizer`, and store optimized Execution IR snapshots.
4. Calls `BuildWithIrRenderer()` to adapt compiler state into an immutable `TargetRenderRequest` and render through `ExecutionTargetCatalog.Render`, which validates requirement kinds plus type/callable portability before invoking the backend. `TargetRenderInputBuildContext` carries common adapter facts, `TargetRenderInputCompilerState` carries converter-only compiler state, and `CSharpClrTargetComposition` alone maps them into `CSharpClrRenderInputs`. The common request uses neutral `CompilationUnitName` identity.
5. Fails fast if a physical plan is missing; do not add or restore a fallback to `ToCSharpRewriteTreeVisitor`.

Optimization responsibility is layered: `BuildPlans()` selects logical meaning and delegates query-level strategy/property decisions to `QueryPlanner`, `BuildExecutionInspection()` makes executable operations and runtime metadata explicit, and `BuildWithIrRenderer()` faithfully emits that Execution IR before readability-only codegen passes. Do not introduce renderer-only performance strategy decisions in converter orchestration, and do not restore lowerer self-planning or legacy codegen fallback paths.

The target boundary is `AST -> logical -> physical -> execution IR -> render phase -> finalization/export phase -> packaging phase`, with an optional inspection phase and an optional activation phase. Pure target contracts live in `Musoq.Targets.Abstractions`; Execution IR-bound SPI contracts live in `Musoq.Targets.Execution`; analyzer implementations live in `Musoq.Targets.Execution.Analysis`; the current production target lives in `Musoq.Targets.CSharpClr`. Converter owns the internal phase-based `ExecutionTargetDescriptor`, catalog, composition adapters, and CSharp artifact loader. Rendering, finalization, and inspection must dispatch through catalog methods so every phase output is checked against the selected target. Inspection and CLR activation are optional. There is no public target-selection API or public external target SPI.

This is converter-owned internal `ExecutionTargetDescriptor` composition, not a public external SPI. Descriptor adapters map `TargetFinalizationOptionsContext` into target-owned options such as `CSharpClrFinalizationOptions`; do not put PDB or other target-specific flags back into neutral finalizer contracts.

Target readiness rules:
- `ExecutionTargetCapabilities` must reject unsupported requirement kinds and unsupported `ExecutionPortableSymbolPortability` values before rendering.
- `ExecutionOperationCatalog` assigns one stable `ExecutionOperationId` to every concrete execution node and expression. `ExecutionTargetCatalog.Render` must validate the exhaustive `ExecutionTargetOperationReport` and `ExecutionSemanticsContract` before invoking a backend; targets must declare operation and semantics support explicitly.
- `ExecutionTargetReadinessAnalyzer` classifies compatibility requirements plus `TargetRuntimeContract` runtime services into future browser/source, bytecode VM, and interpreter blockers. Evaluate broad readiness categories independently from type/callable symbol portability; `ExecutionTargetCapabilities` enforces both before rendering. Readiness blockers are diagnostics and must not be encoded as host ABI imports.
- `TargetRuntimeContract` is the internal inventory for source access, plugin calls, row/table shapes, null behavior, cancellation, diagnostics, and profiling. Build or extend it before adding target-specific runtime assumptions.
- `TargetExportArtifact` is the neutral shape for future source/binary/host-import/entrypoint outputs; public artifact APIs still accept only C# CLR artifacts.
- The fake non-CLR pipeline harness must keep exercising real `TransformTree` request creation with the test-owned `TestExecutionTargetIds.TestOnlyNonClr`; do not replace it with registry-only tests or add the fake id to production target ids.
- `TargetRenderResult` is artifact-first. C# query-method metadata and readability traces belong on `CSharpRenderedQueryArtifact` and must flow through `CSharpClrArtifactCompatibility`, not through the common render result.
- `TargetRenderResult` and finalization results use invariant-checked success/failure factories with structured `TargetDiagnostic` values. Expected capability and lowering failures are diagnostics; programming-contract violations still throw.
- `TargetRenderRequest`, target-specific render inputs, and portable export artifacts should stay deeply immutable. Freeze mutable lists, dictionaries, and byte arrays at construction.
- Cache values and compiled artifact loading must move through target-aware executable artifacts. Do not store bare CLR `Type` as the universal executable representation.
- Generated C# formatting must move through `ExecutionTargetCatalog.InspectArtifact` or `TryInspectArtifact`; `QueryInspectionResult.GeneratedCSharpCode` is only a C# compatibility surface.
- `IRenderedQueryFinalizer` must take `TargetFinalizationOptions`; do not add target-neutral `emitPdb` or `EmitPdb` flags.
- Export-only future targets do not need an activation phase. Public execution paths resolve `ExecutionTargetCatalog.ResolveActivator` and must reject non-activatable targets before CLR casts, assembly loading, or runnable creation.
- Target phase lookup belongs only in `ExecutionTargetCatalog`; avoid adding registry facades or target switches in shared pipeline stages.
- Descriptor-owned `RenderedArtifactBuildContribution` values carry legacy query-method metadata, generated-source hashes, and readability traces. `TransformTree.ExecutionIr` must not import `Musoq.Targets.CSharpClr` or call `CSharpClrArtifactCompatibility`.
- `TargetArtifactSemanticFacts` is built from `BuildItems` before packaging. `TargetArtifactPackagingContext` and `TargetArtifactPackage` are the internal reusable artifact boundary and must not receive the full converter build bag. Prefer portable semantic views such as `PortableOutputTypeName`, `PortableScriptParameters`, `PortableScriptVariables`, `PortableUsedColumns`, and `PortableSourcePlanSignatures`; CLR/schema-shaped compatibility facts exist only for CSharpClr compatibility and hash stability. CSharpClr package factories should use `CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(...)`; portable export package factories should call `TargetArtifactPackage.CreatePortableExportPackage(...)` directly without adding a generic wrapper. Public compiled artifacts are still CSharpClr-only; shared artifact support should consume packages and reject non-CSharp packages before DLL/PDB, CLR type, or assembly-loading assumptions.
- Compatibility, runtime contract, and readiness analysis are built once while creating the render request and carried through rendering artifacts into packaging. Do not recompute target analysis inside artifact packaging.
- Public CSharp artifact loading and validation belongs in `CSharpClrCompiledArtifactLoader`; `CompileTargetPackageWithDiagnostics` must stay target-neutral and must not perform CLR assembly loading, type lookup, or activation.
- `InstanceCreator.CompileTargetPackageWithDiagnostics` is the internal package e2e compiler for render/finalize/package with optional inspection, with table and typed-output paths. Fake non-CLR e2e tests should use it instead of manually constructing packaging contexts.
- Portable symbol records and portability enums live in `Musoq.Targets.Abstractions`; portability is classified as `Portable`, `HostImport`, or `ClrOnly`. Evaluator `IR/Execution/Portability` owns the CLR-backed `ExecutionPortableSymbolFactory` and explicit `ExecutionPortableSymbolCatalog`; classify `LibraryBase` and aggregate-attributed callables by type/attribute, never namespace prefix. Unknown CLR fallback must remain an explicit `ClrOnly` readiness blocker.
- All types reachable through the public `ExecutionPlan` contract graph flow as `ExecutionTypeRef`, including expressions, variables, row shapes, source bindings, aggregates, indexes, windows, and captures. Shared converter and analysis code should consume stable portable identity; generated C# lowering may access the internal CLR sidecar only through the CSharp-owned `RequireClrType` helper.
- Execution call sites use `ExecutionCallableRef`; do not expose `MethodInfo` from method calls, aggregate operations, or plugin windows. Analysis consumes the portable callable descriptor, evaluator lowering/optimization may use the internal sidecar, and CSharp rendering must use `RequireClrMethod`.
- Execution literals and constant `IN` sets use immutable `ExecutionConstantValue`, never public `object` payloads. Preserve exact canonical encodings and report unsupported `ClrOnly` values through target compatibility/readiness analysis.
- `ExecutionRawExpression` is retired. Every evaluator expression must have explicit portable lowering, and an unknown expression must fail before rendering rather than carrying an evaluator object into a target.
- `TargetHostAbiInventory` records actual host imports for source access, plugin invocation, row/table transfer, null/type coercion, cancellation, diagnostics, and profiling. It must not contain readiness/`ClrOnlySymbol` blockers. `TargetHostAbiImport` entries use typed `TargetHostAbiImportDetails`, derived immutable `Attributes`, and a positive `ContractVersion`; target-specific extensions go through `TargetHostAbiImport.CreateCustom(...)`.
- Fake non-CLR e2e coverage must include a callable/plugin query through `CompileTargetPackageWithDiagnostics` so callable compatibility requirements, plugin runtime-contract entries, typed `TargetPluginInvocationAbiDetails`, package ABI metadata, and future-target readiness blockers remain covered end to end.
- `Musoq.Targets.TestPortable` is a separate test-only lowering assembly with no Converter, CSharpClr, Roslyn, reflection activation, or analysis implementation dependency. It lowers a declared subset to `PortableSubsetProgram`, executes it with `PortableValue`, and must never be production registered. This is an intentional breaking change to the public-looking Execution IR surface, not a public target selector or third-party SPI.
- `TargetContractVersions` independently versions Execution IR, host ABI, and target package format. Keep version checks at render/package boundaries and manifests deterministic. Do not couple these internal versions to the legacy compiled artifact version; the public artifact format remains version `2` until a deliberate public migration.

Optimizer trace text is part of inspection output. It should preserve changed/no-op pass decisions and analysis-fact consumption, recomputation, and invalidation counts from `OptimizationContext`; converter orchestration should not rewrite those diagnostics.

Source-aware planner facts flow through `BuildItems.PlanningResult` and `BuildItems.PlanningText`. Source-local ordering/slicing uses the public stateless planning contract: `ISchema.DescribeSource`, `ISchema.TryPlanSource`, and `SourceExecutionContext.Plan`. Do not add runtime-side query state; accepted source plans must be immutable data passed back to execution.

### BuildItems

`BuildItems` is the shared state object that flows through the chain. It accumulates:
- The AST (`RootNode`)
- Schema metadata
- Logical and physical plans (`LogicalPlan`, `PhysicalPlan`)
- Initial/optimized logical, physical, and Execution IR snapshots
- Optimizer trace text
- Pipeline metadata (`PipelineScope`, inferred columns, used columns)
- Generated Roslyn compilation artifacts
- Compiled assembly bytes
- Compilation diagnostics and errors

## Key Classes

| Class | Purpose |
|-------|---------|
| `InstanceCreator` | **Main API entry point** — `CompileForExecution()` compiles SQL to a `CompiledQuery` |
| `BuildResult` | Wraps the final output of the build chain |
| `QueryInspectionResult` | Wraps plan objects, readable plan text, and generated C# without executable bytes |
| `ILoggerResolver` | Interface for providing loggers to the compilation pipeline |

## API Usage

```csharp
// Primary compilation and execution flow
var compiledQuery = InstanceCreator.CompileForExecution(
    "SELECT Name, Count(*) FROM #test.data() GROUP BY Name",
    Guid.NewGuid().ToString(),    // unique assembly name
    schemaProvider,                // ISchemaProvider implementation
    loggerResolver);               // ILoggerResolver implementation

var results = compiledQuery.Run(); // Execute and get table results
```

```csharp
// Inspection flow: plan/code artifacts without DLL/PDB emission
var inspection = InstanceCreator.CompileForInspection(
    "SELECT Name, Count(*) FROM #test.data() GROUP BY Name",
    Guid.NewGuid().ToString(),
    schemaProvider,
    loggerResolver);

var logicalPlan = inspection.LogicalPlanText;
var planningText = inspection.PlanningText;
var physicalPlan = inspection.PhysicalPlanText;
var generatedCode = inspection.GeneratedCSharpCode;
```

### CompileForExecution Parameters

| Parameter | Type | Purpose |
|-----------|------|---------|
| `query` | `string` | SQL query text |
| `assemblyName` | `string` | Unique name for the generated assembly (use `Guid.NewGuid().ToString()`) |
| `schemaProvider` | `ISchemaProvider` | Provides schema definitions and data sources |
| `loggerResolver` | `ILoggerResolver` | Provides logging during compilation and execution |

## Dependencies

```
Musoq.Converter
├── Musoq.Evaluator  (AST visitors, planning, Execution IR, runtime contracts, legacy interpreter/compiler services)
├── Musoq.Targets.Abstractions (internal target contracts)
├── Musoq.Targets.Execution    (Execution IR-bound target SPI)
├── Musoq.Targets.Execution.Analysis (ExecutionPlan analyzer/builders used by converter)
├── Musoq.Targets.CSharpClr    (current production C# CLR target)
├── Musoq.Schema     (ISchemaProvider and source contracts)
├── Musoq.Parser     (lexer, parser, AST nodes)
└── Musoq.Plugins    (built-in SQL functions)
```

## Development Workflow

### Testing

```bash
# Run converter tests
dotnet test src/dotnet/Musoq.Converter.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Common Modifications

**Modifying the build chain:**
1. Each step is a class extending `BuildChain`
2. Steps are linked in `InstanceCreator.cs` — order matters
3. Plan construction and IR rendering currently happen inside `TransformTree`; keep orchestration there unless a broader build-chain refactor is intended
4. Adding a new step: create a class extending `BuildChain`, insert at the correct position in the chain
5. After changes, run both converter and evaluator tests

**Modifying planner integration:**
`BuildPlans()`, `BuildExecutionInspection()`, and `BuildWithIrRenderer()` are private helpers inside `TransformTree`; modify `TransformTree.cs` itself rather than overriding them.
- Change AST-to-plan wiring inside `TransformTree.BuildPlans()`.
- Change logical-plan state carried through the pipeline via `BuildItems.LogicalPlan`.
- Change physical-plan state carried through the pipeline via `BuildItems.PhysicalPlan`.
- Change Execution IR lowering inside `TransformTree.BuildExecutionInspection()` only when the physical-to-execution handoff changes.
- Change IR rendering orchestration inside `TransformTree.BuildWithIrRenderer()` only when renderer construction or metadata handoff changes.
- Read the repository-root `musoq_enchanced_architecture.md` before touching planner, Execution IR, or renderer routing.

**Modifying the API surface:**
- `InstanceCreator` is the public-facing API — changes here affect all consumers
- `ILoggerResolver` is implemented by consumers — changes are breaking

### Impact of Changes

Converter is the entry point for the entire pipeline. Changes here can affect:
- All downstream consumers who call `InstanceCreator.CompileForExecution()`
- The order and configuration of compilation phases
- Logical and physical plan availability to tests and analysis tooling
- Execution IR and renderer metadata handoff, including inferred columns and scope
- Run converter tests: `dotnet test src/dotnet/Musoq.Converter.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- Run evaluator tests for integration coverage: `dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
