# Musoq.Evaluator

Query execution engine - the largest and most complex module in Musoq. Handles AST transformation through a multi-phase visitor pipeline, Expression IR, logical and physical query plans, Execution IR, portable symbol creation, legacy interpretation-schema compilation services, and runtime execution. Pure portable symbol records live in `Musoq.Targets.Abstractions`. Target-facing runtime contract/readiness report types live in `Musoq.Targets.Execution`, plan-walking target analysis implementations live in `Musoq.Targets.Execution.Analysis`, and generated-query C# lowering lives in `Musoq.Targets.CSharpClr`.

## Internal Structure

```
Musoq.Evaluator/
├── Build/                              # Compilation chain
│   ├── BuildChain.cs                   # Chain of Responsibility base
│   ├── BuildItems.cs                   # Compilation artifacts container
│   ├── CreateTree.cs                   # Step 1: Lexer + Parser → AST
│   ├── CompileInterpretationSchemas.cs # Step 2: binary/text schema compilation
│   ├── TransformTree.cs               # Step 3: Visitor pipeline execution
│   ├── TurnQueryIntoRunnableCode.cs   # Step 4: Roslyn compilation → DLL
│   └── InterpreterCompilationUnit.cs  # Interpretation schema compilation unit
├── IR/                                 # Active planner, optimizer, and Execution IR pipeline
│   ├── Expressions/                    # Typed expression IR and expression rewriters
│   ├── Bindings/                       # OutputSchema, projected fields, aggregate/window bindings
│   ├── Logical/                        # LogicalNode tree and LogicalPlanBuilder
│   ├── Planning/                       # QueryPlanner, plan properties, and planner-owned strategy rules
│   │   ├── Cardinality/                # Cardinality estimation inputs
│   │   ├── ExecutionStrategies/        # Behavior-consuming execution strategy records
│   │   ├── RuntimeSettings/            # Source runtime settings planning
│   │   ├── SourcePlanning/             # Source plan request construction
│   │   └── Subqueries/                 # Subquery planning facts
│   ├── Physical/                       # PhysicalNode tree and strategy selection
│   ├── Optimization/                   # Logical, physical, and Execution IR optimizer passes
│   ├── SourcePlanning/                 # Predicate DTO conversion, comparison, and matching utilities
│   ├── Execution/                      # ExecutionNode tree, lowering dispatch/coordinators, and C# syntax bridge metadata
│   │   ├── Facts/                      # Execution IR analysis facts infrastructure
│   │   └── Lowering/                   # Aggregate, CTE, join, and window lowering coordinators
│   ├── Printing/                       # Plan diagnostic printing helpers
│   └── CodeGeneration/                 # Target-neutral render metadata and final projection/sink planning helpers
├── Visitors/                           # AST visitor implementations used before planning
│   ├── (see Visitor System section below)
│   ├── Helpers/                        # Visitor helper utilities
│   └── CodeGeneration/                 # Legacy visitor code-generation and interpretation-schema syntax helpers
├── Tables/                             # Runtime table types
│   ├── Table.cs                        # Main table implementation
│   ├── Row.cs                          # Row with Contexts[] array
│   ├── ContextMaterializer.cs          # Shared lazy row context materialization helpers
│   ├── DescriptionRows.cs              # Self-contained typed rows for DESC metadata output
│   ├── Column.cs                       # Column definition
│   ├── TableIndex.cs                   # Table indexing for joins
│   ├── TransientVariableSource.cs     # Temporary variable handling
│   ├── TransitionLibrary.cs           # Transition table function library
│   ├── TransitionSchema.cs            # Transition table schema
│   ├── VariableTable.cs               # Variable storage table
│   ├── IndexedList.cs                 # Indexed list for lookups
│   ├── Key.cs                          # Generic key type
│   └── IValue.cs                       # Value interface
├── Runtime/                            # Runtime compilation support
│   ├── MetadataReferenceCache.cs       # Caches assembly references for Roslyn
│   ├── RoslynSharedFactory.cs          # Shared Roslyn workspace/compilation
│   └── RuntimeLibraries.cs            # Runtime library references
├── TemporarySchemas/                   # Internal schema implementations
│   ├── DescSchema.cs                   # DESC command schema
│   ├── DynamicTable.cs                 # Dynamic table for runtime data
│   ├── TableMetadataSource.cs          # Table metadata source
│   └── TransitionSchemaProvider.cs    # Transition table schema provider
├── Helpers/                            # General utilities
├── Utils/                              # Additional utilities
├── Exceptions/                         # Evaluator-specific exceptions
├── Resources/                          # Embedded resources
├── Docs/                               # Internal documentation
├── Parser/                             # Parser integration
├── CompiledQuery.cs                    # Final executable query — Run() method
├── SchemaRegistry.cs                   # Schema registration and lookup
├── SchemaRegistration.cs               # Schema registration record
├── QueryAnalyzer.cs                    # Query analysis and metadata extraction
├── QueryAnalysisResult.cs             # Analysis result type
├── SemanticAnalysisException.cs       # Semantic error type
├── SemanticAnalysisResult.cs          # Semantic analysis output
├── CompilationOptions.cs              # Compilation configuration
├── DiagnosticContext.cs               # Diagnostic tracking
├── BaseOperations.cs                  # Common runtime operations
├── Operators.cs                        # Operator implementations
├── AliasGenerator.cs                  # Unique alias generation
├── AmendableQueryStats.cs             # Mutable query statistics
├── QueryStatsSnapshot.cs             # Immutable query stats snapshot
├── QueryPhase.cs                      # Compilation phase enum
├── QueryPhaseEventArgs.cs            # Phase event args
├── QueryPhaseEventHandler.cs         # Phase event handler
├── ParallelizationMode.cs            # Parallelization strategy enum
├── IRunnable.cs                       # Runnable query interface
├── RunnableDebugDecorator.cs          # Debug wrapper for runnable
├── NullLogger.cs                      # No-op logger
└── ExpandoObjectPropertyInfo.cs       # Dynamic property info
```

