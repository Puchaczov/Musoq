using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
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
        return RewriteLocalExpressions(rewritten, folder);
    }

    private static LogicalNode RewriteLocalExpressions(LogicalNode node, LogicalConstantExpressionFolder folder)
    {
        return node switch
        {
            AccessMethodSourceNode accessMethod => RewriteAccessMethod(accessMethod, folder),
            AggregateNode aggregate => RewriteAggregate(aggregate, folder),
            DescNode desc => RewriteDesc(desc, folder),
            FilterNode filter => RewriteFilter(filter, folder),
            HavingFilterNode having => RewriteHaving(having, folder),
            InterpretSourceNode interpret => RewriteInterpret(interpret, folder),
            JoinNode join => RewriteJoin(join, folder),
            ProjectNode project => RewriteProject(project, folder),
            QualifyFilterNode qualify => RewriteQualify(qualify, folder),
            SchemaScanNode scan => RewriteSchemaScan(scan, folder),
            SortNode sort => RewriteSort(sort, folder),
            UnpivotNode unpivot => RewriteUnpivot(unpivot, folder),
            ValuesScanNode values => RewriteValues(values, folder),
            WindowNode window => RewriteWindow(window, folder),
            _ => node
        };
    }

    private static LogicalNode RewriteAccessMethod(AccessMethodSourceNode node, LogicalConstantExpressionFolder folder)
    {
        var expression = folder.Visit(node.MethodCallExpression);
        return ReferenceEquals(expression, node.MethodCallExpression)
            ? node
            : node with { MethodCallExpression = expression };
    }

    private static LogicalNode RewriteAggregate(AggregateNode node, LogicalConstantExpressionFolder folder)
    {
        var groupKeys = LogicalPlanRewriter.RewriteExpressions(node.GroupKeys, folder.Visit, out var groupKeysChanged);
        var bindings = LogicalPlanRewriter.RewriteAggregateBindings(node.Bindings, folder.Visit, out var bindingsChanged);

        return !groupKeysChanged && !bindingsChanged
            ? node
            : new AggregateNode(groupKeys, node.GroupKeyNames, node.GroupKeyTypes, bindings, node.Input);
    }

    private static LogicalNode RewriteDesc(DescNode node, LogicalConstantExpressionFolder folder)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, folder.Visit, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteFilter(FilterNode node, LogicalConstantExpressionFolder folder)
    {
        var predicate = folder.Visit(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new FilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteHaving(HavingFilterNode node, LogicalConstantExpressionFolder folder)
    {
        var predicate = folder.Visit(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new HavingFilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteInterpret(InterpretSourceNode node, LogicalConstantExpressionFolder folder)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, folder.Visit, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteJoin(JoinNode node, LogicalConstantExpressionFolder folder)
    {
        var predicate = folder.Visit(node.OnPredicate);
        var tieBreak = node.TieBreak;
        if (tieBreak != null)
        {
            var tieBreakExpression = folder.Visit(tieBreak.Expression);
            if (!ReferenceEquals(tieBreakExpression, tieBreak.Expression))
                tieBreak = tieBreak with { Expression = tieBreakExpression };
        }

        return ReferenceEquals(predicate, node.OnPredicate) &&
               ReferenceEquals(tieBreak, node.TieBreak)
            ? node
            : new JoinNode(node.Kind, predicate, node.Left, node.Right, tieBreak, node.WithOrdinality);
    }

    private static LogicalNode RewriteProject(ProjectNode node, LogicalConstantExpressionFolder folder)
    {
        var fields = LogicalPlanRewriter.RewriteProjectedFields(node.Fields, folder.Visit, out var fieldsChanged);
        return !fieldsChanged
            ? node
            : new ProjectNode(fields, node.Input) { IsDistinct = node.IsDistinct };
    }

    private static LogicalNode RewriteQualify(QualifyFilterNode node, LogicalConstantExpressionFolder folder)
    {
        var predicate = folder.Visit(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new QualifyFilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteSchemaScan(SchemaScanNode node, LogicalConstantExpressionFolder folder)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, folder.Visit, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteSort(SortNode node, LogicalConstantExpressionFolder folder)
    {
        var keys = LogicalPlanRewriter.RewriteOrderFields(node.Keys, folder.Visit, out var keysChanged);
        return !keysChanged
            ? node
            : new SortNode(keys, node.Input);
    }

    private static LogicalNode RewriteValues(ValuesScanNode node, LogicalConstantExpressionFolder folder)
    {
        var rows = LogicalPlanRewriter.RewriteValuesRows(node.Rows, folder.Visit, out var changed);
        return changed ? node with { Rows = rows } : node;
    }

    private static LogicalNode RewriteWindow(WindowNode node, LogicalConstantExpressionFolder folder)
    {
        var registrations = LogicalPlanRewriter.RewriteWindowRegistrations(
            node.Registrations,
            folder.Visit,
            out var registrationsChanged);
        return !registrationsChanged
            ? node
            : new WindowNode(registrations, node.Input);
    }
}
