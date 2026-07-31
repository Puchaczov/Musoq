# Execution Target Boundary

This directory owns the internal target/runtime composition boundary for executable query output. There is no public target-selection API yet; the only production target is `CSharpClr`.

The pipeline boundary is:

```text
AST -> logical -> physical -> execution IR -> render phase -> finalization/export phase -> packaging phase
                                                        \-> optional inspection phase
                                                        \-> optional activation phase
```

## Ownership

- `Musoq.Targets.Abstractions` contains pure target contracts: `ExecutionTargetId`, rendered/executable artifact bases, `TargetFinalizationOptions`, target-neutral finalization/export/inspection result shapes, and portable export artifacts. It must not depend on `Musoq.Converter`, `Musoq.Evaluator`, `Musoq.Schema`, Roslyn, CLR reflection `Type`/`Assembly`, `Assembly.Load`, or `Activator.CreateInstance`.
- `Musoq.Targets.Execution` is the execution-IR-bound SPI. It owns `IQueryExecutionBackend`, `IClrExecutableQueryActivator`, `TargetRenderRequest`, artifact-first `TargetRenderResult`, `TargetBackendRenderInputs`, `QueryRuntimeBinding`, capability validation, compatibility reports, runtime contracts, readiness report contracts, and `TargetHostAbiInventoryBuilder`. Contracts that must reference `ExecutionPlan`, runnable interfaces, or schema runtime binding live here instead of polluting pure abstractions.
- `Musoq.Targets.Execution.Analysis` contains converter-used plan-walking implementations: `ExecutionTargetCompatibilityAnalyzer`, `ExecutionTargetFeatureAnalyzer`, `TargetRuntimeContractBuilder`, and `ExecutionTargetReadinessAnalyzer`. Target packages should depend on `Musoq.Targets.Execution` contracts, not this implementation package.
- `Musoq.Targets.Abstractions` owns the immutable portable symbol records and portability enums. Evaluator execution portability code owns the CLR-backed `ExecutionPortableSymbolFactory` and explicit `ExecutionPortableSymbolCatalog` that create those records while lowering and analyzing plans.
- `Musoq.Targets.CSharpClr` contains the current production target: C# rendering, `CSharpClrRenderInputs`, `CSharpClrFinalizationOptions`, Roslyn finalization, CLR executable artifacts, CLR activation, generated C# inspection, and `CSharpClrArtifactCompatibility`.
- `Musoq.Converter.ExecutionTargets` contains the internal `ExecutionTargetCatalog` composition root and `CSharpClrTargetComposition`.
- Shared stages before the backend produce target-neutral query meaning: AST rewrites, logical plans, physical plans, and optimized execution IR.
- `IQueryExecutionBackend` is the render phase. It renders optimized execution IR into a target-specific `RenderedQueryArtifact`.
- `IRenderedQueryFinalizer` is the finalization/export phase. It takes target-specific `TargetFinalizationOptions` and turns a target-specific rendered artifact into an `ExecutableQueryArtifact`.
- `IClrExecutableQueryActivator` is an optional CLR activation phase. It turns a target-specific executable artifact into the current public runnable contracts only for targets that can run as in-process .NET runnables.
- `IRenderedQueryInspector` is an optional inspection phase. It formats target-specific rendered artifacts for inspection/debugging.
- `ExecutionTargetCatalog` is the only descriptor and phase dispatch point over `ExecutionTargetId`. Shared code uses `ExecutionTargetCatalog.Render(...)`, `ExecutionTargetCatalog.FinalizeArtifact(...)`, and `ExecutionTargetCatalog.InspectArtifact(...)`; each validates that phase outputs stay on the selected target.
- `CSharpClrTargetComposition` is the only converter owner that constructs concrete C# target components and `CSharpClrRenderInputs`. Shared converter stages must not instantiate C# backend/finalizer/activator/inspector types directly.
- Compatibility members such as `BuildItems.Compilation`, `BuildItems.AccessToClassPath`, `BuildItems.DllFile`, and `BuildItems.PdbFile` stay synchronized for the current C# CLR path, but shared pipeline stages should not grow new dependencies on those C# artifacts.

## Phase-Based Descriptor Catalog

`ExecutionTargetCatalog` is descriptor based. `ExecutionTargetDescriptor` is converter-owned internal composition, not a public external SPI. Each target is registered as a descriptor containing a target id plus optional phase components:

- target id
- render phase
- finalization/export phase
- inspection phase
- optional CLR activation phase
- backend capabilities when a render phase exists
- target-owned render input factory using `TargetRenderInputBuildContext`
- target-specific finalization option factory
- target-owned render build contribution factory for query-method metadata, generated-source hashes, and optional readability traces
- target-owned reusable artifact package factory using `TargetArtifactPackagingContext`

Production registration contains only `ExecutionTargetIds.CSharpClr`. Tests can install scoped temporary descriptors using test-owned ids such as `TestExecutionTargetIds.TestOnlyNonClr`, but production build chains still force `CSharpClr` and no public target selector exists.

`CSharpClr` registers all phases. A future export-only JavaScript, WASM, Python, or bytecode target may register only render, finalization/export, and packaging; inspection and activation are independently optional. Explicitly resolving an unsupported phase must fail through `ExecutionTargetCatalog` with a deterministic target-aware diagnostic before any C# compatibility cast, Roslyn emit, DLL/PDB load, CLR type lookup, `Assembly.Load`, or `Activator.CreateInstance`.

Do not add partial backend/finalizer/activator/inspector override hooks or registry facades. A future target must exercise the same phase-based descriptor path as the current C# target.

## Render, Finalization, And Export Contracts

- `TargetRenderRequest` is the immutable target-facing render input. Its common contract carries only target id, neutral `CompilationUnitName` identity, optimized `ExecutionPlan`, operation and feature reports, compatibility report, runtime contract, script binding names, reference inventories, and `TargetBackendRenderInputs`. Assembly and namespace identity belong to CSharp-specific inputs.
- `TargetRenderInputBuildContext` is a narrowed converter-owned DTO used only by descriptor render input factories. Its common facts are compilation options, result mode, script binding names, target reference inventory names, and render options. `TargetRenderInputCompilerState` is an immutable converter adapter snapshot for concrete composition code; it replaces the old string-keyed object context bag and never crosses into a target backend.
- `CSharpClrTargetComposition` maps `TargetRenderInputCompilerState` into `CSharpClrRenderInputs`, where CLR-only `Type`, `Assembly`, `Scope`, interpreter source, script definitions, output type, reference assemblies, assembly name, namespace name, and compilation options belong. Future target composition maps the same compiler snapshot into its own input type without adding target branches to shared `TransformTree` code.
- `TargetRenderResult` is artifact-first: it carries only the target-owned rendered artifact. C# query-method metadata, generated-source hashes, and readability traces live on `CSharpRenderedQueryArtifact` and are exposed to legacy converter surfaces through descriptor-owned `RenderedArtifactBuildContribution` logic, not shared `TransformTree` C# branches.
- `IRenderedQueryFinalizer` accepts `TargetFinalizationOptions`, not a C#-specific PDB flag. `TargetFinalizationOptionsContext` is converter-owned staging context used only by descriptor finalization option factories. `CSharpClrTargetComposition` maps `BuildItems.EmitPdb` to `CSharpClrFinalizationOptions`; non-CLR export targets can use `TargetFinalizationOptions.Empty` or define their own option record.
- `TargetFinalizationResult` is target-neutral and enforces that success has one matching-target executable artifact, failure has none, and successful results contain no error diagnostics. Roslyn `EmitResult` remains only a C# compatibility member through `CSharpClrArtifactCompatibility`.
- `TargetExportArtifact` is the target-neutral portable export shape for future JavaScript, WASM, Python, or bytecode outputs: source files, binary blobs, host imports, runtime entrypoints, runtime services, diagnostics metadata, and profiling metadata.
- `TargetArtifactSemanticFacts` is built once from `BuildItems` before packaging. `TargetArtifactPackagingContext` carries those explicit immutable facts rather than the full converter build bag. Future export targets should consume portable semantic views such as `PortableOutputTypeName`, `PortableScriptParameters`, `PortableScriptVariables`, `PortableUsedColumns`, `PortablePipelineInferredColumns`, and `PortableSourcePlanSignatures`; CLR/schema-shaped compatibility fields exist only for current C# artifact compatibility and hash stability.
- `TargetArtifactPackagingContext` and `TargetArtifactPackage` are converter-owned reusable artifact packaging contracts. CSharp CLR packages contain DLL/PDB blobs and legacy metadata; export-only targets can package source files, binary blobs, entrypoints, runtime services, diagnostics metadata, and a structured `TargetHostAbiInventory` without pretending to be loadable CLR assemblies.
- `TargetArtifactPackage` has a private constructor. `CreateValidated(...)` owns universal container invariants, `CSharpClrTargetPackageFactory.CreateClrAssemblyPackage(...)` owns CSharpClr reusable artifact validation, and `TargetArtifactPackage.CreatePortableExportPackage(...)` owns portable export validation. Portable exports may expose either `TableQuery` or `TypedQuery` entrypoints and may declare zero host runtime services when the target provides everything internally.
- Compatibility, runtime-contract, and readiness reports are built once during render request creation, stored with render artifacts, and reused during package creation. Packaging must not recompute target analysis from `ExecutionPlan`.
- `InstanceCreator.CompileTargetPackageWithDiagnostics` is the internal package compiler used by public CSharp artifact compilation and the fake non-CLR pressure harness. It drives render, finalization/export, optional inspection, and packaging through the same descriptor path, including a typed-output overload; public `CompileArtifactWithDiagnostics` delegates to it with `CSharpClr` and then converts only CSharp CLR packages into `CompiledQueryArtifact`.
- Public compiled-artifact APIs currently convert only `CSharpClr` CLR assembly packages into `CompiledQueryArtifact`. Non-CLR packages must fail before DLL/PDB assumptions, CLR type lookup, assembly loading, or activation.
- Public CSharp artifact loading and validation is isolated in `CSharpClrCompiledArtifactLoader`. `CompileTargetPackageWithDiagnostics` is target-neutral package compilation and must not perform CLR assembly loading, type lookup, or activation.
- `TargetRenderRequest`, `CSharpClrRenderInputs`, and `TargetExportArtifact` freeze mutable inputs. Lists, dictionaries, and byte arrays must not leak caller-side mutation into shared containers.
- Public compiled-artifact APIs currently reject non-`CSharpClr` artifacts with clear diagnostics.