## Visitor System

The pre-plan visitor pipeline implements `IExpressionVisitor` from Musoq.Parser. These visitors normalize syntax, infer metadata, and produce the typed AST consumed by the IR planner. Shared parser-side traversal helpers live in `Musoq.Parser/Traversal`; use them for new common traversal behavior while keeping `Node.Accept(...)` and the current visitor entrypoints intact.

| Visitor Type | Purpose | Pattern |
|--------------|---------|---------|
| **Traverse Visitor** | Controls AST traversal order | Calls `node.Accept(innerVisitor)` for children |
| **Clone Visitor** | Creates modified AST copy | Pops children from stack, creates new nodes |
| **Rewrite Visitor** | In-place semantic changes | Modifies node properties or replaces nodes |

### Phase 1: Pre-Processing Visitors

| Visitor | Purpose |
|---------|---------|
| `DistinctToGroupByVisitor` | Rewrites `SELECT DISTINCT` as `GROUP BY` for unified handling |
| `DistinctToGroupByTraverseVisitor` | Traversal driver for DistinctToGroupByVisitor |
| `ExtractRawColumnsVisitor` | Collects all column references before type inference |
| `ExtractRawColumnsTraverseVisitor` | Traversal driver for ExtractRawColumnsVisitor |

### Phase 2: Metadata & Type Inference

| Visitor | Purpose |
|---------|---------|
| `BuildMetadataAndInferTypesVisitor` | **Core semantic analysis facade**: resolves schemas, infers types, validates methods, builds symbol tables through focused semantic services |
| `BuildMetadataAndInferTypesTraverseVisitor` | Traversal driver for BuildMetadataAndInferTypesVisitor |
| `BuildMetadataAndInferTypesVisitorUtilities` | Utility methods for type inference |
| `SchemaDefinitionVisitor` | Extracts `binary`/`text` schema definitions for interpretation schemas |

### Phase 3: Query Rewriting

| Visitor | Purpose |
|---------|---------|
| `RewriteQueryVisitor` | **Main AST transformer**: normalizes query structure, resolves aliases, prepares for code gen |
| `RewriteQueryTraverseVisitor` | Traversal driver for RewriteQueryVisitor |
| `RewriteWhereExpressionToPassItToDataSourceVisitor` | Predicate pushdown — extracts WHERE conditions safe for data source filtering |
| `RewriteWhereExpressionToPassItToDataSourceTraverseVisitor` | Traversal for predicate pushdown |
| `RewritePartsWithProperNullHandlingVisitor` | Adds proper null type information to NullNode |
| `RewritePartsWithProperNullHandlingTraverseVisitor` | Traversal for null handling |
| `RewritePartsToUseJoinTransitionTable` | Rewrites JOINs to use intermediate transition tables |
| `CloneQueryVisitor` | Base class for creating modified AST copies |
| `CloneTraverseVisitor` | Traversal driver for CloneQueryVisitor |
| `SubqueryToCteRewriteVisitor` | Rewrites subqueries as CTEs |
| `SubqueryToCteRewriteTraverseVisitor` | Traversal for subquery-to-CTE rewrite |
| `ConstantFoldingVisitor` / `ConstantFoldingTraverseVisitor` | Retired production AST folding visitor; keep out of `TransformTree` unless a future document update explicitly reopens AST ownership |
| `LogicalConstantFoldingPass` | Owns logical literal folding, expression simplification, and constant-expression diagnostics |
| `RewriteFieldOrderedWithGroupMethodCall` | Rewrites ordered fields with group method calls |
| `RewriteFieldWithGroupMethodCall` | Rewrites fields with group method calls |
| `RewriteFieldWithGroupMethodCallBase` | Base class for group method call rewriting |
| `RewriteToUpdatedColumnAccess` | Updates column access patterns |
| `RewriteToUpdatedColumnAccessTraverser` | Traversal for column access updates |

