using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionExpression> CreateAggregateFinalCall(
        string? identifier,
        AggregateFinalizationContext context)
    {
        return TryResolveAggregateBinding(identifier, context.BindingsByIdentifier, out var binding)
            ? CreateAggregateFinalCall(binding, context)
            : BuildResult<ExecutionExpression>.Unsupported(
                $"Execution IR {context.AggregateKind} final expression lowering cannot bind aggregate reference '{identifier}'.");
    }

    private static BuildResult<ExecutionExpression> CreateAggregateFinalCall(
        AggregateBinding binding,
        AggregateFinalizationContext context)
    {
        var arguments = ConvertAggregateArguments(binding.GetArguments, new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase));
        var accumulator = TryResolveTypedAggregateFinalAccumulator(binding, context.TypedAccumulators);
        if (accumulator == null)
        {
            return BuildResult<ExecutionExpression>.Unsupported(
                $"Execution IR {context.AggregateKind} final expression lowering requires typed aggregate kernel metadata for aggregate '{binding.Identifier}'.");
        }

        return BuildResult<ExecutionExpression>.Success(new ExecutionAggregateCall(
            context.Group,
            binding.GetMethod,
            arguments,
            binding.GetMethod.ReturnType,
            accumulator,
            binding.DisplayName));
    }

    private static int? TryGetGroupKeyExpressionIndex(
        IrExpression expression,
        AggregateFinalizationGroupKeys groupKeys)
    {
        for (var index = 0; index < groupKeys.Names.Count; index++)
        {
            if (IsGroupKeyExpression(expression, groupKeys.Expressions[index], groupKeys.Names[index]))
                return index;
        }

        return null;
    }

    private static bool IsGroupKeyExpression(IrExpression expression, IrExpression groupKey, string groupKeyName)
    {
        var expressionText = IrExpressionPrinter.Print(expression);
        var groupKeyText = IrExpressionPrinter.Print(groupKey);

        if (string.Equals(expressionText, groupKeyText, StringComparison.OrdinalIgnoreCase))
            return true;

        var normalizedExpression = AggregateRefRewriter.NormalizeIdentifier(expressionText);
        if (!string.IsNullOrWhiteSpace(normalizedExpression) &&
            (string.Equals(normalizedExpression, AggregateRefRewriter.NormalizeIdentifier(groupKeyText), StringComparison.OrdinalIgnoreCase) ||
             string.Equals(normalizedExpression, AggregateRefRewriter.NormalizeIdentifier(groupKeyName), StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (expression is not ColumnRef columnRef)
            return false;

        if (string.Equals(columnRef.ColumnName, groupKeyName, StringComparison.OrdinalIgnoreCase))
            return true;

        return string.Equals($"{columnRef.Alias}.{columnRef.ColumnName}", groupKeyName, StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectedField[]? SelectAggregateOutputFields(
        ProjectedField[] fields,
        AggregateBinding[] bindings)
    {
        if (fields.Length == bindings.Length)
            return NormalizeProjectedFieldIndexes(fields);

        if (fields.Length < bindings.Length)
            return null;

        return NormalizeProjectedFieldIndexes(fields[^bindings.Length..]);
    }

    private static ProjectedField[] NormalizeProjectedFieldIndexes(ProjectedField[] fields)
    {
        return fields
            .Select((field, index) => field with { OutputIndex = index })
            .ToArray();
    }

    private static Dictionary<string, AggregateBinding> CreateAggregateBindingsMap(IReadOnlyList<AggregateBinding> bindings)
    {
        var bindingsByIdentifier = new Dictionary<string, AggregateBinding>(StringComparer.OrdinalIgnoreCase);
        var ambiguousKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in bindings)
        {
            AddAggregateBindingKey(bindingsByIdentifier, ambiguousKeys, binding.Identifier, binding);
            AddAggregateBindingKey(bindingsByIdentifier, ambiguousKeys, AggregateRefRewriter.NormalizeIdentifier(binding.Identifier), binding);
            AddAggregateBindingKey(bindingsByIdentifier, ambiguousKeys, binding.ColumnName, binding);
            AddAggregateBindingKey(bindingsByIdentifier, ambiguousKeys, AggregateRefRewriter.NormalizeIdentifier(binding.ColumnName), binding);
        }

        return bindingsByIdentifier;
    }

    private static void AddAggregateBindingKey(
        Dictionary<string, AggregateBinding> bindingsByIdentifier,
        HashSet<string> ambiguousKeys,
        string? key,
        AggregateBinding binding)
    {
        if (string.IsNullOrWhiteSpace(key) || ambiguousKeys.Contains(key))
            return;

        if (!bindingsByIdentifier.TryGetValue(key, out var existing))
        {
            bindingsByIdentifier[key] = binding;
            return;
        }

        if (ReferenceEquals(existing, binding))
            return;

        bindingsByIdentifier.Remove(key);
        ambiguousKeys.Add(key);
    }

    private static bool TryResolveProjectedAggregate(
        IrExpression expression,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier,
        out AggregateBinding binding)
    {
        binding = null!;
        if (bindings.Count == 0)
            return false;

        switch (expression)
        {
            case AggregateRef aggregateRef:
                return TryResolveAggregateBinding(aggregateRef.Identifier, bindingsByIdentifier, out binding);
            case ColumnRef columnRef:
                return TryResolveAggregateBinding(columnRef.ColumnName, bindingsByIdentifier, out binding);
            case MethodCall methodCall:
                var rawIdentifier = GetRawAggregateIdentifier(methodCall);
                if (TryResolveAggregateBinding(rawIdentifier, bindingsByIdentifier, out binding))
                    return true;

                var identifier = AggregateRefRewriter.ExtractIdentifier(methodCall);
                if (!string.Equals(identifier, rawIdentifier, StringComparison.OrdinalIgnoreCase) &&
                    TryResolveAggregateBinding(identifier, bindingsByIdentifier, out binding))
                {
                    return true;
                }

                var matchingBindings = bindings
                    .Where(candidate => IsAggregateMethodCallCompatible(methodCall, candidate))
                    .ToArray();
                if (matchingBindings.Length == 1)
                {
                    binding = matchingBindings[0];
                    return true;
                }

                break;
        }

        return false;
    }

    private static bool IsAggregateMethodCallCompatible(MethodCall methodCall, AggregateBinding binding)
    {
        if (methodCall.Method == binding.GetMethod)
            return true;

        if (!string.Equals(methodCall.Method.Name, binding.GetMethod.Name, StringComparison.Ordinal))
            return false;

        if (AggregateRefRewriter.IsAggregateMethod(methodCall.Method))
            return true;

        if (string.IsNullOrWhiteSpace(methodCall.Alias))
            return false;

        var identifier = AggregateRefRewriter.ExtractIdentifier(methodCall);
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        if (MatchesAggregateBindingIdentifier(identifier, binding.Identifier) ||
            MatchesAggregateBindingIdentifier(identifier, binding.ColumnName))
        {
            return true;
        }

        return identifier.StartsWith($"{binding.GetMethod.Name}(", StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAggregateBindingIdentifier(string? identifier, string? bindingIdentifier)
    {
        var normalizedIdentifier = AggregateRefRewriter.NormalizeIdentifier(identifier);
        var normalizedBindingIdentifier = AggregateRefRewriter.NormalizeIdentifier(bindingIdentifier);

        return !string.IsNullOrWhiteSpace(normalizedIdentifier) &&
               !string.IsNullOrWhiteSpace(normalizedBindingIdentifier) &&
               string.Equals(normalizedIdentifier, normalizedBindingIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveAggregateBinding(
        string? identifier,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier,
        out AggregateBinding binding)
    {
        if (!string.IsNullOrWhiteSpace(identifier) && bindingsByIdentifier.TryGetValue(identifier, out binding!))
            return true;

        var normalized = AggregateRefRewriter.NormalizeIdentifier(identifier);
        if (!string.IsNullOrWhiteSpace(normalized) && bindingsByIdentifier.TryGetValue(normalized, out binding!))
            return true;

        binding = null!;
        return false;
    }
}