## Current Target

`CSharpClrExecutionBackend` is the only production backend. `ExecutionTargetCatalog.Render(...)` validates the current CLR requirements reported by `ExecutionTargetCompatibilityAnalyzer` and `TargetRuntimeContract` before the backend is invoked. The backend consumes the descriptive `TargetRuntimeContract`, renders execution IR through the C# renderer, and produces `CSharpRenderedQueryArtifact`.

`CSharpClrRenderedQueryFinalizer` is the only production finalization phase. It emits the Roslyn compilation into CLR DLL/PDB bytes and produces `ClrAssemblyExecutableArtifact`.

`ClrAssemblyExecutableActivator` is the only production activation phase. It owns CLR assembly loading, `Activator.CreateInstance`, loaded CLR executable artifacts, and runtime binding to `ITableRunnable` / `ITypedRunnable<TOut>`. Future export-only targets must not fake this phase just to participate in rendering or export.

`CSharpRenderedQueryInspector` is the only inspector. It owns generated C# formatting for `QueryInspectionResult.GeneratedCSharpCode` and debug source dumping.

## Roslyn And CLR Dependency Budget

The target boundary deliberately keeps generated-query C# lowering and executable CLR activation in `Musoq.Targets.CSharpClr`.

- `CSharpRenderer`, `ExecutionCSharpRenderer`, generated C# syntax lowering, and query-artifact Roslyn emit belong in `Musoq.Targets.CSharpClr`.
- CLR executable activation belongs in `ClrAssemblyExecutableActivator`.
- Target-neutral contracts must not expose `bool emitPdb`, `EmitPdb`, `ITableRunnable`, or `ITypedRunnable<T>`.
- `Musoq.Converter` may reference Roslyn only through current C# compatibility shims: `BuildItems`, `RenderingBuildArtifacts`, `CompilationBuildArtifacts`, and `BuildItems.Rendering`.
- `Musoq.Targets.Abstractions` must not reference Roslyn, evaluator/schema contracts, CLR reflection `Type`/`Assembly`, `Assembly.Load`, or `Activator.CreateInstance`.
- `Musoq.Evaluator` still has intentional legacy C# compiler seams for interpreter compiler services, syntax bridge metadata consumed by planning/lowering, and visitor code-generation helpers.
- Generated-query readability optimization belongs to `Musoq.Targets.CSharpClr/Optimization/Codegen`, not evaluator optimizer contracts.
- `Musoq.Evaluator` still has explicit CLR/reflection activation exceptions for interpretation assembly loading, compiled interpretation schema construction, SQL-style default value construction in `SafeArrayAccess`, semantic exception construction, and window aggregate capability provider construction.