### Phase 4: Logical and Physical Planning

| Component | Purpose |
|-----------|---------|
| `ExpressionConverter` | Converts typed parser expressions into immutable `IrExpression` records |
| `LogicalPlanBuilder` | Lowers the normalized AST into `LogicalNode` relational operators |
| `LogicalOptimizer` | Runs named logical normalization and optimization pass groups, including logical constant folding, source/alias analysis facts, and dead CTE elimination |
| `LogicalPlanBuildTraverseVisitor` | Traversal driver for logical plan construction |
| `QueryPlanner` | Derives plan properties, records planning diagnostics, and routes logical plans into physical construction |
| `PhysicalPlanningPipeline` / `PhysicalOptimizer` | Builds initial physical plans and applies named physical optimization passes |
| `PhysicalPlanBuilder` | Constructs `PhysicalNode` trees from planner-owned strategy/property decisions |
| `PhysicalToExecutionPlanBuilder` | Dispatches physical strategies into focused lowering coordinators and shared lowering context |
| `ExecutionIrOptimizer` | Runs named Execution IR optimization passes after lowering |
| `Musoq.Targets.CSharpClr.Optimization.Codegen.CodegenReadabilityOptimizer` | Runs generated C# readability passes in the C# target before Roslyn compilation |
| `ExecutionNode` / `ExecutionExpression` | Model table, row, join, aggregate, window, and expression operations for code emission |
| `Musoq.Targets.Execution.Analysis.TargetRuntimeContractBuilder` | Builds target-neutral runtime service inventory from optimized Execution IR |
| `Musoq.Targets.Execution.Analysis.ExecutionTargetCompatibilityAnalyzer` | Reports CLR, reflection, schema binding, plugin, generated-row, and raw-expression requirements for target capability checks |
| `Musoq.Targets.Execution.TargetHostAbiInventoryBuilder` | Derives actual host ABI imports from runtime contracts for every target package; readiness blockers remain separate diagnostics |
| `Musoq.Targets.CSharpClr` | Owns generated C# lowering, Roslyn rendering context, finalization, inspection, and CLR activation |

`ToCSharpRewriteTreeVisitor` is legacy direct-AST code generation. Converter code generation is IR-only on this branch; do not add a fallback path to the legacy visitor.

### Traverse vs Inner Visitor Pattern

Most visitors come in pairs. The traversal visitor controls **when** and **in what order** child nodes are visited. The inner visitor performs the actual **transformation logic**.

```csharp
// Example: TransformTree.cs orchestration
var rewriter = new RewriteQueryVisitor();
var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, scopeWalker);
queryTree.Accept(rewriteTraverser);  // Traverse drives, Rewriter transforms
queryTree = rewriter.RootScript;     // Get transformed tree
```

### Other Visitors

| Visitor | Purpose |
|---------|---------|
| `DefensiveVisitorBase` | Base class with default implementations that throw on unhandled nodes |
| `RawTraverseVisitor` | Simple traversal without transformation |
| `IAwareExpressionVisitor` | Scope-aware visitor interface |
| `IScopeAwareExpressionVisitor` | Extended scope awareness |
| `BinaryOperatorTypeRules` | Type inference rules for binary operators |
| `MethodAccessType` | Method access classification |
| `VisitorOperationNames` | Named constants for visitor operations |

## IR Query Planner And Target Boundary

The active runtime path is under `IR/`. It separates query meaning from execution strategy before target packages render or finalize executable artifacts.

| Area | Purpose |
|------|---------|
| `IR/Expressions/` | Typed expression records (`ColumnRef`, `Literal`, `BinaryOp`, `MethodCall`, `AggregateRef`, `WindowFunctionRef`) plus printers and rewriters |
| `IR/Bindings/` | `OutputSchema`, `ColumnSchema`, `ProjectedField`, `AggregateBinding`, `OrderField`, and `WindowRegistration` |
| `IR/Logical/` | `LogicalPlanBuilder`, `LogicalPlanBuildTraverseVisitor`, `LogicalPlanPrinter`, and `LogicalNode` records for query semantics |
| `IR/Planning/` | `QueryPlanner`, plan properties, source-aware metadata/diagnostics, predicate placement diagnostics, and planner-owned physical strategy rules |
| `IR/Optimization/` | Named optimizer pass infrastructure and stage groups for logical, physical, and Execution IR boundaries |
| `IR/SourcePlanning/` | Predicate DTO conversion, comparison, and matching utilities shared by source planning |
| `IR/Physical/` | `PhysicalPlanBuilder`, `PhysicalPlanPrinter`, and `PhysicalNode` records with aggregate, join, window, set-operation, and materialization strategies |
| `IR/Execution/` | `PhysicalToExecutionPlanBuilder`, focused `Lowering/` coordinators, `Facts/` analysis facts, `ExecutionNode`, and `ExecutionExpression` records for explicit executable operations |
| `IR/Printing/` | Shared plan diagnostic printing helpers used by the logical, planning, and physical printers |
| `IR/CodeGeneration/` | Target-neutral final projection/sink planning metadata consumed by target renderers |

