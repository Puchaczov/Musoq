using System.Collections.Generic;
using System.Linq;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Execution;

/// <summary>
/// Checks the binding contract at the last target-neutral boundary.  A renderer
/// must never have to guess how a field is read when the field belongs to a
/// known row shape.
/// </summary>
internal static class ExecutionBindingInvariantValidator
{
    public static void Validate(ExecutionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var shapesByAlias = plan.Shapes
            .Select(static shape => (Shape: shape, HasAlias: RowShapeLookup.TryResolveSourceAlias(shape, out var alias), Alias: alias))
            .Where(static item => item.HasAlias)
            .GroupBy(static item => item.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static item => item.Shape).ToArray(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var fieldRead in ExecutionIrAnalysis.CollectExpressions<ExecutionFieldRead>(plan.Body))
        {
            if (string.IsNullOrWhiteSpace(fieldRead.Alias) ||
                !shapesByAlias.TryGetValue(fieldRead.Alias, out var shapes))
                continue;

            var accessStrategy = fieldRead.AccessStrategy;
            if (accessStrategy == null)
            {
                if (shapes.Any(shape => IsUnresolvedIndexedField(shape, fieldRead.FieldName)))
                {
                    throw new InvalidOperationException(
                        $"Execution binding invariant violated: field '{fieldRead.Alias}.{fieldRead.FieldName}' " +
                        "belongs to a known row carrier but has no access strategy. Lowering must resolve the " +
                        "carrier-specific binding before rendering.");
                }

                continue;
            }

            foreach (var shape in shapes)
            {
                if (TryFindField(shape, fieldRead.FieldName, out var field) &&
                    !IsCompatible(field.AccessStrategy, accessStrategy))
                {
                    throw new InvalidOperationException(
                        $"Execution binding invariant violated: field '{fieldRead.Alias}.{fieldRead.FieldName}' " +
                        $"uses '{accessStrategy.GetType().Name}' for carrier '{shape.GetType().Name}', " +
                        $"but its declared binding is '{field.AccessStrategy.GetType().Name}'.");
                }
            }
        }

        foreach (var sourceScan in ExecutionIrAnalysis.FlattenNodes(plan.Body).OfType<ExecutionSourceScan>())
        {
            if (sourceScan.Binding.QueryRowSourceTransfer is not { } transfer)
                continue;

            ValidateQueryRowTransfer(sourceScan, transfer);
        }
    }

