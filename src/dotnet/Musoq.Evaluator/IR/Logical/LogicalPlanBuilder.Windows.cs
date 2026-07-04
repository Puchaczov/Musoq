using System.Collections.Generic;
using System.Text;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;
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
            var signature = CreateWindowRegistrationSignature(registration);

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

    private static string CreateWindowRegistrationSignature(WindowRegistration registration)
    {
        var builder = new StringBuilder();
        builder.Append(registration.Function?.ToString() ?? registration.FunctionName);
        builder.Append('\u001E');
        builder.Append(registration.ReturnType.AssemblyQualifiedName);
        builder.Append('\u001E');
        builder.Append(registration.Frame?.Id);
        builder.Append('\u001E');
        AppendExpressions(builder, registration.PartitionKeys);
        builder.Append('\u001E');
        AppendOrderFields(builder, registration.OrderKeys);
        builder.Append('\u001E');
        AppendExpressions(builder, registration.ValueArguments);
        builder.Append('\u001E');
        if (registration.FilterPredicate != null)
            builder.Append(IrExpressionPrinter.Print(registration.FilterPredicate));
        return builder.ToString();
    }

    private static void AppendExpressions(StringBuilder builder, IrExpression[] expressions)
    {
        for (var index = 0; index < expressions.Length; index++)
        {
            if (index > 0)
                builder.Append('\u001F');

            builder.Append(IrExpressionPrinter.Print(expressions[index]));
        }
    }

    private static void AppendOrderFields(StringBuilder builder, OrderField[] orderFields)
    {
        for (var index = 0; index < orderFields.Length; index++)
        {
            if (index > 0)
                builder.Append('\u001F');

            var orderField = orderFields[index];
            builder.Append(orderField.Descending ? "DESC:" : "ASC:").Append(orderField.NullOrdering).Append(':');
            builder.Append(IrExpressionPrinter.Print(orderField.Expression));
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
                $"Window function '{functionName}' does not have a resolved factory method.");

        var partitionKeys = ConvertPartitionKeys(specification);
        var orderKeys = ConvertOrderKeys(specification);
        var valueArguments = ConvertWindowValueArguments(node);
        var filterPredicate = node.FunctionCall.FilterExpression == null
            ? null
            : _converter.Convert(node.FunctionCall.FilterExpression);
        var returnType = node.ReturnType ??
                         throw new InvalidOperationException($"Window function '{functionName}' has no inferred return type.");

        _windowRegistrations.Add(new WindowRegistration(
            function,
            functionName,
            partitionKeys,
            orderKeys,
            valueArguments,
            filterPredicate,
            windowIndex,
            returnType,
            specification?.Frame));

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
            || string.Equals(normalized, "denserank", StringComparison.OrdinalIgnoreCase);
    }

    private WindowSpecificationNode? ResolveWindowSpecification(WindowFunctionNode node)
    {
        if (!node.IsNamedWindowReference)
            return node.WindowSpecification;

        if (node.WindowName != null &&
            _windowDefinitions.TryGetValue(node.WindowName, out var specification))
            return specification;

        throw new InvalidOperationException($"Named window '{node.WindowName}' is not registered in the current query.");
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
                orderField.Order == Order.Descending, ConvertNullOrdering(orderField.NullOrdering));
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