### Where New Code Belongs

| Change type | Put it here | Boundary rule |
|-------------|-------------|---------------|
| Planning or strategy metadata | `IR/Planning` and named optimizer passes in `IR/Optimization` | Emit typed artifacts such as `LogicalPlanningArtifacts`, `PhysicalPlanningArtifacts`, and `ExecutionPlanningArtifacts`; do not add behavior-consuming data to loose dictionaries or renderer/lowerer side channels. |
| Physical-to-Execution lowering | `IR/Execution/Lowering` plus `PhysicalToExecutionPlanBuilder` dispatch | Consume planner-owned execution artifacts; aggregate, join, CTE, and window logic should enter through coordinators. |
| Target-specific rendering | `Musoq.Targets.CSharpClr` or a future `Musoq.Targets.*` package | Render optimized Execution IR faithfully through target descriptors and target-specific render inputs; do not add renderer-specific strategy decisions to evaluator planning/lowering. |
| Parser AST traversal | `Musoq.Parser/Traversal` | Add shared traversal in `AstChildren`, `AstWalker`, or `AstRewriter`; keep `IExpressionVisitor` and `Node.Accept(...)` working. |
| Semantic binding | `Musoq.Evaluator.Visitors` semantic services | Keep `BuildMetadataAndInferTypesVisitor` as the facade; add source, method, result-shape, validation, and diagnostic behavior to focused services when practical. |

