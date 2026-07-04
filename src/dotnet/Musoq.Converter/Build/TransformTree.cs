using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.IR.Optimization.Logical;
using Musoq.Evaluator.Visitors;

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
        var queryTree = parseArtifacts.RawQueryTree;

        var preLogicalNormalization = new PreLogicalNormalizer().Normalize(queryTree);
        queryTree = preLogicalNormalization.NormalizedRoot;
        context = context with
        {
            OptimizerTraceText = OptimizationTraceTextPrinter.Print(preLogicalNormalization.Trace)
        };
        items.OptimizerTraceText = context.OptimizerTraceText;

        var extractColumnsVisitor = new ExtractRawColumnsVisitor();
        var extractRawColumnsTraverseVisitor = new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor);

        queryTree.Accept(extractRawColumnsTraverseVisitor);

        var metadataVisitor = CreateMetadataVisitor(context, extractColumnsVisitor.Columns);
        var metadataTraverserVisitor = new BuildMetadataAndInferTypesTraverseVisitor(metadataVisitor);

        try
        {
            queryTree.Accept(metadataTraverserVisitor);
            queryTree = metadataVisitor.Root;
        }
        catch (Exception ex)
        {
            if (!context.DiagnosticContext.HasErrors)
                context.DiagnosticContext.ReportException(ex);
        }

        if (context.DiagnosticContext.HasErrors)
            return;

        var cteExecutionPlan = context.CompilationOptions.UseCteParallelization
            ? ComputeCteExecutionPlan(queryTree)
            : null;
        items.CteExecutionPlan = cteExecutionPlan;

        var rewriter = new RewriteQueryVisitor(context.CompilationOptions);
        var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataTraverserVisitor.Scope));

        queryTree.Accept(rewriteTraverser);

        queryTree = rewriter.RootScript;

        var semanticArtifacts = BuildSemanticArtifacts(queryTree, metadataVisitor, metadataTraverserVisitor, cteExecutionPlan);
        items.SemanticArtifacts = semanticArtifacts;

        var planningStage = BuildPlans(semanticArtifacts, metadataVisitor, context);
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
            metadataVisitor,
            metadataTraverserVisitor);
        context = renderingStage.Context;
        items.OptimizerTraceText = context.OptimizerTraceText;
        items.RenderingArtifacts = renderingStage.Artifacts;

        Successor?.Build(items);
    }


}
