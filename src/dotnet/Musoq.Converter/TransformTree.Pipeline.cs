using System.Runtime.ExceptionServices;
using Musoq.Evaluator;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.Visitors;
using Musoq.Evaluator.Visitors.Helpers.CteDependencyGraph;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Converter.Build;

public partial class TransformTree(BuildChain successor, ILoggerResolver loggerResolver) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        items.CancellationToken.ThrowIfCancellationRequested();
        var telemetry = EvaluatorPerformanceTelemetry.BeginPhase("semantic-pipeline");
        IDisposable? semanticCacheFlight = null;
        try
        {
        var semanticCacheKey = SemanticTemplateCache.CreateKey(new SemanticTemplateCacheInput(
            items.RawQuery,
            items.SchemaProvider,
            items.CompilationOptions,
            items.CompilationPurpose,
            items.ExecutionTarget,
            items.QueryResultMode,
            items.OutputType,
            items.EmitPdb,
            items.InterpreterSourceCode,
            items.HasDeclaredSourceRuntimeSettings,
            items.HasSourceRuntimeSettingValues,
            items.CreateBuildMetadataAndInferTypesVisitor is not null,
            items.AdditionalReferenceTypes,
            items.SchemaRegistry?.GetType().AssemblyQualifiedName ?? string.Empty),
            items.CancellationToken);
        semanticCacheFlight = semanticCacheKey is { } key
            ? SemanticTemplateCache.Acquire(key, items.CancellationToken)
            : null;
        if (items.RetainSemanticCacheState)
            items.SemanticCacheFlight = semanticCacheFlight;
        var context = TransformPipelineContext.From(items) with
        {
            SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider)
        };
        context.CancellationToken.ThrowIfCancellationRequested();
        items.SchemaProvider = context.SchemaProvider;

        SemanticBuildArtifacts? semanticArtifacts = null;
        if (semanticCacheKey is { } cachedKey &&
            SemanticTemplateCache.TryGet(cachedKey, items.CancellationToken, out var cachedArtifacts))
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            DiagnosticReplay.AddMissing(context.DiagnosticContext, cachedArtifacts.Phase.Diagnostics);
            semanticArtifacts = cachedArtifacts with
            {
                CteExecutionPlan = context.CompilationOptions.UseCteParallelization
                    ? ComputeCteExecutionPlan(
                        cachedArtifacts.TransformedQueryTree,
                        context.CancellationToken)
                    : null
            };
            items.CteExecutionPlan = semanticArtifacts.CteExecutionPlan;
            items.SemanticArtifacts = semanticArtifacts;
        }
        else
        {
            ParseBuildArtifacts parseArtifacts = items.ParseArtifacts;
            var parsedQueryTree = parseArtifacts.RawQueryTree;
            var queryTree = parsedQueryTree;
            PreLogicalNormalizationResult? normalization;
            context.CancellationToken.ThrowIfCancellationRequested();
            using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.normalization"))
                normalization = NormalizeQuery(queryTree, context.DiagnosticContext);
            if (normalization == null)
                return;
            var normalizedQueryTree = normalization.NormalizedRoot;
            context.CancellationToken.ThrowIfCancellationRequested();
            queryTree = normalizedQueryTree;
            context = context.AppendTrace(normalization.Trace);
            ExtractRawColumnsVisitor extractColumnsVisitor;
            using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.raw-columns"))
            {
                extractColumnsVisitor = new ExtractRawColumnsVisitor();
                queryTree.Accept(new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor));
            }
            context.CancellationToken.ThrowIfCancellationRequested();

            var metadataVisitor = CreateMetadataVisitor(context, extractColumnsVisitor.Columns);
            SemanticMetadataPhaseResult? metadataPhase = null;
            try
            {
                using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.metadata-binding"))
                    metadataPhase = new SemanticMetadataPhaseCoordinator().Analyze(queryTree, metadataVisitor, parsedQueryTree);
                queryTree = metadataPhase.Query;
                context.CancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                    throw;

                context.CancellationToken.ThrowIfCancellationRequested();

                if (EvaluatorExceptionTaxonomy.FindSchemaProviderFailure(ex) is { } providerFailure)
                {
                    ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
                    throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
                }

                if (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex) &&
                    !context.DiagnosticContext.HasErrors)
                    context.DiagnosticContext.ReportException(ex);
                else if (!context.DiagnosticContext.HasErrors &&
                         !EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
                    context.DiagnosticContext.ReportException(
                        InternalDiagnosticException.ForCompiler(ex));
            }

            if (context.DiagnosticContext.HasErrors || metadataPhase is null)
                return;

            context.CancellationToken.ThrowIfCancellationRequested();
            new SemanticAdvisoryPhaseCoordinator().Analyze(
                metadataPhase.Query,
                metadataPhase.Metadata,
                context.DiagnosticContext,
                normalizedQueryTree,
                parsedQueryTree,
                context.CancellationToken);

            if (context.DiagnosticContext.HasErrors)
                return;

            context.CancellationToken.ThrowIfCancellationRequested();

            var metadataQueryTree = queryTree;
            var semanticMetadata = metadataPhase.Metadata;

            CteExecutionPlan? cteExecutionPlan;
            context.CancellationToken.ThrowIfCancellationRequested();
            using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.cte-facts"))
                cteExecutionPlan = context.CompilationOptions.UseCteParallelization
                    ? ComputeCteExecutionPlan(queryTree, context.CancellationToken)
                    : null;
            context.CancellationToken.ThrowIfCancellationRequested();
            items.CteExecutionPlan = cteExecutionPlan;

            var metadataScopeArtifact = metadataPhase.Scope;
            RootNode rewrittenQueryTree;
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.rewrite"))
                    rewrittenQueryTree = new SemanticRewritePhaseCoordinator().Rewrite(
                        queryTree,
                        metadataScopeArtifact,
                        context.CompilationOptions);
            }
            catch (Exception ex) when (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                if (!context.DiagnosticContext.HasErrors)
                    context.DiagnosticContext.ReportException(ex);
                return;
            }
            queryTree = rewrittenQueryTree;
            context.CancellationToken.ThrowIfCancellationRequested();

            using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.artifact-freeze"))
                semanticArtifacts = BuildSemanticArtifacts(
                    parsedQueryTree,
                    normalizedQueryTree,
                    metadataQueryTree,
                    rewrittenQueryTree,
                    semanticMetadata,
                    metadataScopeArtifact,
                    cteExecutionPlan,
                    context.DiagnosticContext.Diagnostics,
                    context.CancellationToken);
            items.SemanticArtifacts = semanticArtifacts;
            context.CancellationToken.ThrowIfCancellationRequested();
            bool sourceContractsValid;
            using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.source-contracts"))
                sourceContractsValid = ValidateGeneratedExecutionSourceContracts(
                    context,
                    queryTree,
                    metadataScopeArtifact,
                    semanticMetadata,
                    semanticArtifacts);
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!sourceContractsValid)
                return;

            if (semanticCacheKey is { } publishKey &&
                !semanticArtifacts.HasDeclaredSourceRuntimeSettings &&
                !semanticArtifacts.HasSourceRuntimeSettingValues)
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                items.SemanticCachePublication = SemanticTemplateCache.PreparePublication(
                    publishKey,
                    semanticArtifacts with { CteExecutionPlan = null },
                    context.CancellationToken);
            }
        }

        var scopeArtifact = semanticArtifacts.ScopeArtifact;

        PlanningStageBuildResult? planningStage;
        context.CancellationToken.ThrowIfCancellationRequested();
        using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.planning"))
            planningStage = BuildPlans(semanticArtifacts, context);
        context.CancellationToken.ThrowIfCancellationRequested();
        PlanningBuildArtifacts? planningArtifacts = null;
        if (planningStage != null)
        {
            context = planningStage.Context;
            semanticArtifacts = planningStage.SemanticArtifacts;
            planningArtifacts = planningStage.Artifacts;
            items.SemanticArtifacts = semanticArtifacts;
            items.PlanningArtifacts = planningArtifacts;
        }

        if (context.DiagnosticContext is { HasErrors: true } || context.StopAfterPlanning)
            return;

        if (planningArtifacts == null)
            return;

        ExecutionStageBuildResult executionStage;
        context.CancellationToken.ThrowIfCancellationRequested();
        using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.execution-ir"))
            executionStage = BuildExecutionInspection(context, semanticArtifacts, planningArtifacts);
        context.CancellationToken.ThrowIfCancellationRequested();
        context = executionStage.Context;
        items.ExecutionArtifacts = executionStage.Artifacts;

        RenderingStageBuildResult? renderingStage;
        context.CancellationToken.ThrowIfCancellationRequested();
        using (EvaluatorPerformanceTelemetry.BeginPhase("semantic.rendering"))
            renderingStage = BuildWithIrRenderer(
                context,
                semanticArtifacts,
                planningArtifacts,
                executionStage.Artifacts,
                scopeArtifact);
        context.CancellationToken.ThrowIfCancellationRequested();
        if (renderingStage == null)
            return;

        context = renderingStage.Context;
        items.OptimizerTraceText = context.OptimizerTraceText;
        items.RenderingArtifacts = renderingStage.Artifacts;
        }
        finally
        {
            if (!items.RetainSemanticCacheState)
            {
                SemanticTemplateCache.Discard(items.SemanticCachePublication);
                semanticCacheFlight?.Dispose();
            }

            telemetry.Dispose();
        }

        items.CancellationToken.ThrowIfCancellationRequested();
        Successor?.Build(items);
    }
}