Those evaluator exceptions are not target-extension points. New target rendering, finalization, activation, or inspection code belongs in a target package and must be reached through `ExecutionTargetCatalog`.

## Compatibility And Contract Inventory

- `ExecutionTargetCapabilities` is the descriptor-facing capability gate in `Musoq.Targets.Execution`. Requirement kinds plus `SupportedTypeSymbolPortabilities` and `SupportedCallableSymbolPortabilities` are enforced in `ExecutionTargetCatalog.Render(...)` before rendering starts.
- `Musoq.Targets.Execution` owns capability, compatibility, runtime-contract, and readiness report contracts.
- `Musoq.Targets.Execution.Analysis` owns the plan-walking analyzer/builders. `ExecutionTargetCompatibilityAnalyzer` reports both stable requirement strings and portable symbols for CLR types, generated rows, and callable methods. The symbol records and `ExecutionPortableSymbolPortability` values (`Portable`, `HostImport`, or `ClrOnly`) are pure abstraction contracts; unknown CLR fallback must stay explicit so future targets can reject it deterministically.
- `ExecutionTargetReadinessAnalyzer` maps compatibility requirements plus `TargetRuntimeContract` services into readiness blockers for placeholder future families: browser-like source output, bytecode VM output, and interpreter-like output. Readiness profiles evaluate broad requirement categories separately from type/callable symbol portability, so `ClrOnly` symbols cannot be hidden by a broad supported category unless a profile explicitly opts into `ClrOnly` symbols.
- `TargetRuntimeContract` records the runtime services a future target must model: source access, plugin invocation, row/table shapes, null behavior, cancellation, diagnostics, and profiling hooks.
- Evaluator `IR/Execution/Portability` owns the explicit `ExecutionPortableSymbolCatalog` used by `ExecutionPortableSymbolFactory`. Portable primitives, known collection shapes, Musoq host runtime concepts, `LibraryBase` callables, and aggregate-attributed callables must be classified there; do not infer plugin identity from namespaces. Unknown CLR types or methods remain `ClrOnly` with a fallback reason.
- The full `ExecutionPlan` contract graph carries `ExecutionTypeRef`, including expressions, variables, row shapes, source bindings, aggregate metadata, indexes, windows, and captured locals. No public execution-plan property or constructor exposes CLR `Type`/`Assembly`. The reference exposes stable portable identity, keeps its CLR binding internal to evaluator-owned lowering/optimization, and lets target analysis consume the portable descriptor directly. CSharp lowering must obtain the sidecar only through the CSharp-owned `RequireClrType` compatibility helper.
- Method, aggregate, and plugin-window call sites carry `ExecutionCallableRef`, whose stable signature records callable classification, declaring/parameter/return symbols, generic arity, and invocation mode. `MethodInfo` is an internal evaluator sidecar; target analysis consumes `PortableCallable`, and CSharp lowering accesses reflection only through `RequireClrMethod`.
- Execution literals and hoisted constant `IN` sets carry immutable `ExecutionConstantValue` payloads. Integer widths, IEEE floating bits, decimal words, UTF-16 units, temporal ticks/kinds/offsets, RFC 4122 GUID bytes, enum identity, and nulls are canonical; unsupported payloads are explicit `ClrOnly` sidecars and readiness blockers.
- `TargetHostAbiInventoryBuilder` derives actual host ABI imports from `TargetRuntimeContract`: source access, plugin invocation, row/table shape transfer, null/type coercion, cancellation, diagnostics, and profiling. Readiness blockers are diagnostics, not imports, and must not be serialized as `ClrOnlySymbol` ABI entries. Each import carries typed `TargetHostAbiImportDetails`, a readable name, contract string, positive `ContractVersion`, and derived immutable string `Attributes`; target-specific extensions must use `TargetHostAbiImport.CreateCustom(...)`. The inventory is a canonical set: equivalent `(Kind, Name)` imports collapse, while conflicting definitions fail explicitly. Plugin invocation names include the callable stable signature so overloads cannot collide, and the plugin contract is `plugin-invocation-v2`.
- `TargetExportArtifact` models future portable outputs: source files, binary blobs, host imports, runtime entrypoints, runtime service requirements, and diagnostics metadata.
- The current C# backend accepts that contract but continues to use existing CLR runtime behavior.
- Cache values and artifact loading use target-aware executable artifacts; public artifact APIs still accept only C# CLR artifacts.

