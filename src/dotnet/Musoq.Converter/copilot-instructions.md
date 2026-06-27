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
│   ├── TransformTree.cs               # Step 3: AST transforms, staged optimizers, Execution IR lowering, readability, IR rendering
│   ├── TerminalBuildChain.cs          # Inspection endpoint; stops after IR rendering
│   └── TurnQueryIntoRunnableCode.cs   # Step 4: Roslyn compilation -> DLL bytes
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
| 3 | `TransformTree` | Executes the AST visitor pipeline, builds staged logical/physical/Execution IR artifacts, runs optimizer/readability groups, renders via the C# backend, and stores the `CSharpCompilation` |
| 4 | `TurnQueryIntoRunnableCode` | Emits DLL + PDB bytes from the `CSharpCompilation` in memory |

Inspection builds use the same first three steps but end at `TerminalBuildChain`, so they produce logical plan text, physical plan text, and generated C# source without emitting DLL/PDB bytes.

### TransformTree IR Pipeline

`TransformTree` is the bridge between AST normalization and executable code:

1. Runs pre-plan AST visitors: `DistinctToGroupBy`, `SubqueryToCte`, `ExtractRawColumns`, `BuildMetadataAndInferTypes`, and `RewriteQueryVisitor`. Dead CTE elimination and constant folding now belong to the logical optimizer.
2. Calls `BuildPlans()` to create initial and optimized logical/physical plans, run `QueryPlanner`, and store `BuildItems.PlanningResult`, `BuildItems.PlanningText`, optimizer trace text, and `BuildItems.PhysicalPlan`.
3. Calls `BuildExecutionInspection()` to lower the physical plan into initial Execution IR, run `ExecutionIrOptimizer`, and store optimized Execution IR snapshots.
4. Calls `BuildWithIrRenderer()` to render it with `CSharpRenderer`, `ExecutionCSharpRenderer`, and `RenderContext`, then run `CodegenReadabilityOptimizer` before Roslyn compilation.
5. Fails fast if a physical plan is missing; do not add or restore a fallback to `ToCSharpRewriteTreeVisitor`.

Optimization responsibility is layered: `BuildPlans()` selects logical meaning and delegates query-level strategy/property decisions to `QueryPlanner`, `BuildExecutionInspection()` makes executable operations and runtime metadata explicit, and `BuildWithIrRenderer()` faithfully emits that Execution IR before readability-only codegen passes. Do not introduce renderer-only performance strategy decisions in converter orchestration, and do not restore lowerer self-planning or legacy codegen fallback paths.

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
└── Musoq.Evaluator  (visitors, code generation, compilation, runtime)
    ├── Musoq.Parser  (lexer, parser, AST nodes)
    ├── Musoq.Schema  (ISchema, ISchemaProvider, data source abstractions)
    └── Musoq.Plugins (built-in SQL functions)
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
