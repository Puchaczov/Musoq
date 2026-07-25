# Runtime Testability Coverage Notes

Last updated: 2026-07-23

## Coverage Command

```powershell
dotnet test src\dotnet\Musoq.Evaluator.Tests\Musoq.Evaluator.Tests.csproj --no-restore --collect:"XPlat Code Coverage"
```

Latest recorded coverage run:

- Result: 7916 passed, 4 skipped.
- Cobertura artifact: `src/dotnet/Musoq.Evaluator.Tests/TestResults/e7f4f7cc-6729-4733-ae31-01c41c9ae519/coverage.cobertura.xml`
- Overall line-rate: `0.7956`
- Overall branch-rate: `0.7003`

Latest remediation gate:

- Command: `dotnet test src/dotnet/Musoq.sln --no-restore`
- Scope: full solution gate after each architecture wave.
- Latest result: 16710 passed, 4 skipped (Parser 1693, Schema 457, Evaluator 9424, Converter 856, Plugins 4161, CSV examples 43, Git examples 45, Benchmarks 31).

## Directly Covered Runtime Seams

- Profiling/API boundary: `CompiledQuery`, `QueryProfileTextPrinter`, and `ExplainAnalyzeTextPrinter` now have focused boundary tests.
- Roslyn runtime seams: metadata reference cache, runtime reference provider, and compilation factory defaults have direct tests around reuse, failure, and concurrency.
- Interpreter compilation: `InterpreterCompilationUnit` has isolated tests for diagnostics, successful compile, type lookup, and assembly-load failure.
- Lowering planners/registries: post-operation, CTE sidecar/runtime/fusion planners, lowering session state, projection/apply lowering models and services, outer-apply null substitution, aggregate lowering models, window lowering models, set-operation lowering models, execution plan inventory, and window registries have direct unit tests. The current CTE characterization covers sidecar storage decisions, runtime guard/keyset scheduling, missing-alias schedule rejection, source-backed fusion rejection, hash-payload pruning, and builder reentrancy across unrelated plans.
- CTE sidecar default posture: `CompilationOptions` now defaults query-compilation tests to full parallelization, hash/sort-merge joins, CSE, constant folding, primitive validation, CTE parallelization, and CTE sidecar indexes. Tests that need legacy or disabled behavior opt out explicitly.
- CTE sidecar global joins: sidecar-enabled regression families cover hash/keyset indexes, CTE-backed aggregate fusion, CROSS JOIN values, CROSS/OUTER APPLY including ordinality, ASOF inner/left with partitions, direction, tie-breaks and CTE-backed sources, plus FULL OUTER presence/residual semantics.
- Planning boundary: planning-owned shape contracts and expression eligibility rules are covered by architecture tests that forbid planning dependencies on execution IR.
- Physical planning baseline: characterization tests now pin required optimizer shape facts, staged `PlanProperties` components, physical/source rewrite fact application, expando join fallback, source-local sort/skip/take rewrites, accepted source-predicate filter removal, identity-keyed execution-strategy lookup scoping, and shared IR expression traversal facts; architecture guardrails keep physical planning orchestration under `IR/Planning/Physical`.
- Physical planning ratchets: final guardrails require optimizer shape facts, prevent physical optimization passes from consuming `PlanProperties` directly, keep execution strategies keyed by `PhysicalNodeId`, and keep parallel planning on shared `IrExpressionTraversal`/`IrExpressionFacts`.
- Renderer seams: renderer session reuse, explicit `ExecutionRenderSession` flow, mutable-field-free renderer facade behavior, node family dispatch, registry traversal metadata, and capability decisions have focused tests; generated renderer output has semantic compile tests for CTE indexes, hash/keyset paths, windows, final shape output, strict casts, set operations, and profiling scopes.
- Semantic analysis seams: `SemanticAnalysisState` owns semantic mutable bags behind the visitor facade. Source binding, column/property binding, method binding, result-shape binding, query validation, diagnostic reporting, expression-diagnostic facts, and set-operator fact services now have directly testable internal seams and architecture guardrails that keep them as top-level services instead of visitor-private artifacts.
- Named datasource arguments: `NamedArgumentAstTests` and `NamedDatasourceArgumentParserTests` cover aligned names/spans, clone/rewrite preservation, positional-prefix/named-suffix grammar, unsupported scalar/row-access/DESC FUNCTIONS contexts, comments, multiline calls, and parser diagnostics. `NamedDatasourceArgumentFoundationTests`, `NamedDatasourceArgumentEvaluatorTests`, and `NamedDatasourceArgumentBinderMatrixTests` cover reflected names/defaults, hidden metadata parameters, metadata-less rejection, coupled/DESC/DESC SETTINGS binding, exact/assignable/ambiguous overloads, duplicate/unknown/missing/incompatible diagnostics, canonical runtime vectors, optional-default DESC rendering, property-source re-resolution, and public `Musoq.Schema` compatibility. `QueryInspectionTests` and the named evaluator surface tests cover JOIN, correlated CROSS APPLY, DESC QUERY, logical/physical/execution plans, generated C# label erasure, runtime correlated rows, and positional APPLY overload compatibility. The final full-solution gate records 16710 passed and 4 skipped.
- Parser traversal seams: `ParserNodeTraversalRegistry` owns parser-node child descriptors, including special ordering for CTEs, set operators, queries, and source nodes. Guardrails require every concrete parser node to be registered or explicitly leaf/unsupported and keep `ParserNodeChildTraversal` as a registry-backed adapter.
- Evaluator spec-diff syntax coverage: core scalar and row-source syntax, subquery and join behavior, aggregate/window/reshape/set syntax, TABLE/COUPLE/DESC contracts, and binary/text interpretation profile syntax now have evaluator tests that assert result columns and exact rows. Aggregate `FILTER` coverage includes `Count()`, `Count(*)`, `Count(distinct ...)`, and window/composition regressions. Ordered row assertions are used only when the query contains an explicit final `ORDER BY`; otherwise unordered row assertions are used.
- Remaining architecture remediation baseline: ratchets now freeze current optimizer root pass inventory, direct `PlanProperties` construction pressure, legacy node-keyed execution-strategy construction, renderer mutable-session references, planning-internal `PlanProperties` parameter use, and `BuildItems` raw-dictionary compatibility behavior.
- Residual architecture baseline: guardrails now freeze builder-private lowering model declarations, projection/apply private models, whole-builder lowering coordinator construction and constructor dependencies, renderer session-access surface, semantic visitor stack mutation, parser traversal `Visit` inventories, registry-backed parser traversal, semantic service ownership, and production `BuildItems` method-parameter pressure.
- Residual Wave 1 baseline: guardrails now freeze current ambient `ExecutionCSharpRenderer` session-slot usage, codegen direct calls into execution typed-sink/query-context internals, static string-keyed runtime caches, regex construction without explicit timeout in pattern operators, and broad `NotSupportedException` unsupported-shape fallback catches. Pattern-operator cache characterization tests pin the current per-distinct-pattern static cache behavior before the bounded-cache waves.
- Residual architecture completion: final guardrails require explicit renderer context flow, registry-owned execution-lowering dispatch, extracted builder lowering models, semantic stack ownership through `SemanticAnalysisState`/`SemanticTraversalFrame`, execution render artifact APIs at the codegen boundary, bounded cache contracts for user-shaped runtime caches, and typed unsupported-shape fallback outside documented compatibility boundaries.
- Runtime cache and unsupported-shape seams: `BoundedRuntimeCache` has deterministic eviction/concurrency tests, LIKE/RLIKE pattern caches use bounded caches plus regex timeouts, dynamic/XML documentation caches use explicit runtime cache options, and expression conversion exposes `TryConvert` so direct-apply order handling no longer catches broad `NotSupportedException`.
- Build artifact boundary: `BuildArtifactStore` backs typed `BuildItems` access while public dictionary inheritance remains a documented legacy compatibility shell. Typed parse, semantic, planning, execution, rendering, and compilation stage artifact records now carry internal pipeline data, and guardrails forbid production code from treating `BuildItems` as an `IDictionary<string, object>` contract outside the compatibility implementation.
- Architecture guardrails: `RuntimeTestabilityGuardrailTests` pins the current `PhysicalToExecutionPlanBuilder*.cs` private planner/result record count at `7`, freezes the registry-backed renderer-dispatch inventory, verifies registry coverage for rewriter/printer inventories, forbids renderer dependencies on planning/lowering, enforces registry-only renderer node dispatch, and keeps renderer mutable state session-owned.
- Sidecar architecture guardrails: `CteSidecarGlobalSupportGuardrailTests` requires explicit sidecar stage kinds and delegation mappings for ASOF, FULL/STANDARD, APPLY, and CROSS families, ties those mappings to converter regression families, and prevents hard unsupported rejections from returning in the sidecar-chain entry points.

## Intentional Indirect-Only Areas

- `PhysicalToExecutionPlanBuilder` still has a broad adapter surface even though builder-private lowering model types are now forbidden. CTE/session, projection/apply, aggregate, window, source/join, table-completion, and set/pipeline ownership have extracted seams; future feature work should keep moving remaining adapter logic into focused lowering services when touching those paths.
- Renderer syntax helpers remain primarily covered through generated-code snapshots and semantic compilation. Dispatch, session state, and traversal metadata now have direct tests, but exhaustive direct tests for every syntax fragment would duplicate Roslyn implementation details and make refactoring harder.
- Static runtime facade classes remain as compatibility entry points. Their default implementations are directly tested through internal seams; some platform-dependent branch paths remain coverage-limited by assembly loading behavior.
- Full query execution paths remain integration-tested through the existing evaluator suite. The new semantic compile tests intentionally stop at C# compilation for renderer families where runtime data setup would obscure the renderer contract.
- AI `Infer()` evaluator coverage remains intentionally out of scope until executable AI runtime fixtures or changed executable AI syntax are available.
