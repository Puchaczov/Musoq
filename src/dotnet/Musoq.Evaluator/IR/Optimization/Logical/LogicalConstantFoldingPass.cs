using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Rewriting;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed partial class LogicalConstantFoldingPass : ILogicalNormalizationPass
{
    public string Name => "LogicalConstantFolding";

    public OptimizationResult<LogicalNode> Optimize(LogicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Options.ConstantFoldingEnabled)
        {
            return OptimizationResult<LogicalNode>.NoChange(
                plan,
                "Logical constant folding is disabled by compilation options.");
        }

        var diagnostics = context.State.DiagnosticContext;
        var folder = new LogicalConstantExpressionFolder(diagnostics);
        var optimized = Rewrite(plan, folder);
        if (!ReferenceEquals(optimized, plan))
        {
            return OptimizationResult<LogicalNode>.Changed(
                optimized,
                folder.FoldedExpressions == 1
                    ? "Folded 1 logical constant expression."
                    : $"Folded {folder.FoldedExpressions} logical constant expression(s).");
        }

        return OptimizationResult<LogicalNode>.NoChange(
            plan,
            "No logical constant expressions were safe to fold.");
    }

    private static LogicalNode Rewrite(LogicalNode node, LogicalConstantExpressionFolder folder)
    {
        var rewritten = LogicalPlanRewriter.RewriteChildren(node, child => Rewrite(child, folder));
        return LogicalPlanRewriter.RewriteLocalExpressions(rewritten, folder.Visit);
    }

}
