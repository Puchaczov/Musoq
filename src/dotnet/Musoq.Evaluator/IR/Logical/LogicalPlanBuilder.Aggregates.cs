using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using IrNodes = Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical;
public sealed partial class LogicalPlanBuilder
{
    public void Visit(AccessRefreshAggregationScoreNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var setMethod = node.Method ?? throw new InvalidOperationException($"Aggregate refresh method '{node.Name}' was not resolved before logical planning.");
        var setArgs = new List<IrExpression>();
        foreach (var arg in node.Arguments.Args)
            setArgs.Add(_converter.Convert(arg));
        var filterPredicate = node.FilterExpression == null ? null : _converter.Convert(node.FilterExpression);
        var displayName = GetAggregateDisplayName(node);

        _refreshMethods.Add(new RefreshMethodCapture(setMethod, setArgs, filterPredicate, displayName));
    }

    public void Visit(RefreshNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        foreach (var methodNode in node.Nodes)
        {
            var setMethod = methodNode.Method;
            if (setMethod is null)
                throw new InvalidOperationException($"Aggregate refresh method '{methodNode.Name}' was not resolved before logical planning.");
            var setArgs = new List<IrExpression>();
            foreach (var arg in methodNode.Arguments.Args)
                setArgs.Add(_converter.Convert(arg));
            var filterPredicate = methodNode.FilterExpression == null
                ? null
                : _converter.Convert(methodNode.FilterExpression);
            var displayName = GetAggregateDisplayName(methodNode);

            _refreshMethods.Add(new RefreshMethodCapture(setMethod, setArgs, filterPredicate, displayName));
        }
    }
    private LogicalNode ExtractAggregateBindings(LogicalNode source)
    {
        var (aggregateNode, wrapperFactory) = FindAggregateNode(source);

        if (aggregateNode is null)
            return source;

        var refreshLookup = BuildRefreshLookup();

        if (refreshLookup.Captures.Count == 0)
            return source;

        var bindings = new List<AggregateBinding>();
        var bindingsByIdentifier = new Dictionary<string, AggregateBinding>(StringComparer.Ordinal);

        CollectBindingsFromProjectedFields(refreshLookup, bindings, bindingsByIdentifier);
        CollectBindingsFromSourceExpressions(source, refreshLookup, bindings, bindingsByIdentifier);
        CollectBindingsFromOrderFields(refreshLookup, bindings, bindingsByIdentifier);

        if (bindings.Count == 0)
            return source;

        RewriteProjectedFieldsWithAggregateRefs(bindingsByIdentifier);
        RewriteHavingPredicateWithAggregateRefs(bindingsByIdentifier);
        RewriteOrderFieldsWithAggregateRefs(bindingsByIdentifier);

        var updatedAggregate = aggregateNode with { Bindings = [.. bindings] };
        return RewriteSourceExpressionsWithAggregateRefs(wrapperFactory(updatedAggregate), bindingsByIdentifier);
    }

    private static (IrNodes.AggregateNode? Aggregate, Func<LogicalNode, LogicalNode> WrapperFactory) FindAggregateNode(LogicalNode source)
    {
        if (source is IrNodes.AggregateNode aggregate)
            return (aggregate, node => node);

        if (source is IrNodes.HavingFilterNode having)
            return CreateNestedAggregateResult(
                having.Input,
                node => new IrNodes.HavingFilterNode(having.Predicate, node));

        if (source is IrNodes.WindowNode window)
            return CreateNestedAggregateResult(
                window.Input,
                node => new IrNodes.WindowNode(window.Registrations, node));

        if (source is IrNodes.QualifyFilterNode qualify)
            return CreateNestedAggregateResult(
                qualify.Input,
                node => new IrNodes.QualifyFilterNode(qualify.Predicate, node));

        return (null, static node => node);
    }

    private static (IrNodes.AggregateNode? Aggregate, Func<LogicalNode, LogicalNode> WrapperFactory) CreateNestedAggregateResult(
        LogicalNode input,
        Func<LogicalNode, LogicalNode> wrap)
    {
        var nested = FindAggregateNode(input);
        return nested.Aggregate is null
            ? nested
            : (nested.Aggregate, node => wrap(nested.WrapperFactory(node)));
    }
}