## Portable Execution Core

Execution IR is now the portable query contract consumed by target lowerers. Its public contract graph must remain free of CLR reflection and arbitrary payloads:

- `ExecutionTypeRef` carries stable portable type identity and an evaluator-internal CLR `Type` sidecar. Public plan properties expose the reference, never `Type`, `Assembly`, or reflection members. Evaluator lowering/optimization may use the sidecar while constructing or optimizing plans; CSharp lowering accesses it only through `RequireClrType`.
- `ExecutionCallableRef` carries stable callable kind, declaring type, parameter and return identities, generic arity, and invocation mode. Its `MethodInfo` sidecar is internal; CSharp lowering accesses it only through `RequireClrMethod`.
- `ExecutionConstantValue` is the canonical immutable literal representation. It preserves integer width/sign, IEEE bits, decimal words, UTF-16 code units, temporal ticks and kind/offset, RFC 4122 GUID bytes, enum identity, and null. Unsupported values retain an internal `ClrOnly` sidecar and must become readiness blockers.
- `ExecutionRawExpression` has been removed. Every evaluator `IrExpression` must have an explicit `ExecutionExpressionConverter` registration and lower to a typed execution expression. Unknown expression types fail deterministically during lowering.
- Every concrete `ExecutionNode` and `ExecutionExpression` has one stable `ExecutionOperationId`. `ExecutionOperationCatalog` is exhaustive, and `ExecutionTargetOperationReport` records deterministic operation counts for capability validation.

This is an intentional breaking change to the public-looking Execution IR surface. Removed `Type`, `MethodInfo`, and `object` members must not be restored as obsolete compatibility properties. The supported high-level `InstanceCreator` APIs remain behavior-compatible; the internal/in-repo target SPI is not a third-party extension API.

## Semantics, Diagnostics, And Versions

`ExecutionSemanticsContract.Version1` defines the current `CSharpClr` behavior. Targets must explicitly advertise supported semantics versions as well as supported operations, requirement kinds, symbol portability, and runtime services. Version 1 fixes SQL three-valued null logic and ordering, unchecked-width runtime integer add/subtract/multiply, divide/modulo exception behavior, checked constant-fold diagnostics, checked aggregate overflow, IEEE floating behavior, CLR decimal behavior, ordinal string equality/ordering/hashing, temporal/GUID/timespan behavior, invariant strict casts, and equality used by grouping, distinct, joins, and set operations. Operation-specific arithmetic differences remain explicit instead of being collapsed into one global rule.

`ExecutionTargetCatalog.Render(...)` validates the request's IR version, `ExecutionTargetOperationReport`, `ExecutionTargetFeatureReport`, exact `ExecutionSemanticsContract` fingerprint, compatibility requirements, symbol portability, runtime contract, and host ABI version before invoking a backend. Expected capability or lowering failures use invariant-checked `TargetRenderResult` success/failure factories and structured `TargetDiagnostic` values. Finalization uses the same diagnostic model; programming-contract violations still throw.

`TargetContractVersions` independently versions Execution IR, host ABI, and package format. `ExecutionPlan`, `TargetRenderRequest`, and `TargetArtifactPackage` carry the applicable versions, and deterministic package manifests include them with the exact execution-semantics fingerprint. ABI definitions also contribute a canonical fingerprint to the manifest. Compilation cache keys include both target id and semantics fingerprint. Contract version changes require an explicit migration and conformance update. This milestone does not change NuGet/package versions, and the legacy public artifact format remains version `2`.

## Standalone Portable Test Target

`Musoq.Targets.TestPortable` is a separate test-only target assembly. It references `Musoq.Evaluator`, `Musoq.Targets.Abstractions`, and `Musoq.Targets.Execution`, but not Converter, CSharpClr, Roslyn, reflection activation, or the analysis implementation package. It is never registered by production composition and has no CLR activation phase.