Key planner rules:
- New optimizer rules belong in `IR/Optimization` under the owning stage group. Builders create initial shapes, lowerers translate selected strategy, and target renderers emit faithfully; update [architecture.md](../../../.claude/rules/architecture.md) if a feature truly needs different ownership.
- Optimizer passes should consume and recompute analysis facts through `OptimizationContext.AnalysisFacts`; changed passes invalidate stale plan-derived facts unless they recompute them in the same pass.
- Scalar-reuse transformations share `ScalarEvaluationRegion`, `ScalarReuseCandidate`, `ScalarReuseCostModel`, and `ScalarReuseCollector`. Register every evaluation-changing pass in `OptimizerClassificationRegistry`; an unknown classification is an architecture failure, and profitability belongs to compile time rather than generated runtime branches.
- Stability-aware reuse may lower through `ExecutionLet`, typed carriers, generated row fields, or existing typed arrays, but never through a runtime cache dictionary or initialization sentinel. Preserve volatile/materialization, source/enumeration, conditional, aggregate/window, helper/recursive, and specialized-parallel boundaries.
- Every logical and physical plan node carries an `OutputSchema`; prefer it over inferred-column fallbacks when resolving produced query shape.
- Source-aware planning belongs in planner helpers such as `RequiredColumnUsagePlanner`, `SourcePredicatePlanner`, `SourceInteractionPlanner`, `SourcePlanningPlanner`, `SourceBoundaryPlanner`, and `PredicatePlacementPlanner`. Public source planning is stateless: accepted ordering/slicing must flow as immutable `SourceExecutionPlan` data through `SourceExecutionContext.Plan`.
- Keep source-planning ownership split: request construction in `IR/Planning/SourcePlanning`, predicate DTO conversion/comparison/matching in `IR/SourcePlanning`, physical residual lowering in `IR/Physical/SourcePlanning`, and evaluator/benchmark datasource mechanics in `Musoq.Tests.Common.SourcePlanning`. Reuse these helpers before adding a new rewriter, predicate evaluator, ordering comparer, or benchmark-only datasource implementation.
- Source boundary plans explain APPLY, interpretation, property-source, and access-method boundaries through `PlanningText`, including invocation shape, row behavior, result shape, and cacheability confidence; they are diagnostic metadata until a physical strategy or Execution IR metadata change explicitly consumes them.
- Predicate placement plans should distinguish source-local placement from pre-inner-join left/right, post-join, post-aggregate, and post-window stages without moving predicates unless a planner-owned physical strategy explicitly consumes that decision.
- Projection pruning belongs to required-column planning and `ProjectionPruningPhysicalPass`; do not move required-column mapping or projection pruning decisions into physical builders, execution lowerers, renderers, or generated helper code.
- Boundary row-shape and row-width pruning decisions belong to planning. Execution lowering may consume selected `RowWidthPruningPlan` metadata, but must not construct new boundary row-shape or row-width pruning plans.
- Parser-shape subquery and distinct rewrites must run through `PreLogicalNormalizer`. Do not call `SubqueryToCteRewriteVisitor`, `SubqueryToCteRewriteTraverseVisitor`, `DistinctToGroupByVisitor`, or `DistinctToGroupByTraverseVisitor` directly from other production stages.
- Aggregate binding pairs refresh/set methods with getter methods. Identifier normalization strips qualifiers and whitespace, so `Sum(r.Val)` and `Sum(Val)` can match.
- Runtime-v2 aggregate hot paths should use static typed aggregate kernels declared through `[AggregateFunction]`. A kernel owns a concrete `State` type, `Set(ref State, args...)`, `Get(in State)`, and optional `Merge(ref State, in State)`. The generated code should pass concrete aggregate arguments directly and store concrete query-specific state fields.
- Multi-argument aggregate kernels expose tuple-shaped metadata for planning, but C# target hot paths should still call `Set` with separate concrete arguments when the kernel declares them that way.
- Keep the deleted plugin `Group` model and legacy aggregate injection markers out of normal runtime-v2 aggregate lowering. Captured aggregate values must use typed captured-field Execution IR, not object-backed group value reads or writes.
- Aggregate group ownership is planned explicitly through `AggregateGroupPlan`: root, prefix, and leaf levels are generated only when required. Parent-depth aggregate ownership is computed once as `ownerPrefixLength = max(0, keyCount - parentDepth)`, and leaf groups keep typed owner references instead of target renderer code walking parent chains.
- C# aggregate rendering is split inside `Musoq.Targets.CSharpClr`: aggregate operations, query-specific group classes, and query-specific parallel single-key loops stay target-owned. Keep prefix-group logic in the plan/lowering metadata before adding renderer branches.
- Single-key parallel aggregation should stay generated for the concrete query shape. The generated local method performs direct key reads, direct group construction, direct accumulator updates, and direct merge calls; helper-driven generic aggregation is legacy cleanup territory unless a query cannot safely use the generated path.
- Physical aggregate strategy selection chooses aggregate-only, single-key, or value-tuple strategies; do not reintroduce object-backed hash aggregate keys. Physical join strategy selection chooses hash joins only when equality keys decompose cleanly.
- Window plans add explicit materialization before `PhysicalWindowNode` rendering.
- Physical planning chooses execution strategies; Execution IR carries executable decisions and runtime metadata such as materialization shape, capacity hints, context liveness, static metadata, typed keys, and precomputed lookup sets.
- Do not restore retired optimizer fallbacks in execution lowering or target rendering. Lowerers must not self-plan strategies, create replacement join strategies, bind reusable method targets, perform expression hoisting, disable planner-selected CTE sidecars, emit final CTE sidecar runtime nodes, or rely on renderer method-target synthesis.
- Helper extraction approval is generated-C# readability owned inside `Musoq.Targets.CSharpClr.Optimization.Codegen`. Target renderers may attach `CodegenHelperExtractionMetadata` candidate annotations, but `HelperExtractionReadabilityPass` is the only owner that approves existing helper candidates or extracts metadata-approved inline helper blocks.
- Generated C# is acceptance evidence, not the primary optimization surface. If a generated sample looks slow, first decide which `PhysicalNode` strategy or `ExecutionNode` metadata should prevent that shape.
- Target renderer decomposition is strict. Plain, grouped, and window query shapes are different; inspect logical plans, physical plans, and Execution IR before changing renderer code after a `NotSupportedException`. Generated C# rendering behavior belongs in `Musoq.Targets.CSharpClr`.
- Pure portable symbol records and `ExecutionPortableSymbolPortability` belong to `Musoq.Targets.Abstractions`; portability is classified as `Portable`, `HostImport`, or `ClrOnly`. Evaluator `IR/Execution/Portability` owns the CLR-backed `ExecutionPortableSymbolFactory` and explicit `ExecutionPortableSymbolCatalog` used during lowering and analysis. Classify `LibraryBase` and aggregate-attributed callables from their contracts rather than namespaces. Unknown CLR fallback must include a reason. `ExecutionTargetCapabilities` enforces allowed type/callable portability before backend rendering, while readiness evaluates broad requirement categories separately from type/callable symbol portability.
- Every type in the public `ExecutionPlan` contract graph uses `ExecutionTypeRef`, including expressions, variables, row shapes, source bindings, aggregate metadata, indexes, windows, and captured locals; public execution-plan properties and constructors must not expose `System.Type` or `Assembly`. Evaluator lowering and optimization may use the internal CLR sidecar while constructing plans; analysis should consume `PortableType`, and CSharp rendering must use its target-owned `RequireClrType` compatibility helper.
- Method calls, aggregate operations, and plugin windows use `ExecutionCallableRef`, never public `MethodInfo`. Portable callable signatures include classification, declaring/parameter/return symbols, generic arity, and invocation mode. Evaluator lowering/optimization may inspect `ClrMethod`; analysis consumes `PortableCallable`; CSharp rendering must use `RequireClrMethod`.
- `ExecutionLiteral` and `ExecutionConstantInSet` store immutable canonical `ExecutionConstantValue` instances, not arbitrary public `object` payloads. Preserve width/bit/word/code-unit/tick/offset/byte encodings; unsupported CLR values must remain explicit internal `ClrOnly` sidecars and readiness blockers.
- `ExecutionRawExpression` is retired. `ExecutionExpressionConverter.RegisteredExpressionTypes` must exhaustively cover concrete `IrExpression` types, and unregistered expressions must fail deterministically during lowering.
- Every concrete execution node and expression has one stable `ExecutionOperationId`; keep `ExecutionOperationCatalog` exhaustive and produce an `ExecutionTargetOperationReport` before rendering. A new operation is incomplete until target capabilities and rejection/conformance tests are updated.
- `ExecutionSemanticsContract.Version1` is the compatibility contract for current CSharpClr null, arithmetic, conversion, comparison, ordering, grouping, and exception behavior. Preserve operation-specific differences such as unchecked-width runtime integer arithmetic versus checked constant-fold diagnostics and checked aggregate overflow.
- Host ABI inventory belongs in `Musoq.Targets.Execution.Analysis` and package metadata, not evaluator runtime nodes. It contains actual typed `TargetHostAbiImportDetails` with derived immutable `Attributes` and a positive `ContractVersion`; readiness and `ClrOnly` blockers must not be represented as imports. Target-specific extension imports must use `TargetHostAbiImport.CreateCustom(...)`.
- Callable/plugin target coverage belongs in fake non-CLR package e2e tests through `CompileTargetPackageWithDiagnostics`. Keep callable compatibility requirements, plugin runtime-contract entries, typed `TargetPluginInvocationAbiDetails`, pre-render capability rejection, typed export entrypoints, optional inspection, and zero-host-import package paths covered.
- `TargetRenderResult` and finalization use structured `TargetDiagnostic` failures for expected capability/lowering limits. Keep programming-contract violations as exceptions and do not let a renderer bypass catalog validation.
- `Musoq.Targets.TestPortable` consumes Execution IR directly, lowers its supported subset to immutable `PortableSubsetProgram`, and executes with `PortableValue`. It must not access CLR sidecars or reference Converter, CSharpClr, Roslyn, reflection activation, or target analysis implementations, and it must not be production registered. This is an intentional breaking change to the public-looking Execution IR surface; there is no public target selector.
- `TargetContractVersions` versions Execution IR, host ABI, and target package format independently. Version changes require updated capability checks, manifests, and portable conformance coverage; the public artifact format remains version `2` until deliberately migrated.
- Lowerer decomposition is likewise strict. Aggregate, join, CTE, and window lowering behavior belongs in `IR/Execution/Lowering` coordinators when practical; set operations remain in their focused lowerer partials. Avoid rebuilding source, group, and finalization setup ad hoc in unrelated lowerer partials.
- Before touching IR code, read [architecture.md](../../../.claude/rules/architecture.md).

