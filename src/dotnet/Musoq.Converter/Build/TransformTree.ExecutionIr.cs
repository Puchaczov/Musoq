using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Execution;
using Musoq.Evaluator.IR.Optimization.Execution;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Diagnostics;
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
        context.CancellationToken.ThrowIfCancellationRequested();
        var renderRequest = CreateTargetRenderRequest(
            context,
            semantic,
            planning,
            execution,
            scopeArtifact);
        context.CancellationToken.ThrowIfCancellationRequested();
        Func<string, IDisposable>? beginTargetPhase = EvaluatorPerformanceTelemetry.IsEnabled
            ? static name => EvaluatorPerformanceTelemetry.BeginPhase(name)
            : null;
        using var targetTelemetry = TargetRenderTelemetry.Push(beginTargetPhase);
        var result = ExecutionTargetCatalog.Render(renderRequest);
        context.CancellationToken.ThrowIfCancellationRequested();
        if (!result.Success)
        {
            TargetDiagnosticReporter.Report(result.Diagnostics, context.DiagnosticContext);
            return null;
        }

        context.CancellationToken.ThrowIfCancellationRequested();
        var renderedArtifact = result.Artifact ??
                               throw new InvalidOperationException("Successful target rendering did not produce an artifact.");
        context.CancellationToken.ThrowIfCancellationRequested();
        var contribution = ExecutionTargetCatalog.CreateRenderBuildContribution(
            renderedArtifact,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        var readinessReport = CreateReadinessReport(context, renderRequest);
        context.CancellationToken.ThrowIfCancellationRequested();
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
        context.CancellationToken.ThrowIfCancellationRequested();
        var operationReport = ExecutionTargetOperationAnalyzer.Analyze(executionPlan);
        context.CancellationToken.ThrowIfCancellationRequested();
        var compatibilityReport = ExecutionTargetCompatibilityAnalyzer.Analyze(executionPlan);
        context.CancellationToken.ThrowIfCancellationRequested();
        var scriptBinding = CreateScriptBinding(semantic, context.CancellationToken);
        var references = CreateReferenceInventory(
            semantic.Phase.Metadata.Assemblies,
            context.CancellationToken);
        context.CancellationToken.ThrowIfCancellationRequested();
        var runtimeContract = TargetRuntimeContractBuilder.Build(
            executionPlan,
            compatibilityReport,
            TargetSourceRuntimeMetadataFactory.Create(semantic, planning));
        context.CancellationToken.ThrowIfCancellationRequested();
        var renderPurpose = TargetRenderPurposeFactory.CreatePurpose(context.CompilationPurpose);
        var renderProfile = TargetRenderPurposeFactory.CreateProfile(context.CompilationPurpose, context.EmitPdb);
        context.CancellationToken.ThrowIfCancellationRequested();
        return new TargetRenderRequest
        {
            TargetId = context.ExecutionTarget,
            Purpose = renderPurpose,
            Profile = renderProfile,
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
            CancellationToken = context.CancellationToken,
            BackendInputs = ExecutionTargetCatalog.CreateRenderInputs(
                context.ExecutionTarget,
                new TargetRenderInputBuildContext(
                    context.CancellationToken,
                    context.CompilationOptions,
                    context.QueryResultMode,
                    scriptBinding,
                    references,
                    TargetRenderOptionsFactory.Create(context.EnableContextualExecution),
                    renderPurpose,
                    renderProfile,
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

    private static TargetScriptBindingContract CreateScriptBinding(
        SemanticBuildArtifacts semantic,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parameters = new List<string>(semantic.ScriptParameterDefinitions.Count);
        foreach (var definition in semantic.ScriptParameterDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            parameters.Add(definition.Name);
        }

        var variables = new List<string>(semantic.ScriptVariableDefinitions.Count);
        foreach (var definition in semantic.ScriptVariableDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            variables.Add(definition.Name);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new TargetScriptBindingContract(
            parameters,
            variables);
    }

    private static TargetReferenceInventory CreateReferenceInventory(
        IReadOnlyList<System.Reflection.Assembly> assemblies,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var references = new List<string>(assemblies.Count);
        foreach (var assembly in assemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            references.Add(assembly.FullName ?? assembly.GetName().Name ?? assembly.ToString());
        }

        cancellationToken.ThrowIfCancellationRequested();
        references.Sort(StringComparer.Ordinal);
        cancellationToken.ThrowIfCancellationRequested();
        return new TargetReferenceInventory(
            references);
    }

    private static ExecutionPlan ResolveSupportedExecutionPlan(ExecutionPlanBuildResult? executionPlanBuildResult)
    {
        if (executionPlanBuildResult is { Supported: true, ExecutionPlan: { } executionPlan })
            return executionPlan;

        throw CreateUnsupportedExecutionIrException(executionPlanBuildResult?.UnsupportedReason);
    }

    private static InternalDiagnosticException CreateUnsupportedExecutionIrException(string? unsupportedReason)
    {
        var reason = string.IsNullOrWhiteSpace(unsupportedReason)
            ? "Execution IR lowering did not produce a plan."
            : unsupportedReason;
        return InternalDiagnosticException.ForCompiler(
            new NotSupportedException(
                $"Execution IR does not support this query shape and old physical rendering is disabled: {reason}"));
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
        context.CancellationToken.ThrowIfCancellationRequested();
        var result = builder.Build(planning.PhysicalPlan);
        context.CancellationToken.ThrowIfCancellationRequested();

        if (result is { Supported: true, ExecutionPlan: not null })
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var optimizationResult = new ExecutionIrOptimizer().Optimize(result.ExecutionPlan, context.CompilationOptions);
            context.CancellationToken.ThrowIfCancellationRequested();
            var updatedContext = context.AppendTrace(optimizationResult.Trace);
            context.CancellationToken.ThrowIfCancellationRequested();
            var optimizedPlan = ExecutionPhaseBoundaryPlanner.RepositionRootBoundaries(
                planning.PhysicalPlan,
                optimizationResult.OptimizedPlan);
            context.CancellationToken.ThrowIfCancellationRequested();

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

            context.CancellationToken.ThrowIfCancellationRequested();

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

        context.CancellationToken.ThrowIfCancellationRequested();

        return new ExecutionStageBuildResult(unsupportedArtifacts, context);
    }

}