The target lowers its declared operation subset to immutable `PortableSubsetProgram` instructions, finalizes a deterministic instruction manifest, and can provide portable inspection text. Its test-only interpreter uses a `PortableValue` union rather than CLR `object`; execution through a rendered artifact validates its canonical `TargetHostAbiInventory` before interpreting instructions. Conformance e2e tests compile through the real catalog pipeline and compare supported query results and exceptions with CSharpClr semantics version 1. Aggregate, join, CTE-index, window, and unsupported callable shapes must be rejected by capability validation before the backend runs.

## Test Pressure Harness

`TestExecutionTargetIds.TestOnlyNonClr` exists only in test infrastructure for the fake non-CLR pipeline harness. It proves the real internal build pipeline can carry non-C# rendered, finalization, export, optional inspection, typed entrypoints, zero-host-import packages, and package metadata internally. It is not production selectable and must not be added to production target ids.

Temporary test descriptors use atomically disposed linked scopes. Nested overrides restore the active outer descriptor, out-of-order disposal cannot resurrect registrations, and captured async contexts stop resolving a descriptor after its owning scope is disposed.

The fake target harness is export-only by default. It is expected to fail public activation/artifact APIs before Roslyn, DLL/PDB loading, CLR type lookup, CLR runnable contracts, or `Activator.CreateInstance`.

The fake non-CLR e2e coverage must include at least one callable/plugin query through `CompileTargetPackageWithDiagnostics` so callable symbols, plugin runtime-contract entries, typed `TargetPluginInvocationAbiDetails`, ABI package metadata, and future-target readiness blockers stay covered end to end.

## Future Target Rules

- New backends must declare capabilities for stable operations, execution semantics versions, requirement kinds, runtime services, and type/callable portability reported by target analysis before rendering.
- New target packages must register through a phase-based `ExecutionTargetDescriptor` and should not require converter shared-stage edits after their target id and composition file exist.
- New target packages must provide descriptor-owned render input factories when they need target-specific inputs; shared `TransformTree` code must not grow target-specific branches.
- New target packages must provide descriptor-owned render build contributions and artifact package factories when they expose legacy metadata or reusable export artifacts. Shared artifact support must consume `TargetArtifactPackage`, not target-specific rendered artifacts.
- New target package factories must use validated package helpers and carry the render-time compatibility/runtime/readiness analysis through packaging instead of rebuilding target analysis.
- New backends must produce their own rendered artifact type instead of adding non-C# state to `CSharpRenderedQueryArtifact`.
- New finalizers must accept only the rendered artifact type they own, consume target-specific finalization options, and produce their own executable or export artifact type.
- New export-only targets do not need an activation phase. New activators are required only when a target can produce current in-process .NET runnables; they must accept only the executable artifact type they own and must not route through CLR assembly loading unless the target is CLR-based.
- New inspectors must format only the rendered artifact type they own. Inspection is optional for package/export targets; public `GeneratedCSharpCode` remains a C# compatibility surface.
- New targets must use portable symbols, `TargetRuntimeContract`, `ExecutionTargetReadinessAnalyzer`, and `TargetExportArtifact` to declare runtime service and export needs before adding target-specific lowering.
- New export targets must attach the single typed `TargetHostAbiInventory` model to artifacts and packages so browser, WASM, Python, or bytecode hosts can see source, callable, row transfer, null/coercion, cancellation, diagnostics, and profiling imports before runtime implementation begins. Do not add a parallel host-import model.
- New package/e2e tests should exercise `CompileTargetPackageWithDiagnostics` instead of manually constructing `TargetArtifactPackagingContext`, except for narrow catalog/container unit tests.
- Shared pipeline stages must avoid adding C# assumptions, Roslyn assumptions, CLR assembly assumptions, or generated-source formatting assumptions.
- Inspection and artifact metadata must branch from `RenderedQueryArtifact` helpers, keeping `QueryInspectionResult.GeneratedCSharpCode` as a C#-backend compatibility surface only.
- Cache keys must include `ExecutionTarget` whenever executable output or rendered artifacts are reused.
- Cache values must be target-aware executable artifacts, not bare CLR `Type` values.
- Target dispatch must stay inside `ExecutionTargetCatalog`; do not reintroduce backend/finalizer/inspector/activator registry facades.