## Generated Code Samples & Runtime Optimization

Musoq compiles SQL queries into C# code at runtime. The generated-code sample corpus is tracked under `generated-code-samples/current/` and `generated-code-samples/profiled/` and is governed by catalog-driven tests in `Musoq.Evaluator.Tests`:

- `GeneratedCodeSamplesCatalog` is the source of truth for all generated sample names, SQL queries, categories, output formats, and schema-provider factories. `GeneratedCodeSamplesShapeTests.ExpectedSampleFileCount` records the current expected corpus size.
- `GeneratedCodeSamplesSnapshotTests` validates tracked sample files against deterministic catalog output; CI shape coverage also generates current catalog output in memory.
- `GeneratedInterpretationCodeDumpTests` keeps the binary/text interpretation samples (`Q16` and `Q17`) on the same parity path as the catalog.
- `GeneratedCodeSamplesShapeTests` checks fast structural budgets for generated-code patterns and guards that retired helper shapes such as `GetColumnValue`, `SmartForEach`, `ConvertTableToSource`, `TableRowSource`, discarded-context conversions, and inline `IN` array allocations stay absent unless a test explicitly documents a transitional allowance.
- `RuntimeV2MaintainabilityBudgetTests` enforces source-file maintainability budgets. Runtime-v2 Execution files should stay at or below 900 lines unless the test carries a concrete temporary justification, and `EvaluationHelper` must remain split into small domain partials rather than growing back into a catch-all helper.
- Generated query rows are self-contained `Row` subclasses emitted per query. Do not reintroduce a shared `GeneratedRow` base or eager `object[]` storage; `Values` should stay a lazy public-compatibility boundary.

