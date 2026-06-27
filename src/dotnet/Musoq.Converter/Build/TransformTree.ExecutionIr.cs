using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Schema;
using PhysicalToExecutionPlanBuilder = Musoq.Evaluator.IR.Execution.PhysicalToExecutionPlanBuilder;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static RenderingBuildArtifacts BuildWithIrRenderer(
        BuildItems items,
        SemanticBuildArtifacts semantic,
        BuildMetadataAndInferTypesVisitor metadata,
        BuildMetadataAndInferTypesTraverseVisitor metadataTraverser)
    {
        var assemblyName = items.AssemblyName;
        var safeNamespaceName = SanitizeNameForNamespace(assemblyName);
        var generator = RoslynSharedFactory.Generator;

        var context = new RenderContext(
            generator,
            new RenderContextOptions(
                Scope: metadataTraverser.Scope,
                AssemblyName: safeNamespaceName,
                ScriptParameterDefinitions: semantic.ScriptParameterDefinitions,
                ScriptVariableDefinitions: semantic.ScriptVariableDefinitions,
                InstrumentationMode: items.CompilationOptions.InstrumentationMode,
                ResultMode: items.QueryResultMode,
                FinalResultSinkKind: ResolveFinalResultSinkKind(items.QueryResultMode),
                OutputType: items.OutputType,
                ForceTableResultMaterialization: items.CompilationOptions.ForceTableResultMaterialization));

        var renderer = new CSharpRenderer(context);
        var queryIdentifier = "compiled";
        if (items.PhysicalPlan is null)
            throw new InvalidOperationException(
                "IR cutover requires a physical plan, but none was produced for the query.");

        var executionQueryResult = RenderExecutionQueryMethod(items, renderer, queryIdentifier);
        items.QueryMethodRenderMetadata = executionQueryResult.Metadata;
        context.AddClassMember(executionQueryResult.MethodDeclaration);
        var compilationUnit = renderer.RenderCompilationUnit(
            queryIdentifier,
            CountExecutionTableSlots(items.ExecutionPlan),
            CountExecutionCteIndexSlots(items.ExecutionPlan));
        var readabilityResult = new CodegenReadabilityOptimizer().Optimize(compilationUnit);
        items.OptimizerTraceText = OptimizationTraceTextPrinter.Append(items.OptimizerTraceText, readabilityResult.Trace);
        compilationUnit = readabilityResult.OptimizedCode;

        var compilationContext = new CompilationContextManager(
            RoslynSharedFactory.CreateCompilation(assemblyName));
        compilationContext.InitializeDefaults();
        foreach (var referenceType in items.AdditionalReferenceTypes)
        {
            if (!metadata.Assemblies.Contains(referenceType.Assembly))
                metadata.Assemblies.Add(referenceType.Assembly);
        }

        if (items.OutputType?.Assembly is { } outputAssembly && !metadata.Assemblies.Contains(outputAssembly))
            metadata.Assemblies.Add(outputAssembly);

        compilationContext.InitializeCoreReferences(metadata.Assemblies);
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(compilationUnit));
        if (!string.IsNullOrEmpty(items.InterpreterSourceCode))
        {
            compilationContext.TrackNamespace("Musoq.Generated.Interpreters");
            compilationContext.AddSyntaxTree(CSharpSyntaxTree.ParseText(
                items.InterpreterSourceCode,
                new CSharpParseOptions(LanguageVersion.CSharp11)));
        }

        return new RenderingBuildArtifacts(
            compilationContext.GetCompilation(),
            $"{safeNamespaceName}.CompiledQuery");
    }

    private static QueryMethodRenderResult RenderExecutionQueryMethod(
        BuildItems items,
        CSharpRenderer renderer,
        string queryIdentifier)
    {
        if (items.ExecutionPlanBuildResult is not { Supported: true, ExecutionPlan: { } executionPlan })
            throw CreateUnsupportedExecutionIrException(items.ExecutionPlanBuildResult?.UnsupportedReason);

        var outcome = renderer.TryRenderExecutionQueryMethod(executionPlan, queryIdentifier);
        if (outcome.Method is { } renderedMethod)
            return renderedMethod;

        var reason = string.IsNullOrWhiteSpace(outcome.UnsupportedReason)
            ? "Execution IR C# backend did not produce a query method."
            : outcome.UnsupportedReason;

        RecordExecutionRenderUnsupported(items, reason);
        throw CreateUnsupportedExecutionIrException(reason);
    }

    private static FinalResultSinkKind ResolveFinalResultSinkKind(QueryResultMode resultMode)
    {
        return resultMode switch
        {
            QueryResultMode.TypedEnumerable => FinalResultSinkKind.TypedSerialEnumerable,
            QueryResultMode.TableViaRows => FinalResultSinkKind.TableRowsMaterialized,
            _ => FinalResultSinkKind.TableDirect
        };
    }

    private static NotSupportedException CreateUnsupportedExecutionIrException(string? unsupportedReason)
    {
        var reason = string.IsNullOrWhiteSpace(unsupportedReason)
            ? "Execution IR lowering did not produce a plan."
            : unsupportedReason;

        return new NotSupportedException(
            $"Execution IR does not support this query shape and old physical rendering is disabled: {reason}");
    }

    private static void RecordExecutionRenderUnsupported(BuildItems items, string unsupportedReason)
    {
        items.ExecutionPlanBuildResult = ExecutionPlanBuildResult.CreateUnsupported(unsupportedReason);
        items.ExecutionPlan = null;
        if (items.EmitExecutionPlanText)
            items.ExecutionPlanText = ExecutionPlanPrinter.PrintUnsupported(unsupportedReason);
    }

    private static ExecutionBuildArtifacts BuildExecutionInspection(BuildItems items)
    {
        if (items.PhysicalPlan == null)
            return new ExecutionBuildArtifacts();

        var shapeResolver = new ExecutionShapeResolver(
            items.PipelineScope,
            items.PipelineInferredColumns ?? new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal),
            schemaRegistry: items.SchemaRegistry);
        var builder = new PhysicalToExecutionPlanBuilder(
            shapeResolver,
            items.SchemaRegistry,
            items.CompilationOptions,
            items.CteExecutionPlan,
            items.PlanningResult?.ExecutionArtifacts ??
            throw new InvalidOperationException("Execution IR lowering requires planner-owned execution artifacts from QueryPlanner."));
        var result = builder.Build(items.PhysicalPlan);

        if (result is { Supported: true, ExecutionPlan: not null })
        {
            var optimizationResult = new ExecutionIrOptimizer().Optimize(result.ExecutionPlan, items.CompilationOptions);
            items.OptimizerTraceText = OptimizationTraceTextPrinter.Append(items.OptimizerTraceText, optimizationResult.Trace);
            var optimizedPlan = optimizationResult.OptimizedPlan;

            return new ExecutionBuildArtifacts
            {
                ExecutionPlanBuildResult = result with { ExecutionPlan = optimizedPlan },
                InitialExecutionPlan = optimizationResult.InitialPlan,
                OptimizedExecutionPlan = optimizedPlan,
                ExecutionPlan = optimizedPlan,
                ExecutionPlanText = items.EmitExecutionPlanText && optimizedPlan != null
                    ? ExecutionPlanPrinter.Print(optimizedPlan)
                    : null
            };
        }

        return new ExecutionBuildArtifacts
        {
            ExecutionPlanBuildResult = result,
            InitialExecutionPlan = result.ExecutionPlan,
            OptimizedExecutionPlan = result.ExecutionPlan,
            ExecutionPlan = result.ExecutionPlan,
            ExecutionPlanText = items.EmitExecutionPlanText
                ? result.ExecutionPlan != null
                    ? ExecutionPlanPrinter.Print(result.ExecutionPlan)
                    : ExecutionPlanPrinter.PrintUnsupported(result.UnsupportedReason ?? "Execution IR lowering did not produce a plan.")
                : null
        };
    }

    private static int CountExecutionTableSlots(ExecutionPlan? executionPlan)
    {
        if (executionPlan == null)
            return 0;

        return FindMaxExecutionTableIndex(executionPlan.Body) + 1;
    }

    private static int CountExecutionCteIndexSlots(ExecutionPlan? executionPlan)
    {
        if (executionPlan == null)
            return 0;

        return FindMaxExecutionCteIndexSlot(executionPlan.Body) + 1;
    }

    private static int FindMaxExecutionTableIndex(ExecutionBlock block)
    {
        var maxIndex = -1;

        foreach (var node in block.Nodes)
            maxIndex = Math.Max(maxIndex, FindMaxExecutionTableIndex(node));

        return maxIndex;
    }

    private static int FindMaxExecutionTableIndex(ExecutionNode node)
    {
        return node switch
        {
            ExecutionStoreTable store => store.TableIndex,
            ExecutionForEach forEach => FindMaxExecutionTableIndex(forEach.Body),
            ExecutionForEachWithOrdinality forEach => FindMaxExecutionTableIndex(forEach.Body),
            ExecutionIf branch => FindMaxExecutionTableIndex(branch.Body),
            ExecutionHashProbe probe => FindMaxExecutionTableIndex(probe.Body),
            ExecutionKeySetProbe probe => Math.Max(
                FindMaxExecutionTableIndex(probe.Body),
                probe.NoMatchBody == null ? -1 : FindMaxExecutionTableIndex(probe.NoMatchBody)),
            ExecutionParallelBlock parallel => Math.Max(
                FindMaxExecutionTableIndex(parallel.Merge.Body),
                parallel.Tasks.Select(task => FindMaxExecutionTableIndex(task.Body)).DefaultIfEmpty(-1).Max()),
            _ => -1
        };
    }

    private static int FindMaxExecutionCteIndexSlot(ExecutionBlock block)
    {
        var maxIndex = -1;

        foreach (var node in block.Nodes)
            maxIndex = Math.Max(maxIndex, FindMaxExecutionCteIndexSlot(node));

        return maxIndex;
    }

    private static int FindMaxExecutionCteIndexSlot(ExecutionNode node)
    {
        return node switch
        {
            ExecutionStoreCteIndex store => store.IndexSlot,
            ExecutionLoadCteIndex load => load.IndexSlot,
            ExecutionForEach forEach => FindMaxExecutionCteIndexSlot(forEach.Body),
            ExecutionForEachWithOrdinality forEach => FindMaxExecutionCteIndexSlot(forEach.Body),
            ExecutionIf branch => FindMaxExecutionCteIndexSlot(branch.Body),
            ExecutionHashProbe probe => Math.Max(
                FindMaxExecutionCteIndexSlot(probe.Body),
                probe.NoMatchBody == null ? -1 : FindMaxExecutionCteIndexSlot(probe.NoMatchBody)),
            ExecutionKeySetProbe probe => Math.Max(
                FindMaxExecutionCteIndexSlot(probe.Body),
                probe.NoMatchBody == null ? -1 : FindMaxExecutionCteIndexSlot(probe.NoMatchBody)),
            ExecutionParallelBlock parallel => Math.Max(
                FindMaxExecutionCteIndexSlot(parallel.Merge.Body),
                parallel.Tasks.Select(task => FindMaxExecutionCteIndexSlot(task.Body)).DefaultIfEmpty(-1).Max()),
            _ => -1
        };
    }
}
