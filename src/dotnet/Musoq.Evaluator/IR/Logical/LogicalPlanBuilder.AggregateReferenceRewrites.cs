using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private void RewriteProjectedFieldsWithAggregateRefs(Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        for (var index = 0; index < _projectedFields.Count; index++)
        {
            var field = _projectedFields[index];
            var rewritten = AggregateRefRewriter.Rewrite(field.Expression, bindingsByIdentifier);

            if (!ReferenceEquals(rewritten, field.Expression))
                _projectedFields[index] = field with { Expression = rewritten };
        }
    }

    private void RewriteHavingPredicateWithAggregateRefs(Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        if (_havingPredicate is null)
            return;

        _havingPredicate = AggregateRefRewriter.Rewrite(_havingPredicate, bindingsByIdentifier);
    }

    private void RewriteOrderFieldsWithAggregateRefs(Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        for (var index = 0; index < _orderFields.Count; index++)
        {
            var orderField = _orderFields[index];
            var rewritten = AggregateRefRewriter.Rewrite(orderField.Expression, bindingsByIdentifier);

            if (!ReferenceEquals(rewritten, orderField.Expression))
                _orderFields[index] = orderField with { Expression = rewritten };
        }
    }

    private static OrderField[] RewriteOrderFieldsWithAggregateRefs(
        OrderField[] fields,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        var rewritten = new OrderField[fields.Length];

        for (var index = 0; index < fields.Length; index++)
        {
            var field = fields[index];
            rewritten[index] = field with
            {
                Expression = AggregateRefRewriter.Rewrite(field.Expression, bindingsByIdentifier)
            };
        }

        return rewritten;
    }

    private static LogicalNode RewriteSourceExpressionsWithAggregateRefs(
        LogicalNode source,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        return source switch
        {
            IrNodes.HavingFilterNode having => new IrNodes.HavingFilterNode(
                AggregateRefRewriter.Rewrite(having.Predicate, bindingsByIdentifier),
                RewriteSourceExpressionsWithAggregateRefs(having.Input, bindingsByIdentifier)),
            IrNodes.WindowNode window => new IrNodes.WindowNode(
                RewriteWindowRegistrationsWithAggregateRefs(window.Registrations, bindingsByIdentifier),
                RewriteSourceExpressionsWithAggregateRefs(window.Input, bindingsByIdentifier)),
            IrNodes.QualifyFilterNode qualify => new IrNodes.QualifyFilterNode(
                AggregateRefRewriter.Rewrite(qualify.Predicate, bindingsByIdentifier),
                RewriteSourceExpressionsWithAggregateRefs(qualify.Input, bindingsByIdentifier)),
            _ => source
        };
    }

    private static WindowRegistration[] RewriteWindowRegistrationsWithAggregateRefs(
        WindowRegistration[] registrations,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        var rewritten = new WindowRegistration[registrations.Length];

        for (var index = 0; index < registrations.Length; index++)
            rewritten[index] = RewriteWindowRegistrationWithAggregateRefs(registrations[index], bindingsByIdentifier);

        return rewritten;
    }

    private static WindowRegistration RewriteWindowRegistrationWithAggregateRefs(
        WindowRegistration registration,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        return registration with
        {
            PartitionKeys = RewriteExpressionsWithAggregateRefs(registration.PartitionKeys, bindingsByIdentifier),
            OrderKeys = RewriteOrderFieldsWithAggregateRefs(registration.OrderKeys, bindingsByIdentifier),
            ValueArguments = RewriteExpressionsWithAggregateRefs(registration.ValueArguments, bindingsByIdentifier),
            FilterPredicate = registration.FilterPredicate == null
                ? null
                : AggregateRefRewriter.Rewrite(registration.FilterPredicate, bindingsByIdentifier)
        };
    }

    private static IrExpression[] RewriteExpressionsWithAggregateRefs(
        IrExpression[] expressions,
        Dictionary<string, AggregateBinding> bindingsByIdentifier)
    {
        var rewritten = new IrExpression[expressions.Length];

        for (var index = 0; index < expressions.Length; index++)
            rewritten[index] = AggregateRefRewriter.Rewrite(expressions[index], bindingsByIdentifier);

        return rewritten;
    }
}