    private static void ValidateQueryRowTransfer(
        ExecutionSourceScan sourceScan,
        ExecutionQueryRowSourceTransfer transfer)
    {
        var sourceId = sourceScan.Binding.RuntimeContextId;
        var shapeFields = new List<QueryRowField>(transfer.Fields.Count);
        if (sourceScan.Binding.Fields.Count != transfer.Fields.Count)
        {
            throw new InvalidOperationException(
                $"Execution query-row transfer for source '{sourceId}' has {transfer.Fields.Count} fields, " +
                $"but its generated source binding has {sourceScan.Binding.Fields.Count} fields.");
        }

        for (var slot = 0; slot < transfer.Fields.Count; slot++)
        {
            var transferField = transfer.Fields[slot];
            if (transferField.Slot != slot)
            {
                throw new InvalidOperationException(
                    $"Execution query-row transfer for source '{sourceId}' has non-dense field slots.");
            }

            Type fieldType;
            Type sourceReadType;
            try
            {
                fieldType = transferField.FieldType.ResolveClrType();
                sourceReadType = transferField.SourceReadType.ResolveClrType();
            }
            catch (NotSupportedException exception)
            {
                throw new InvalidOperationException(
                    $"Execution query-row transfer field '{sourceId}.{transferField.Name}' has no usable CLR binding.",
                    exception);
            }

            if (fieldType == typeof(object) || !QueryRowField.IsSupportedFieldType(fieldType))
            {
                throw new InvalidOperationException(
                    $"Execution query-row transfer field '{sourceId}.{transferField.Name}' has unsupported CLR type '{fieldType}'.");
            }

            var binding = sourceScan.Binding.Fields[slot];
            var expectedAccess = QueryRowSourceNaming.CreateFieldName(slot);
            if (binding.OutputIndex != slot ||
                !string.Equals(binding.Name, transferField.Name, StringComparison.Ordinal) ||
                !string.Equals(binding.Type.StableId, transferField.FieldType.StableId, StringComparison.Ordinal) ||
                !string.Equals(binding.SourceReadType.StableId, transferField.SourceReadType.StableId, StringComparison.Ordinal) ||
                !EnumTypesMatch(binding.EnumType, transferField.EnumType) ||
                binding.AccessStrategy is not GeneratedFieldAccess generatedAccess ||
                !string.Equals(generatedAccess.FieldName, expectedAccess, StringComparison.Ordinal) ||
                !ReadModifiersMatch(binding.ReadModifiers, transferField.ReadModifiers))
            {
                throw new InvalidOperationException(
                    $"Execution query-row transfer field '{sourceId}.{transferField.Name}' is incompatible with its generated source binding at slot {slot}.");
            }

            shapeFields.Add(new QueryRowField(
                transferField.Slot,
                transferField.SourceColumnIndex,
                transferField.Name,
                fieldType,
                sourceReadType,
                transferField.EnumType,
                transferField.IsNullable,
                transferField.ReadModifiers,
                ColumnStability.Stable));
        }

        var expectedFingerprint = new QueryRowShape(shapeFields).Fingerprint;
        if (!string.Equals(expectedFingerprint, transfer.ShapeFingerprint, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Execution query-row transfer for source '{sourceId}' has a shape fingerprint that does not match its fields.");
        }
    }

    private static bool EnumTypesMatch(EnumTypeDescriptor? left, EnumTypeDescriptor? right)
    {
        return left == null
            ? right == null
            : right != null && string.Equals(left.Fingerprint, right.Fingerprint, StringComparison.Ordinal);
    }

    private static bool ReadModifiersMatch(
        IReadOnlyDictionary<string, string> left,
        IReadOnlyDictionary<string, string> right)
    {
        return left.Count == right.Count && left.All(pair =>
            right.TryGetValue(pair.Key, out var value) && string.Equals(pair.Value, value, StringComparison.Ordinal));
    }

    private static bool TryFindField(RowShape shape, string fieldName, out FieldBinding field)
    {
        var candidates = shape.Fields.Concat(shape is TableRowShape table ? table.Contexts : []);
        field = candidates.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, fieldName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, GetUnqualifiedName(fieldName), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.QualifiedName, GetUnqualifiedName(fieldName), StringComparison.OrdinalIgnoreCase))!;
        return field != null;
    }

    private static bool IsUnresolvedIndexedField(RowShape shape, string fieldName)
    {
        if (TryFindField(shape, fieldName, out var field))
            return field.AccessStrategy is PositionalAccess or NestedPositionalAccess;

        return !fieldName.Contains('.', StringComparison.Ordinal) &&
               !fieldName.Contains('[', StringComparison.Ordinal) &&
               shape.Fields.Any(static candidate => candidate.AccessStrategy is PositionalAccess);
    }

    private static bool IsCompatible(FieldAccessStrategy declared, FieldAccessStrategy actual)
    {
        return declared switch
        {
            PositionalAccess => actual is not
                (ClrPropertyAccess or ReflectedMemberAccess or ExpandoDictionaryAccess or RuntimeDynamicMemberAccess),
            NestedPositionalAccess => actual is not
                (NestedClrPropertyAccess or ReflectedMemberAccess or ExpandoDictionaryAccess or RuntimeDynamicMemberAccess),
            _ => true
        };
    }

    private static string GetUnqualifiedName(string name)
    {
        var separatorIndex = name.LastIndexOf('.');
        return separatorIndex < 0 ? name : name[(separatorIndex + 1)..];
    }
}
