using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Rewriting;
using Musoq.Evaluator.IR.Optimization.Logical;

namespace Musoq.Evaluator.IR.Planning.Subqueries;

internal sealed class SubqueryFallbackPlanner(PlanningContext context) : IrExpressionRewriter
{
    internal List<PlanningDecision> Decisions { get; } = [];

    internal LogicalNode Rewrite(LogicalNode node) => LogicalPlanRewriter.RewriteLocalExpressions(
        LogicalPlanRewriter.RewriteChildren(node, Rewrite), Visit);

    protected override IrExpression VisitSubquery(SubqueryExpression node)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        var query = node.Kind == SubqueryResultKind.Exists ? ExistsProjection.Prepare(node.Query) : node.Query;
        var optimized = new LogicalOptimizer(context.CompilationOptions.UseConstantFolding).Optimize(query);
        var nestedContext = context with
        {
            LogicalArtifacts = new LogicalPlanningArtifacts(optimized.InitialPlan, optimized.OptimizedPlan, optimized.Trace)
        };
        var result = new QueryPlanner().Plan(nestedContext);
        Decisions.Add(new PlanningDecision(PlanningDecisionCategory.SubqueryStrategy, "CorrelatedSubqueryFallback",
            node.Identifier, "Apply", PlanningConfidence.High,
            "The typed inner plan executes for the current outer binding only when its expression is demanded."));
        Decisions.AddRange(result.Decisions);
        var rewritten = (SubqueryExpression)base.VisitSubquery(node);
        return rewritten with
        {
            Planning = new SubqueryPlanningArtifacts(result,
                context.ShapeResolver,
                context.CompilationOptions, context.SchemaRegistry, context.CteExecutionPlan)
        };
    }
}
