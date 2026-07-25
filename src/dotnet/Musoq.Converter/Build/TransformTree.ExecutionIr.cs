using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.Visitors;
using Musoq.Schema;
using Musoq.Targets.Execution.Analysis;
using PhysicalToExecutionPlanBuilder = Musoq.Evaluator.IR.Execution.PhysicalToExecutionPlanBuilder;

namespace Musoq.Converter.Build;

public partial class TransformTree
{
    private static RenderingStageBuildResult? BuildWithIrRenderer(
        TransformPipelineContext context,
        SemanticBuildArtifacts semantic,
        PlanningBuildArtifacts planning,
        ExecutionBuildArtifacts execution,
        SemanticScopeArtifact scopeArtifact)
    {
        var renderRequest = CreateTargetRenderRequest(
            context,
            semantic,
            planning,
            execution,
            scopeArtifact);
        var result = ExecutionTargetCatalog.Render(renderRequest);
        if (!result.Success)
        {
            TargetDiagnosticReporter.Report(result.Diagnostics, context.DiagnosticContext);
            return null;
        }

        var renderedArtifact = result.Artifact ??
                               throw new InvalidOperationException("Successful target rendering did not produce an artifact.");
        var contribution = ExecutionTargetCatalog.CreateRenderBuildContribution(renderedArtifact);
        var readinessReport = ExecutionTargetReadinessAnalyzer.AnalyzeFutureTargets(
            renderRequest.CompatibilityReport,
            renderRequest.RuntimeContract,
            renderRequest.SemanticsContract);
        var updatedContext = contribution.OptimizationTrace is null
            ? context
            : context.AppendTrace(contribution.OptimizationTrace);
        updatedContext = updatedContext with
        {
            QueryMethodRenderMetadata = contribution.QueryMethodRenderMetadata
        };

        var artifacts = new RenderingBuildArtifacts(renderedArtifact)
        {
            QueryMethodRenderMetadata = contribution.QueryMethodRenderMetadata,
            CompatibilityReport = renderRequest.CompatibilityReport,
            RuntimeContract = renderRequest.RuntimeContract,
            ReadinessReport = readinessReport,
            SemanticsContract = renderRequest.SemanticsContract
        };

        return new RenderingStageBuildResult(artifacts, updatedContext);
    }

    private static TargetRenderRequest CreateTargetRenderRequest(
        TransformPipelineContext context,
        SemanticBuildArtifacts semantic,
        PlanningBuildArtifacts planning,
        ExecutionBuildArtifacts execution,
        SemanticScopeArtifact scopeArtifact)
    {
        var executionPlan = ResolveSupportedExecutionPlan(execution.ExecutionPlanBuildResult);
        var operationReport = ExecutionTargetOperationAnalyzer.Analyze(executionPlan);
        var compatibilityReport = ExecutionTargetCompatibilityAnalyzer.Analyze(executionPlan);
        var scriptBinding = CreateScriptBinding(semantic);
        var references = CreateReferenceInventory(semantic.Phase.Metadata.Assemblies);
        var runtimeContract = TargetRuntimeContractBuilder.Build(
            executionPlan,
            compatibilityReport,
            TargetSourceRuntimeMetadataFactory.Create(semantic, planning));

        return new TargetRenderRequest
        {
            TargetId = context.ExecutionTarget,
            Identity = new TargetRenderIdentity(context.AssemblyName),
            Options = TargetRenderOptionsFactory.Create(context.EnableContextualExecution),
            ScriptBinding = scriptBinding,
            References = references,
            ExecutionPlan = executionPlan,
            ExecutionIrVersion = executionPlan.ExecutionIrVersion,
            SemanticsContract = executionPlan.SemanticsContract,
            OperationReport = operationReport,
            FeatureReport = ExecutionTargetFeatureAnalyzer.Analyze(executionPlan),
            CompatibilityReport = compatibilityReport,
            RuntimeContract = runtimeContract,
            HostAbiVersion = TargetContractVersions.HostAbi,
            BackendInputs = ExecutionTargetCatalog.CreateRenderInputs(
                context.ExecutionTarget,
                new TargetRenderInputBuildContext(
                    context.CompilationOptions,
                    context.QueryResultMode,
                    scriptBinding,
                    references,
                    TargetRenderOptionsFactory.Create(context.EnableContextualExecution),
                    new TargetRenderInputCompilerState(
                        context.AssemblyName,
                        context.OutputType,
                        context.AdditionalReferenceTypes,
                        context.InterpreterSourceCode,
                        scopeArtifact.CreateScope(),
                        semantic.ScriptParameterDefinitions,
                        semantic.ScriptVariableDefinitions,
                        semantic.Phase.Metadata.Assemblies)))
        };
    }

    private static TargetScriptBindingContract CreateScriptBinding(SemanticBuildArtifacts semantic)
    {
        return new TargetScriptBindingContract(
            semantic.ScriptParameterDefinitions
                .Select(static definition => definition.Name)
                .ToArray(),
            semantic.ScriptVariableDefinitions
                .Select(static definition => definition.Name)
                .ToArray());
    }

    private static TargetReferenceInventory CreateReferenceInventory(
        IReadOnlyList<System.Reflection.Assembly> assemblies)
    {
        return new TargetReferenceInventory(
            assemblies
                .Select(static assembly => assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString())
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray());
    }

    private static ExecutionPlan ResolveSupportedExecutionPlan(ExecutionPlanBuildResult? executionPlanBuildResult)
    {
        if (executionPlanBuildResult is { Supported: true, ExecutionPlan: { } executionPlan })
            return executionPlan;

        throw CreateUnsupportedExecutionIrException(executionPlanBuildResult?.UnsupportedReason);
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

        var executionScope = semantic.ScopeArtifact.CreateScope();
        var shapeResolver = new ExecutionShapeResolver(
            executionScope,
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
