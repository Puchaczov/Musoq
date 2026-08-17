using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Musoq.Evaluator.IR.Bindings;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static AggregateSetBuildResult CreateAggregateSetNodes(
        IReadOnlyList<AggregateBinding> bindings,
        ExecutionVariable group,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<Type, ExecutionVariable> libraries,
        AggregateGroupLowering aggregateGroup)
    {
        var nodes = new List<ExecutionNode>();
        var typedAccumulators = new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase);

        foreach (var binding in bindings)
        {
            var arguments = ConvertAggregateArguments(binding.SetArguments, sourceLookup, libraries);
            var filterPredicate = binding.FilterPredicate == null
                ? null
                : ExecutionExpressionConverter.Convert(binding.FilterPredicate, sourceLookup);

            if (!TryGetTypedAccumulator(binding, aggregateGroup, out var accumulator))
            {
                return AggregateSetBuildResult.Unsupported(
                    $"Execution IR aggregate lowering requires typed aggregate kernel metadata for aggregate '{binding.Identifier}'.");
            }

            if (!TryGetTypedAggregateInput(binding, arguments, out var accumulatorInput))
            {
                return AggregateSetBuildResult.Unsupported(
                    $"Execution IR aggregate lowering cannot create typed aggregate input for aggregate '{binding.Identifier}'.");
            }

            nodes.Add(new ExecutionAggregateSet(
                group,
                binding.SetMethod,
                arguments,
                filterPredicate,
                accumulator,
                accumulatorInput));
            AddTypedAccumulatorKeys(typedAccumulators, binding, accumulator);
        }

        return AggregateSetBuildResult.Success(nodes, typedAccumulators);
    }

    private static bool TryGetTypedAccumulator(
        AggregateBinding binding,
        AggregateGroupLowering aggregateGroup,
        [NotNullWhen(true)] out AggregateAccumulatorField? accumulator)
    {
        return TryResolveAggregateAccumulator(binding, aggregateGroup.AccumulatorsByIdentifier, out accumulator);
    }

    private static bool TryResolveAggregateAccumulator(
        AggregateBinding binding,
        IReadOnlyDictionary<string, AggregateAccumulatorField> accumulatorsByIdentifier,
        [NotNullWhen(true)] out AggregateAccumulatorField? accumulator)
    {
        if (TryGetAccumulator(accumulatorsByIdentifier, binding.Identifier, out accumulator) ||
            TryGetAccumulator(accumulatorsByIdentifier, AggregateRefRewriter.NormalizeIdentifier(binding.Identifier), out accumulator) ||
            TryGetAccumulator(accumulatorsByIdentifier, binding.ColumnName, out accumulator) ||
            TryGetAccumulator(accumulatorsByIdentifier, AggregateRefRewriter.NormalizeIdentifier(binding.ColumnName), out accumulator))
        {
            return true;
        }

        accumulator = null;
        return false;
    }

    private static bool TryGetAccumulator(
        IReadOnlyDictionary<string, AggregateAccumulatorField> accumulatorsByIdentifier,
        string? key,
        [NotNullWhen(true)] out AggregateAccumulatorField? accumulator)
    {
        accumulator = null;
        return !string.IsNullOrWhiteSpace(key) &&
               accumulatorsByIdentifier.TryGetValue(key, out accumulator);
    }

    private static bool TryGetTypedAggregateInput(
        AggregateBinding binding,
        ExecutionExpression[] arguments,
        out ExecutionExpression? input)
    {
        if (binding.Kernel is not null)
        {
            var argumentCount = binding.Kernel.InputShape.ArgumentTypes.Count;
            var valueArguments = AggregateKernelArgumentSelector.SelectValueArgumentsAfterGroup(
                arguments);
            if (argumentCount == 0)
            {
                input = null;
                return valueArguments.Length == 0;
            }

            if (valueArguments.Length != argumentCount)
            {
                input = null!;
                return false;
            }

            input = argumentCount == 1
                ? valueArguments[0]
                : new ExecutionValueTupleKey(valueArguments, binding.Kernel.InputShape.InputType);
            return true;
        }

        if (arguments.Length is < 2 or > 3)
        {
            input = null!;
            return false;
        }

        input = arguments[1];
        return true;
    }

    private static void AddTypedAccumulatorKeys(
        IDictionary<string, AggregateAccumulatorField> typedAccumulators,
        AggregateBinding binding,
        AggregateAccumulatorField accumulator)
    {
        AddTypedAccumulatorKey(typedAccumulators, binding.Identifier, accumulator);
        AddTypedAccumulatorKey(typedAccumulators, AggregateRefRewriter.NormalizeIdentifier(binding.Identifier), accumulator);
        AddTypedAccumulatorKey(typedAccumulators, binding.ColumnName, accumulator);
        AddTypedAccumulatorKey(typedAccumulators, AggregateRefRewriter.NormalizeIdentifier(binding.ColumnName), accumulator);
    }

    private static void AddTypedAccumulatorKey(
        IDictionary<string, AggregateAccumulatorField> typedAccumulators,
        string? key,
        AggregateAccumulatorField accumulator)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        typedAccumulators.TryAdd(key, accumulator);
    }

    private static AggregateCapturedField? TryResolveAggregateCapturedField(
        AggregateGroupLowering aggregateGroup,
        string valueName)
    {
        return TryResolveAggregateCapturedField(aggregateGroup.CapturedFieldsByName, valueName);
    }

    private static AggregateCapturedField? TryResolveAggregateCapturedField(
        AggregateGroupShape aggregateGroupShape,
        string valueName)
    {
        return TryResolveAggregateCapturedField(
            CreateAggregateCapturedFieldsLookup(aggregateGroupShape.CapturedFields),
            valueName);
    }

    private static AggregateCapturedField? TryResolveAggregateCapturedField(
        IReadOnlyDictionary<string, AggregateCapturedField> capturedFieldsByName,
        string valueName)
    {
        var normalizedValueName = AggregateRefRewriter.NormalizeIdentifier(valueName);
        if (capturedFieldsByName.TryGetValue(valueName, out var capturedField) ||
            (!string.IsNullOrWhiteSpace(normalizedValueName) &&
             capturedFieldsByName.TryGetValue(normalizedValueName, out capturedField)))
        {
            return capturedField;
        }

        return null;
    }

    private static AggregateAccumulatorField? TryResolveTypedAggregateFinalAccumulator(
        AggregateBinding binding,
        IReadOnlyDictionary<string, AggregateAccumulatorField> typedAccumulators)
    {
        return TryResolveAggregateAccumulator(binding, typedAccumulators, out var accumulator)
            ? accumulator
            : null;
    }
}