### How to Validate Samples

```bash
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "GeneratedCodeSamplesSnapshotTests|GeneratedCodeSamplesShapeTests|GeneratedInterpretationCodeDumpTests" --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

The snapshot tests compile each catalog query through `InstanceCreator.CreateForAnalyze`, format the Roslyn syntax trees deterministically, and compare them with tracked sample files. `GeneratedCodeSamplesManifestTests` also hashes every normalized generated sample against a tracked manifest so CI catches generated-output drift independently from line-ending or namespace noise. Refresh utilities are intentionally marked `[Ignore]`; call them deliberately from a temporary local runner when a generated-code refresh is expected.

### Runtime Optimization Workflow

1. **Run the generated-sample tests** to see whether current generated code still matches the sample corpus.
2. **Refresh and read the local samples** to identify slow shapes such as redundant materialization, discarded contexts, string-keyed lookups, inline allocation, or reflection-like access.
3. **Map each slow shape to an owner**: logical planning for query meaning, physical planning for strategy, Execution IR for executable metadata, and target renderer only for faithful syntax emission.
4. **Consult the planner architecture reference** in [architecture.md](../../../.claude/rules/architecture.md) for Physical/Execution IR ownership boundaries, and the [Musoq.Benchmarks copilot-instructions.md](../Musoq.Benchmarks/copilot-instructions.md) for the benchmark-to-query-family mapping.
5. **Establish a benchmark baseline before code changes** for the affected query family.
6. **Implement changes** in `IR/Physical` or `IR/Execution`; touch `IR/CodeGeneration` only for target-neutral render metadata, and touch `Musoq.Targets.CSharpClr` only for generated C# syntax emission.
7. **Refresh tracked generated-code samples intentionally** and verify the generated code improved.
8. **Run the full test suite** to ensure correctness.
9. **Run the same benchmarks** and report before/after runtime and allocation deltas.

### Legacy Codegen Patterns That Should Stay Absent

- **`EvaluationHelper.ConvertTableToSource(...)`, `EvaluationHelper.SmartForEach(...)`, and `TableRowSource`** — deleted old-renderer table/resolver adapters. Runtime v2 should materialize typed rows or iterate `Table.Rows` through explicit Execution IR table-row shapes.
- **`score[@"alias.Column"]` and hot-loop `EvaluationHelper.GetColumnValue(...)`** — string-keyed resolver lookup. Keep dynamic/object access at explicit adapter boundaries, then operate on typed source or generated rows.
- **Repeated inline casts** (`((BasicEntity)row.Contexts[0]).Prop`) — multiple casts of the same entity per loop iteration. Runtime v2 should hoist repeated entity reads or render direct typed row access whenever shape metadata permits it.
- **`: GeneratedRow`, `GroupLayout`, `GroupSlot`, `GroupKey`, `new Group(...)`, `.GetValue<...>` aggregate reads, and `.SetValue(...)` aggregate writes** — retired generated-code shapes for normal runtime-v2 queries. Add or extend static typed aggregate kernel metadata instead of routing awkward aggregates through object-backed groups.
- **`EvaluationHelper.AggregateSingleKeyParallel(...)` and `AggregateSingleKeyParallel_...` generated names** — retired generic parallel aggregate helper paths. Parallel single-key aggregation should use generated query-specific loops named for the concrete query shape.

## Interpretation Schema Enforcement

The Evaluator enforces the generic-style interpretation function syntax (`Interpret<Schema>(data)`) and bans the old string-based syntax (`Interpret(data, 'SchemaName')`):

- **`BuildMetadataAndInferTypesVisitor`**: Detects old syntax early during semantic analysis and throws `InvalidOperationException` with a migration message
- **`RewriteQueryVisitor`**: Second enforcement point during query rewriting

## Key Classes

| Class | Purpose |
|-------|---------|
| `CompiledQuery` | Final executable query — call `Run()` to execute and get results |
| `SchemaRegistry` | Registers and resolves schema providers |
| `BuildItems` | Contains all compilation artifacts and metadata passed through the build chain |
| `LogicalPlanBuilder` | Lowers the normalized typed AST into logical query operators |
| `QueryPlanner` | Derives source properties, source-aware diagnostics, and query-level strategy decisions |
| `PhysicalPlanBuilder` | Builds physical nodes from planner decisions |
| `PhysicalToExecutionPlanBuilder` | Dispatches physical strategies into focused lowering coordinators and shared context |
| `Musoq.Targets.Execution.Analysis.ExecutionTargetCompatibilityAnalyzer` | Reports target requirements from optimized Execution IR |
| `Musoq.Targets.Execution.Analysis.TargetRuntimeContractBuilder` | Builds portable runtime service inventory for target packages |
| `Musoq.Targets.CSharpClr` | Owns generated C# rendering, Roslyn finalization, inspection, and CLR activation |
| `QueryAnalyzer` | Analyzes queries for metadata without full compilation |
| `RewriteQueryVisitor` | Main AST transformer — normalizes structure for code generation |
| `BuildMetadataAndInferTypesVisitor` | Semantic analysis facade — type inference, method resolution, symbol tables, and focused services |
| `Table` | Runtime table — stores rows with column definitions |
| `Row` | Runtime row — `Contexts[]` array holds entity objects per source |

## Dependencies

```
Musoq.Evaluator
├── Musoq.Parser    (AST, nodes, tokens, IExpressionVisitor)
├── Musoq.Schema    (ISchema, ISchemaProvider, data source abstractions)
└── Musoq.Plugins   (built-in SQL functions, LibraryBase)
    ↑ depended on by: Musoq.Converter
