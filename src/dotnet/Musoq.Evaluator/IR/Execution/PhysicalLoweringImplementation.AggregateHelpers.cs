using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionExpression[] ConvertAggregateArguments(
        IReadOnlyList<IrExpression> arguments,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets = null)
    {
        return arguments
            .Select(argument => ExecutionExpressionConverter.Convert(argument, sourceLookup, null, methodTargets))
            .ToArray();
    }

    private static bool TryCreateAggregateGroupLowering(
        string resultTableName,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyList<ProjectedField> fields,
        IrExpression? havingPredicate,
        IReadOnlyList<PostOperation> postOperations,
        out AggregateGroupLowering aggregateGroup,
        out string unsupportedReason)
    {
        aggregateGroup = null!;
        unsupportedReason = string.Empty;

        if (!CanUseTypedAggregateGroupKeys(groupKeys))
        {
            var unsupportedKeyType = groupKeys.Types.First(IsUnsupportedTypedAggregateGroupKeyType);
            unsupportedReason =
                $"Execution IR aggregate lowering requires typed aggregate group keys. Key type {unsupportedKeyType.FullName} cannot be represented by a generated group shape.";
            return false;
        }

        var accumulators = new List<AggregateAccumulatorField>(bindings.Count);
        var accumulatorsByIdentifier = new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase);
        var keys = CreateAggregateGroupKeyFields(groupKeys);

        for (var index = 0; index < bindings.Count; index++)
        {
            var binding = bindings[index];
            if (!TryCreateAggregateAccumulatorField(binding, index, keys.Count, out var accumulator))
            {
                unsupportedReason =
                    $"Execution IR aggregate lowering requires typed aggregate kernel metadata for aggregate '{binding.Identifier}'.";
                return false;
            }

            accumulators.Add(accumulator);
            AddTypedAccumulatorKeys(accumulatorsByIdentifier, binding, accumulator);
        }

        var capturedFields = CreateAggregateCapturedFields(groupKeys, fields, havingPredicate, postOperations);
        if (capturedFields == null)
        {
            unsupportedReason =
                "Execution IR aggregate lowering cannot create typed aggregate captured fields for the final projection.";
            return false;
        }

        var capturedFieldsByName = CreateAggregateCapturedFieldsLookup(capturedFields);
        var plan = AggregateGroupPlanner.Create(
            CreateAggregateGroupShapeName(resultTableName),
            keys,
            capturedFields,
            accumulators);

        aggregateGroup = new AggregateGroupLowering(plan, accumulatorsByIdentifier, capturedFieldsByName);
        return true;
    }

    private static bool CanUseTypedAggregateGroupKeys(AggregateFinalizationGroupKeys groupKeys)
    {
        return groupKeys.Types.All(IsSafeTypedAggregateGroupKeyType);
    }

    private static bool IsSafeTypedAggregateGroupKeyType(Type type)
    {
        var keyType = Nullable.GetUnderlyingType(type) ?? type;

        return keyType.IsPrimitive ||
               keyType.IsEnum ||
               keyType == typeof(string) ||
               keyType == typeof(decimal) ||
               keyType == typeof(DateTime) ||
               keyType == typeof(DateTimeOffset) ||
               keyType == typeof(TimeSpan) ||
               keyType == typeof(Guid);
    }

    private static bool IsUnsupportedTypedAggregateGroupKeyType(Type type)
    {
        return !IsSafeTypedAggregateGroupKeyType(type);
    }

    private static bool TryCreateAggregateAccumulatorField(
        AggregateBinding binding,
        int ordinal,
        int groupKeyCount,
        out AggregateAccumulatorField accumulator)
    {
        accumulator = null!;
        if (binding.Kernel is null)
            return false;

        var parentDepth = Math.Min(Math.Max(binding.ParentDepth, 0), groupKeyCount);
        var ownerPrefixLength = Math.Max(0, groupKeyCount - parentDepth);
        var ownerFieldName = ownerPrefixLength == groupKeyCount
            ? null
            : AggregateGroupPlanner.CreateOwnerFieldName(ownerPrefixLength);
        accumulator = new AggregateAccumulatorField(
            binding.Identifier,
            $"__agg{ordinal.ToString(CultureInfo.InvariantCulture)}",
            binding.Kernel,
            parentDepth,
            ownerPrefixLength,
            ownerFieldName);
        return true;
    }

    private static List<AggregateCapturedField>? CreateAggregateCapturedFields(
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<ProjectedField> fields,
        IrExpression? havingPredicate,
        IReadOnlyList<PostOperation> postOperations)
    {
        var capturedFields = new List<AggregateCapturedField>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var column in CollectAggregateFinalSourceColumns(fields, havingPredicate, postOperations, groupKeys))
        {
            var valueName = CreateAggregateColumnValueName(column);
            if (seen.Add(valueName))
                capturedFields.Add(CreateAggregateCapturedField(valueName, column.ReturnType, capturedFields.Count));
        }

        return capturedFields;
    }

    private static AggregateCapturedField CreateAggregateCapturedField(
        string name,
        Type type,
        int index)
    {
        return new AggregateCapturedField(
            name,
            $"__slot{index.ToString(CultureInfo.InvariantCulture)}",
            type);
    }

    private static Dictionary<string, AggregateCapturedField> CreateAggregateCapturedFieldsLookup(
        IReadOnlyList<AggregateCapturedField> capturedFields)
    {
        var lookup = new Dictionary<string, AggregateCapturedField>(StringComparer.OrdinalIgnoreCase);
        foreach (var capturedField in capturedFields)
        {
            AddAggregateCapturedFieldLookup(lookup, capturedField.Name, capturedField);
            AddAggregateCapturedFieldLookup(
                lookup,
                AggregateRefRewriter.NormalizeIdentifier(capturedField.Name),
                capturedField);
        }

        return lookup;
    }

    private static void AddAggregateCapturedFieldLookup(
        IDictionary<string, AggregateCapturedField> lookup,
        string? key,
        AggregateCapturedField capturedField)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        lookup.TryAdd(key, capturedField);
    }

    private static List<AggregateGroupKeyField> CreateAggregateGroupKeyFields(
        AggregateFinalizationGroupKeys groupKeys)
    {
        var keys = new List<AggregateGroupKeyField>(groupKeys.Names.Count);

        for (var index = 0; index < groupKeys.Names.Count; index++)
        {
            keys.Add(new AggregateGroupKeyField(
                groupKeys.Names[index],
                $"__key{index.ToString(CultureInfo.InvariantCulture)}",
                groupKeys.Types[index]));
        }

        return keys;
    }

    private static string CreateAggregateGroupShapeName(string resultTableName)
    {
        var candidate = CreateIdentifierCandidate($"{CreateGeneratedTypeNamePrefix(resultTableName)}AggregateGroup", 0);
        return ExecutionSymbolicNamePolicy.IsReservedIdentifier(candidate)
            ? $"_{candidate}"
            : candidate;
    }

    private static IReadOnlyList<RowShape> CreateAggregateResultShapes(
        IReadOnlyList<RowShape> sourceShapes,
        AggregateGroupPlan aggregateGroupPlan,
        GeneratedRowShape resultShape)
    {
        return [..sourceShapes, ..aggregateGroupPlan.Levels.Select(static level => level.Shape), resultShape];
    }
}
