## Architecture Understanding

### Query Processing Pipeline
1. **Parse**: SQL text -> `RootNode` parser AST
2. **Normalize and type**: AST visitors rewrite syntax, infer schema metadata, bind methods, and produce a normalized typed AST
3. **Build logical plan**: `LogicalPlanBuilder` + `LogicalPlanBuildTraverseVisitor` lower the typed AST into a `LogicalNode` tree that describes what the query means
4. **Plan query**: `QueryPlanner` derives properties, records planning diagnostics, and invokes physical plan construction
5. **Build physical plan**: `PhysicalPlanBuilder.Lower()` maps logical operators to `PhysicalNode` execution strategies selected by planner-owned rules
6. **Build Execution IR**: `PhysicalToExecutionPlanBuilder` lowers physical strategies into explicit executable operations and metadata
7. **Render C#**: `Musoq.Targets.CSharpClr` owns `CSharpRenderer` and `ExecutionCSharpRenderer`, which emit Roslyn syntax from Execution IR
8. **Compile**: `TurnQueryIntoRunnableCode` compiles the generated C# into an in-memory assembly
9. **Execute**: `CompiledQuery.Run()` executes against data sources from schema providers and returns table results

### Current Pipeline

```text
SQL text
  -> Musoq.Parser lexer/parser
  -> RootNode AST
  -> TransformTree pipeline
       PreLogicalNormalizer
       semantic metadata and scope analysis
       RewriteQueryVisitor
  -> LogicalPlanBuilder
  -> LogicalNode tree with OutputSchema
  -> LogicalOptimizer
  -> QueryPlanner with PlanProperties and PlanningDecision diagnostics
  -> PhysicalPlanBuilder
  -> PhysicalNode tree with execution strategies
  -> PhysicalToExecutionPlanBuilder
  -> ExecutionNode tree with executable operations and metadata
  -> ExecutionIrOptimizer
  -> Musoq.Targets.CSharpClr: CSharpRenderer + ExecutionCSharpRenderer + RenderContext
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
- **Optimization ownership**: `QueryPlanner` chooses safe query-level strategy/property decisions; physical planning builds nodes from those decisions; Execution IR carries executable decisions and runtime metadata such as materialization shape, capacity hints, context liveness, static metadata, typed keys, and precomputed lookup sets. Generated C# is evidence of these decisions, not the primary optimization surface.
- **Stability-aware scalar movement**: `ExpressionStabilityAnalyzer` is the shared authority for transformations that can change scalar evaluation count, timing, or order. It consumes `ColumnStability` propagated through schema, logical, and Execution IR metadata; stable fields and unmarked callables are eligible, while volatile fields, `[NonDeterministic]` members, injected context, and unknown dynamic access taint an expression. CSE, predicate/APPLY movement, CTE fusion, source transfer, join/parallel strategy selection, and loop-invariant code motion must consult the same facts.
- **Scalar-reuse model and inventory**: `ScalarReuseCollector`, `ScalarEvaluationRegion`, and `ScalarReuseCostModel` are compile-time planning facts shared by LICM, CSE, field hoisting, windows, aggregates, joins, row-shape planning, and source pushdown. Every evaluation-changing optimizer pass must be present in `OptimizerClassificationRegistry` and declare evaluation-preserving, stability-checked, region-checked, or not-applicable behavior. An unclassified pass is an architecture error; profitability must never become a runtime branch.
- **Source scalar/replayability contract**: `Musoq.Schema.Optimization` owns the portable source scalar subset, accepted/residual computed-projection partition, and `RowStreamReplayability`. Stable columns and replayable streams are independent facts; unknown replayability blocks reuse across enumeration boundaries. Provider acceptance is validated against the exact `QueryRowShape` and missing accepted fields are deterministic contract failures, never silent residual recomputation.
- **Execution LICM ownership**: `LoopInvariantCodeMotionPass` runs in the Execution IR pipeline after method-target reuse and before field/expression CSE. It inserts ordinary `ExecutionLet` nodes for maximal stable scalar candidates in serial `ExecutionForEach`, ordinality, and indexed loop scopes. It never moves source enumeration, streams, materialization, aggregate/window state, helper/recursive boundaries, conditional-only arms, or specialized parallel loops; renderers only render the resulting locals.
- **Behavior-consuming planner facts**: diagnostics may change behavior only after the planner promotes them into explicit internal strategy records such as `PredicateMovementPlan`, `RowWidthPruningPlan`, set-operation and CTE execution strategies, or source-boundary strategy guards. Execution builders may lower and defensively validate those records; renderers must only emit the resulting Execution IR. `BoundaryRowShapePlan` remains diagnostic row-shape metadata; selected sort/top/top-offset opportunities become behavior-consuming only through `RowWidthPruningPlan`. Aggregate, window, set operation, hash-join build, and CTE materialization pruning remain diagnostic-only in v1.
- **No-caching source boundary rule**: `SourceBoundaryStrategyPlan` may classify APPLY, interpretation, property, and access-method boundaries as per-row required, candidate-not-applied, or unknown, but it must not cache plugin/source calls without a separate capability design.
- **RenderContext**: centralized code-generation state for entity metadata, row classes, CTE table indexes, aggregate/window bindings, inferred columns, scope, and current row identifiers.
- **Renderer decomposition shapes**: plain, grouped, and window queries have strict physical and Execution IR shapes. If a renderer rejects a shape, inspect the logical plan, physical plan, and Execution IR before changing rendering code.
- **Debugging helpers**: use `IrExpressionPrinter`, `LogicalPlanPrinter`, `PlanningTextPrinter`, and `PhysicalPlanPrinter` to inspect intermediate representations.


This file is the current architecture reference for planner, Execution IR, and renderer ownership. For detailed architecture of each covered module, see the per-project `copilot-instructions.md` files listed from the root instructions.
