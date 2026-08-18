using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

internal static class LogicalExpressionTraversal
{
    public static IEnumerable<IrExpression> SelfAndDescendantExpressions(LogicalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        foreach (var expression in NodeExpressions(node))
            yield return expression;

        foreach (var child in node.Children)
        foreach (var expression in SelfAndDescendantExpressions(child))
            yield return expression;
    }

    private static IEnumerable<IrExpression> NodeExpressions(LogicalNode node)
    {
        switch (node)
        {
            case SchemaScanNode scan:
                return scan.Arguments;
            case InterpretSourceNode interpret:
                return interpret.Arguments;
            case DescNode desc:
                return desc.Arguments;
            case AccessMethodSourceNode accessMethod:
                return [accessMethod.MethodCallExpression];
            case FilterNode filter:
                return [filter.Predicate];
            case ProjectNode project:
                return project.Fields.Select(static field => field.Expression);
            case HavingFilterNode having:
                return [having.Predicate];
            case QualifyFilterNode qualify:
                return [qualify.Predicate];
            case SortNode sort:
                return sort.Keys.Select(static key => key.Expression);
            case AggregateNode aggregate:
                return AggregateExpressions(aggregate);
            case JoinNode join:
                return JoinExpressions(join);
            case WindowNode window:
                return WindowExpressions(window);
            case UnpivotNode unpivot:
                return UnpivotExpressions(unpivot);
            case ValuesScanNode values:
                return values.Rows.SelectMany(static row => row.Fields).Select(static field => field.Value);
            default:
                return [];
        }
    }

    private static IEnumerable<IrExpression> AggregateExpressions(AggregateNode aggregate)
    {
        foreach (var groupKey in aggregate.GroupKeys)
            yield return groupKey;

        foreach (var binding in aggregate.Bindings)
        {
            foreach (var argument in binding.SetArguments)
                yield return argument;
            if (binding.FilterPredicate != null)
                yield return binding.FilterPredicate;
            foreach (var argument in binding.GetArguments)
                yield return argument;
        }
    }

    private static IEnumerable<IrExpression> JoinExpressions(JoinNode join)
    {
        yield return join.OnPredicate;
        if (join.TieBreak != null)
            yield return join.TieBreak.Expression;
    }

    private static IEnumerable<IrExpression> WindowExpressions(WindowNode window)
    {
        foreach (var registration in window.Registrations)
        {
            foreach (var partitionKey in registration.PartitionKeys)
                yield return partitionKey;
            foreach (var orderKey in registration.OrderKeys)
                yield return orderKey.Expression;
            foreach (var argument in registration.ValueArguments)
                yield return argument;
            if (registration.FilterPredicate != null)
                yield return registration.FilterPredicate;
        }
    }

    private static IEnumerable<IrExpression> UnpivotExpressions(UnpivotNode unpivot)
    {
        foreach (var entry in unpivot.Entries)
            yield return entry.Value;
        foreach (var field in unpivot.KeepFields)
            yield return field.Expression;
    }
}
