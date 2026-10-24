using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical.Rewriting;

internal static partial class LogicalPlanRewriter
{
    public static LogicalNode RewriteLocalExpressions(LogicalNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        return node switch
        {
            AccessMethodSourceNode accessMethod => RewriteAccessMethod(accessMethod, rewriteExpression),
            AggregateNode aggregate => RewriteAggregate(aggregate, rewriteExpression),
            DescNode desc => RewriteDesc(desc, rewriteExpression),
            FilterNode filter => RewriteFilter(filter, rewriteExpression),
            HavingFilterNode having => RewriteHaving(having, rewriteExpression),
            InterpretSourceNode interpret => RewriteInterpret(interpret, rewriteExpression),
            JoinNode join => RewriteJoin(join, rewriteExpression),
            ProjectNode project => RewriteProject(project, rewriteExpression),
            QualifyFilterNode qualify => RewriteQualify(qualify, rewriteExpression),
            SchemaScanNode scan => RewriteSchemaScan(scan, rewriteExpression),
            SortNode sort => RewriteSort(sort, rewriteExpression),
            UnpivotNode unpivot => RewriteUnpivot(unpivot, rewriteExpression),
            ValuesScanNode values => RewriteValues(values, rewriteExpression),
            WindowNode window => RewriteWindow(window, rewriteExpression),
            _ => node
        };
    }

    private static LogicalNode RewriteAccessMethod(AccessMethodSourceNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var expression = rewriteExpression(node.MethodCallExpression);
        return ReferenceEquals(expression, node.MethodCallExpression)
            ? node
            : node with { MethodCallExpression = expression };
    }

    private static LogicalNode RewriteAggregate(AggregateNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var groupKeys = LogicalPlanRewriter.RewriteExpressions(node.GroupKeys, rewriteExpression, out var groupKeysChanged);
        var bindings = LogicalPlanRewriter.RewriteAggregateBindings(node.Bindings, rewriteExpression, out var bindingsChanged);

        return !groupKeysChanged && !bindingsChanged
            ? node
            : new AggregateNode(groupKeys, node.GroupKeyNames, node.GroupKeyTypes, bindings, node.Input);
    }

    private static LogicalNode RewriteDesc(DescNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, rewriteExpression, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteFilter(FilterNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var predicate = rewriteExpression(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new FilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteHaving(HavingFilterNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var predicate = rewriteExpression(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new HavingFilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteInterpret(InterpretSourceNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, rewriteExpression, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteJoin(JoinNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var predicate = rewriteExpression(node.OnPredicate);
        var tieBreak = node.TieBreak;
        if (tieBreak != null)
        {
            var tieBreakExpression = rewriteExpression(tieBreak.Expression);
            if (!ReferenceEquals(tieBreakExpression, tieBreak.Expression))
                tieBreak = tieBreak with { Expression = tieBreakExpression };
        }

        return ReferenceEquals(predicate, node.OnPredicate) &&
               ReferenceEquals(tieBreak, node.TieBreak)
            ? node
            : new JoinNode(node.Kind, predicate, node.Left, node.Right, tieBreak, node.WithOrdinality);
    }

    private static LogicalNode RewriteProject(ProjectNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var fields = LogicalPlanRewriter.RewriteProjectedFields(node.Fields, rewriteExpression, out var fieldsChanged);
        return !fieldsChanged
            ? node
            : new ProjectNode(fields, node.Input) { IsDistinct = node.IsDistinct };
    }

    private static LogicalNode RewriteQualify(QualifyFilterNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var predicate = rewriteExpression(node.Predicate);
        return ReferenceEquals(predicate, node.Predicate)
            ? node
            : new QualifyFilterNode(predicate, node.Input);
    }

    private static LogicalNode RewriteSchemaScan(SchemaScanNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var arguments = LogicalPlanRewriter.RewriteExpressions(node.Arguments, rewriteExpression, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    private static LogicalNode RewriteSort(SortNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var keys = LogicalPlanRewriter.RewriteOrderFields(node.Keys, rewriteExpression, out var keysChanged);
        return !keysChanged
            ? node
            : new SortNode(keys, node.Input);
    }

    private static LogicalNode RewriteValues(ValuesScanNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var rows = LogicalPlanRewriter.RewriteValuesRows(node.Rows, rewriteExpression, out var changed);
        return changed ? node with { Rows = rows } : node;
    }

    private static LogicalNode RewriteWindow(WindowNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var registrations = LogicalPlanRewriter.RewriteWindowRegistrations(
            node.Registrations,
            rewriteExpression,
            out var registrationsChanged);
        return !registrationsChanged
            ? node
            : new WindowNode(registrations, node.Input);
    }
}
