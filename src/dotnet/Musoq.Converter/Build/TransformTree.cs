using System.Runtime.ExceptionServices;
using Musoq.Evaluator;
using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.Visitors;
using System.Linq;

namespace Musoq.Converter.Build;

public partial class TransformTree(BuildChain successor, ILoggerResolver loggerResolver) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        var context = TransformPipelineContext.From(items) with
        {
            SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider)
        };
        items.SchemaProvider = context.SchemaProvider;

        ParseBuildArtifacts parseArtifacts = items.ParseArtifacts;
        var parsedQueryTree = parseArtifacts.RawQueryTree;
        var queryTree = parsedQueryTree;
        if (!RecursiveCtePrevalidation.TryValidate(queryTree, context.DiagnosticContext)) return;

        var preLogicalNormalization = new PreLogicalNormalizer().Normalize(queryTree);
        var normalizedQueryTree = preLogicalNormalization.NormalizedRoot;
        queryTree = normalizedQueryTree;
        context = context with
        {
            OptimizerTraceText = OptimizationTraceTextPrinter.Print(preLogicalNormalization.Trace)
        };
        items.OptimizerTraceText = context.OptimizerTraceText;

        var extractColumnsVisitor = new ExtractRawColumnsVisitor();
        queryTree.Accept(new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor));

        var metadataVisitor = CreateMetadataVisitor(context, extractColumnsVisitor.Columns);
        SemanticMetadataPhaseResult? metadataPhase = null;
        try
        {
            metadataPhase = new SemanticMetadataPhaseCoordinator().Analyze(queryTree, metadataVisitor);
            queryTree = metadataPhase.Query;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
                throw;

            if (ex is SchemaProviderFailureException providerFailure)
            {
                ExceptionDispatchInfo.Capture(providerFailure.InnerException ?? providerFailure).Throw();
                throw new InvalidOperationException("Schema provider failure rethrow did not propagate.");
            }

            if (EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex) &&
                !context.DiagnosticContext.HasErrors)
                context.DiagnosticContext.ReportException(ex);
            else if (!EvaluatorExceptionTaxonomy.IsExpectedQueryFailure(ex))
                throw;
        }

        if (context.DiagnosticContext.HasErrors || metadataPhase is null)
            return;

        var metadataQueryTree = queryTree;
        var semanticMetadata = metadataPhase.Metadata;

        var cteExecutionPlan = context.CompilationOptions.UseCteParallelization
            ? ComputeCteExecutionPlan(queryTree)
            : null;
        items.CteExecutionPlan = cteExecutionPlan;

        var scopeArtifact = metadataPhase.Scope;
        var rewrittenQueryTree = new SemanticRewritePhaseCoordinator().Rewrite(
            queryTree,
            scopeArtifact,
            context.CompilationOptions);
        queryTree = rewrittenQueryTree;

        var semanticArtifacts = BuildSemanticArtifacts(
            parsedQueryTree,
            normalizedQueryTree,
            metadataQueryTree,
            rewrittenQueryTree,
            semanticMetadata,
            scopeArtifact,
            cteExecutionPlan,
            context.DiagnosticContext.Diagnostics.ToArray());
        items.SemanticArtifacts = semanticArtifacts;

        var planningStage = BuildPlans(semanticArtifacts, context);
        PlanningBuildArtifacts? planningArtifacts = null;
        if (planningStage != null)
        {
            context = planningStage.Context;
            semanticArtifacts = planningStage.SemanticArtifacts;
            planningArtifacts = planningStage.Artifacts;
            items.OptimizerTraceText = context.OptimizerTraceText;
            items.SemanticArtifacts = semanticArtifacts;
            items.PlanningArtifacts = planningArtifacts;
        }

        if (context.DiagnosticContext is { HasErrors: true } || context.StopAfterPlanning)
            return;

        if (planningArtifacts == null)
            return;

        var executionStage = BuildExecutionInspection(context, semanticArtifacts, planningArtifacts);
        context = executionStage.Context;
        items.OptimizerTraceText = context.OptimizerTraceText;
        items.ExecutionArtifacts = executionStage.Artifacts;

        var renderingStage = BuildWithIrRenderer(
            context,
            semanticArtifacts,
            planningArtifacts,
            executionStage.Artifacts,
            scopeArtifact);
        if (renderingStage == null)
            return;

        context = renderingStage.Context;
        items.OptimizerTraceText = context.OptimizerTraceText;
        items.RenderingArtifacts = renderingStage.Artifacts;

        Successor?.Build(items);
    }


}
