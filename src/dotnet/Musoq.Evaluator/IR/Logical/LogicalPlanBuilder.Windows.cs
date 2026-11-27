using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using WindowFunctionRefRewriter = Musoq.Evaluator.IR.Expressions.WindowFunctionRefRewriter;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    public void Visit(WindowNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _windowDefinitions.Clear();

        foreach (var definition in node.Definitions)
            _windowDefinitions[definition.Name] = definition.Specification;
    }

    public void Visit(QualifyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _qualifyPredicate = _converter.Convert(node.Expression);
    }

    private WindowRegistration[] DeduplicateWindowRegistrations(out Dictionary<int, int> windowIndexMap)
    {
        windowIndexMap = new Dictionary<int, int>();
        var uniqueRegistrations = new List<WindowRegistration>(_windowRegistrations.Count);
        var indexBySignature = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var index = 0; index < _windowRegistrations.Count; index++)
        {
            var registration = _windowRegistrations[index];
            var signature = WindowRegistrationIdentity.Create(registration);

            if (indexBySignature.TryGetValue(signature, out var deduplicatedIndex))
            {
                windowIndexMap[index] = deduplicatedIndex;
                continue;
            }

            deduplicatedIndex = uniqueRegistrations.Count;
            indexBySignature[signature] = deduplicatedIndex;

            if (deduplicatedIndex != index)
                windowIndexMap[index] = deduplicatedIndex;

            uniqueRegistrations.Add(registration with { WindowIndex = deduplicatedIndex });
        }

        return [.. uniqueRegistrations];
    }

    private void RewriteWindowReferences(IReadOnlyDictionary<int, int> windowIndexMap)
    {
        if (_havingPredicate is not null)
            _havingPredicate = WindowFunctionRefRewriter.Rewrite(_havingPredicate, windowIndexMap);

        if (_qualifyPredicate is not null)
            _qualifyPredicate = WindowFunctionRefRewriter.Rewrite(_qualifyPredicate, windowIndexMap);

        RewriteProjectedFields(windowIndexMap);
        RewriteOrderFields(windowIndexMap);
    }

    private void RewriteProjectedFields(IReadOnlyDictionary<int, int> windowIndexMap)
    {
        for (var index = 0; index < _projectedFields.Count; index++)
        {
            var field = _projectedFields[index];
            var expression = WindowFunctionRefRewriter.Rewrite(field.Expression, windowIndexMap);

            if (!ReferenceEquals(expression, field.Expression))
                _projectedFields[index] = field with { Expression = expression };
        }
    }

    private void RewriteOrderFields(IReadOnlyDictionary<int, int> windowIndexMap)
    {
        for (var index = 0; index < _orderFields.Count; index++)
        {
            var orderField = _orderFields[index];
            var expression = WindowFunctionRefRewriter.Rewrite(orderField.Expression, windowIndexMap);

            if (!ReferenceEquals(expression, orderField.Expression))
                _orderFields[index] = orderField with { Expression = expression };
        }
    }

    private WindowFunctionRef RegisterWindowFunction(WindowFunctionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var specification = ResolveWindowSpecification(node);
        var windowIndex = _windowRegistrations.Count;
        var functionName = node.FunctionCall.Name;
        var function = node.FunctionCall.Method;

        if (function is null && !IsBuiltInWindowFunctionWithoutFactory(functionName))
            throw new InvalidOperationException(
                $"Window function '{functionName}' does not have a resolved factory method."
            );

        var partitionKeys = ConvertPartitionKeys(specification);
        var orderKeys = ConvertOrderKeys(specification);
        var valueArguments = ConvertWindowValueArguments(node);
        var filterPredicate =
            node.FunctionCall.FilterExpression == null ? null : _converter.Convert(node.FunctionCall.FilterExpression);
        var returnType =
            node.ReturnType
            ?? throw new InvalidOperationException($"Window function '{functionName}' has no inferred return type.");

        _windowRegistrations.Add(
            new WindowRegistration(
                function,
                functionName,
                partitionKeys,
                orderKeys,
                valueArguments,
                filterPredicate,
                windowIndex,
                returnType,
                specification?.Frame
            )
            {
                NumbersQualifiedRows =
                    node.FunctionCall.FunctionToken is Visitors.ResultRowNumberProjection.QualifiedPositionToken,
            }
        );

        return new WindowFunctionRef(windowIndex, returnType);
    }

    private static bool IsBuiltInWindowFunctionWithoutFactory(string functionName)
    {
        if (string.IsNullOrEmpty(functionName))
            return false;

        var normalized = functionName.Replace("_", string.Empty, StringComparison.Ordinal);
        return string.Equals(normalized, "lag", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "lead", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "rownumber", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "rank", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "denserank", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "percentrank", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "cumedist", StringComparison.OrdinalIgnoreCase);
    }

    private WindowSpecificationNode? ResolveWindowSpecification(WindowFunctionNode node)
    {
        if (!node.IsNamedWindowReference)
            return node.WindowSpecification;

        if (node.WindowName != null && _windowDefinitions.TryGetValue(node.WindowName, out var specification))
            return specification;

        throw new InvalidOperationException(
            $"Named window '{node.WindowName}' is not registered in the current query."
        );
    }

    private IrExpression[] ConvertPartitionKeys(WindowSpecificationNode? specification)
    {
        if (specification == null || specification.PartitionFields.Length == 0)
            return [];

        var partitionKeys = new IrExpression[specification.PartitionFields.Length];

        for (var index = 0; index < specification.PartitionFields.Length; index++)
            partitionKeys[index] = _converter.Convert(specification.PartitionFields[index].Expression);

        return partitionKeys;
    }

    private OrderField[] ConvertOrderKeys(WindowSpecificationNode? specification)
    {
        if (specification == null || specification.OrderByFields.Length == 0)
            return [];

        var orderKeys = new OrderField[specification.OrderByFields.Length];

        for (var index = 0; index < specification.OrderByFields.Length; index++)
        {
            var orderField = specification.OrderByFields[index];
            orderKeys[index] = new OrderField(
                _converter.Convert(orderField.Expression),
                orderField.Order == Order.Descending,
                ConvertNullOrdering(orderField.NullOrdering)
            );
        }

        return orderKeys;
    }

    private IrExpression[] ConvertWindowValueArguments(WindowFunctionNode node)
    {
        var arguments = node.FunctionCall.Arguments?.Args;

        if (arguments == null || arguments.Length == 0)
            return [];

        var valueArguments = new IrExpression[arguments.Length];

        for (var index = 0; index < arguments.Length; index++)
            valueArguments[index] = _converter.Convert(arguments[index]);

        return valueArguments;
    }
}