```

## Development Workflow

### Testing

```bash
# Run evaluator tests - largest suite
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"

# Run specific test by name
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "FullyQualifiedName~TestMethodName" --nologo --verbosity quiet --logger "console;verbosity=normal"

# Validate generated code samples
dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --filter "GeneratedCodeSamplesSnapshotTests|GeneratedCodeSamplesShapeTests|GeneratedInterpretationCodeDumpTests" --nologo --verbosity quiet --logger "console;verbosity=minimal"
```

### Common Modifications

**Adding a new visitor:**
1. Create the visitor class implementing `IExpressionVisitor` (or extending `DefensiveVisitorBase`)
2. Prefer `Musoq.Parser/Traversal` helpers for shared raw traversal, walking, or identity rewriting behavior
3. Create a corresponding traverse visitor if the visitor needs controlled legacy traversal
4. Put semantic source, method, result-shape, validation, or diagnostic behavior behind the focused semantic services when the visitor is `BuildMetadataAndInferTypesVisitor`
5. Register the visitor in `TransformTree.cs` at the appropriate phase
6. Add tests in `Musoq.Evaluator.Tests`

**Modifying execution strategy or generated code shape:**
1. Identify the relevant `LogicalNode`, `PhysicalNode`, and `ExecutionNode` path.
2. Inspect the logical plan, physical plan, and Execution IR before changing rendering code.
3. Put strategy choices in `IR/Physical` and executable metadata in `IR/Execution`.
4. Touch `IR/CodeGeneration` only when target-neutral final projection/sink render metadata changes; touch `Musoq.Targets.CSharpClr` when generated C# syntax changes.
5. Regenerate code samples intentionally through the ignored refresh utility in `GeneratedCodeSamplesSnapshotTests`.
6. Review the generated code diff.
7. Run the full test suite.
8. Run benchmarks if the change affects runtime performance.

**Adding a new SQL clause or feature:**
1. Add AST nodes in Musoq.Parser (if needed)
2. Update `BuildMetadataAndInferTypesVisitor` for semantic analysis
3. Update `RewriteQueryVisitor` for query normalization
4. Update `ExpressionConverter` and `LogicalPlanBuilder` so the feature enters Expression IR and logical planning
5. Update `QueryPlanner` or planner-owned strategy rules when the feature needs execution-strategy selection
6. Update `PhysicalToExecutionPlanBuilder`, `IR/Execution/Lowering` coordinators, and Execution IR records when execution needs new operations or metadata
7. Add or update target rendering in `Musoq.Targets.CSharpClr` only after the plan and Execution IR shape are explicit
8. Add tests covering the new feature, including focused IR tests when plan shape or rendering changes

### Impact of Changes

Evaluator changes can affect the entire query pipeline. After modifications:
- Always run evaluator tests: `dotnet test src/dotnet/Musoq.Evaluator.Tests --configuration Release --no-build --nologo --verbosity quiet --logger "console;verbosity=minimal"`
- For code generation changes, regenerate samples and review
- For planner or Execution IR changes, add or update tests under `src/dotnet/Musoq.Evaluator.Tests/IR`
- For performance-sensitive changes, run benchmarks
- Changes to visitors may affect query correctness across all query patterns
