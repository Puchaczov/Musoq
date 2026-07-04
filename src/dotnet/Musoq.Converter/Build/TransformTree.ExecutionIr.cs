using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Musoq.Evaluator.IR.CodeGeneration;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Codegen;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.CodeGeneration;
using Musoq.Schema;
using PhysicalToExecutionPlanBuilder = Musoq.Evaluator.IR.Execution.PhysicalToExecutionPlanBuilder;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static RenderingStageBuildResult BuildWithIrRenderer(
        TransformPipelineContext context,
        SemanticBuildArtifacts semantic,
        PlanningBuildArtifacts planning,
        ExecutionBuildArtifacts execution,
        BuildMetadataAndInferTypesVisitor metadata,
        BuildMetadataAndInferTypesTraverseVisitor metadataTraverser)
    {
        var assemblyName = context.AssemblyName;
        var safeNamespaceName = SanitizeNameForNamespace(assemblyName);
        var generator = RoslynSharedFactory.Generator;

        var renderContext = new RenderContext(
            generator,
            new RenderContextOptions(
                Scope: metadataTraverser.Scope,
                AssemblyName: safeNamespaceName,
                ScriptParameterDefinitions: semantic.ScriptParameterDefinitions,
                ScriptVariableDefinitions: semantic.ScriptVariableDefinitions,
                InstrumentationMode: context.CompilationOptions.InstrumentationMode,
                ResultMode: context.QueryResultMode,
                FinalResultSinkKind: ResolveFinalResultSinkKind(context.QueryResultMode),
                OutputType: context.OutputType,
                ForceTableResultMaterialization: context.CompilationOptions.ForceTableResultMaterialization));

        var renderer = new CSharpRenderer(renderContext);
        var queryIdentifier = "compiled";
        if (planning.PhysicalPlan is null)
            throw new InvalidOperationException(
                "IR cutover requires a physical plan, but none was produced for the query.");

        var executionQueryResult = RenderExecutionQueryMethod(execution, renderer, queryIdentifier);
        renderContext.AddClassMember(executionQueryResult.MethodDeclaration);
        var compilationUnit = renderer.RenderCompilationUnit(
            queryIdentifier,
            ExecutionPlanInventory.CountTableSlots(execution.ExecutionPlan),
            ExecutionPlanInventory.CountCteIndexSlots(execution.ExecutionPlan));
        var readabilityResult = new CodegenReadabilityOptimizer().Optimize(compilationUnit);
        var updatedContext = context.AppendTrace(readabilityResult.Trace) with
        {
            QueryMethodRenderMetadata = executionQueryResult.Metadata
        };
        compilationUnit = readabilityResult.OptimizedCode;

        var compilationContext = new CompilationContextManager(
            RoslynSharedFactory.CreateCompilation(assemblyName));
        compilationContext.InitializeDefaults();
        foreach (var referenceType in updatedContext.AdditionalReferenceTypes)
        {
            if (!metadata.Assemblies.Contains(referenceType.Assembly))
                metadata.Assemblies.Add(referenceType.Assembly);
        }

        if (updatedContext.OutputType?.Assembly is { } outputAssembly && !metadata.Assemblies.Contains(outputAssembly))
            metadata.Assemblies.Add(outputAssembly);

        compilationContext.InitializeCoreReferences(metadata.Assemblies);
        compilationContext.AddSyntaxTree(ClassEmitter.CreateSyntaxTreeDirect(compilationUnit));
        if (!string.IsNullOrEmpty(updatedContext.InterpreterSourceCode))
        {
            compilationContext.TrackNamespace("Musoq.Generated.Interpreters");
            compilationContext.AddSyntaxTree(CSharpSyntaxTree.ParseText(
                updatedContext.InterpreterSourceCode,
                new CSharpParseOptions(LanguageVersion.CSharp11)));
        }

        var artifacts = new RenderingBuildArtifacts(
            compilationContext.GetCompilation(),
            $"{safeNamespaceName}.CompiledQuery",
            updatedContext.QueryMethodRenderMetadata);
        return new RenderingStageBuildResult(artifacts, updatedContext);
    }

    private static QueryMethodRenderResult RenderExecutionQueryMethod(
        ExecutionBuildArtifacts execution,
        CSharpRenderer renderer,
        string queryIdentifier)
    {
        if (execution.ExecutionPlanBuildResult is not { Supported: true, ExecutionPlan: { } executionPlan })
            throw CreateUnsupportedExecutionIrException(execution.ExecutionPlanBuildResult?.UnsupportedReason);

        var outcome = renderer.TryRenderExecutionQueryMethod(executionPlan, queryIdentifier);
        if (outcome.Method is { } renderedMethod)
            return renderedMethod;

        var reason = string.IsNullOrWhiteSpace(outcome.UnsupportedReason)
            ? "Execution IR C# backend did not produce a query method."
            : outcome.UnsupportedReason;

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

    private static ExecutionStageBuildResult BuildExecutionInspection(
        TransformPipelineContext context,
        SemanticBuildArtifacts semantic,
        PlanningBuildArtifacts planning)
    {
        if (planning.PhysicalPlan == null)
            return new ExecutionStageBuildResult(new ExecutionBuildArtifacts(), context);

        var shapeResolver = new ExecutionShapeResolver(
            semantic.PipelineScope,
            semantic.PipelineInferredColumns ?? new Dictionary<string, ISchemaColumn[]>(StringComparer.Ordinal),
            schemaRegistry: context.SchemaRegistry);
        var builder = new PhysicalToExecutionPlanBuilder(
            shapeResolver,
            context.SchemaRegistry,
            context.CompilationOptions,
            semantic.CteExecutionPlan,
            planning.PlanningResult?.ExecutionArtifacts ??
            throw new InvalidOperationException("Execution IR lowering requires planner-owned execution artifacts from QueryPlanner."));
        var result = builder.Build(planning.PhysicalPlan);

        if (result is { Supported: true, ExecutionPlan: not null })
        {
            var optimizationResult = new ExecutionIrOptimizer().Optimize(result.ExecutionPlan, context.CompilationOptions);
            var updatedContext = context.AppendTrace(optimizationResult.Trace);
            var optimizedPlan = optimizationResult.OptimizedPlan;

            var artifacts = new ExecutionBuildArtifacts
            {
                ExecutionPlanBuildResult = result with { ExecutionPlan = optimizedPlan },
                InitialExecutionPlan = optimizationResult.InitialPlan,
                OptimizedExecutionPlan = optimizedPlan,
                ExecutionPlan = optimizedPlan,
                ExecutionPlanText = context.EmitExecutionPlanText && optimizedPlan != null
                    ? ExecutionPlanPrinter.Print(optimizedPlan)
                    : null
            };

            return new ExecutionStageBuildResult(artifacts, updatedContext);
        }

        var unsupportedArtifacts = new ExecutionBuildArtifacts
        {
            ExecutionPlanBuildResult = result,
            InitialExecutionPlan = result.ExecutionPlan,
            OptimizedExecutionPlan = result.ExecutionPlan,
            ExecutionPlan = result.ExecutionPlan,
            ExecutionPlanText = context.EmitExecutionPlanText
                ? result.ExecutionPlan != null
                    ? ExecutionPlanPrinter.Print(result.ExecutionPlan)
                    : ExecutionPlanPrinter.PrintUnsupported(result.UnsupportedReason ?? "Execution IR lowering did not produce a plan.")
                : null
        };

        return new ExecutionStageBuildResult(unsupportedArtifacts, context);
    }

}
