using Musoq.Evaluator.TemporarySchemas;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.IR.Optimization;
using Musoq.Evaluator.Visitors;

namespace Musoq.Converter.Build;

public partial class TransformTree(BuildChain successor, ILoggerResolver loggerResolver) : BuildChain(successor)
{
    public override void Build(BuildItems items)
    {
        ArgumentNullException.ThrowIfNull(items);
        items.SchemaProvider = new TransitionSchemaProvider(items.SchemaProvider);

        ParseBuildArtifacts parseArtifacts = items.ParseArtifacts;
        var queryTree = parseArtifacts.RawQueryTree;

        var preLogicalNormalization = new PreLogicalNormalizer().Normalize(queryTree);
        queryTree = preLogicalNormalization.NormalizedRoot;
        items.OptimizerTraceText = OptimizationTraceTextPrinter.Print(preLogicalNormalization.Trace);

        var extractColumnsVisitor = new ExtractRawColumnsVisitor();
        var extractRawColumnsTraverseVisitor = new ExtractRawColumnsTraverseVisitor(extractColumnsVisitor);

        queryTree.Accept(extractRawColumnsTraverseVisitor);

        var metadataVisitor = CreateMetadataVisitor(items, extractColumnsVisitor.Columns);
        var metadataTraverserVisitor = new BuildMetadataAndInferTypesTraverseVisitor(metadataVisitor);

        try
        {
            queryTree.Accept(metadataTraverserVisitor);
            queryTree = metadataVisitor.Root;
        }
        catch (Exception ex)
        {
            if (!items.DiagnosticContext.HasErrors)
                items.DiagnosticContext.ReportException(ex);
        }

        if (items.DiagnosticContext.HasErrors)
            return;

        if (items.CompilationOptions.UseCteParallelization)
            items.CteExecutionPlan = ComputeCteExecutionPlan(queryTree);

        var rewriter = new RewriteQueryVisitor(items.CompilationOptions);
        var rewriteTraverser = new RewriteQueryTraverseVisitor(rewriter, new ScopeWalker(metadataTraverserVisitor.Scope));

        queryTree.Accept(rewriteTraverser);

        queryTree = rewriter.RootScript;

        var semanticArtifacts = BuildSemanticArtifacts(items, queryTree, metadataVisitor, metadataTraverserVisitor);
        items.SemanticArtifacts = semanticArtifacts;

        var planningArtifacts = BuildPlans(semanticArtifacts, metadataVisitor, items);
        if (planningArtifacts != null)
            items.PlanningArtifacts = planningArtifacts;

        if (items.DiagnosticContext is { HasErrors: true } || items.StopAfterPlanning)
            return;

        items.ExecutionArtifacts = BuildExecutionInspection(items);
        items.RenderingArtifacts = BuildWithIrRenderer(items, semanticArtifacts, metadataVisitor, metadataTraverserVisitor);

        Successor?.Build(items);
    }


}
