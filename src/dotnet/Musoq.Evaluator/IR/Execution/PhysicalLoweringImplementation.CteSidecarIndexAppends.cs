using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IReadOnlyList<ExecutionCteSidecarAppendIndexSpec> CreateCteSidecarAppendIndexes(
        ExecutionAppendRow appendRow,
        IReadOnlyList<CteSidecarIndexBuild> builds)
    {
        return builds
            .Select(build => new ExecutionCteSidecarAppendIndexSpec(
                build.Index,
                CreateCteSidecarKeyExpression(build.Spec, appendRow.Values),
                ToExecutionIndexKind(build.Spec.Kind),
                build.Spec.KeyType,
                build.PayloadShape,
                build.PayloadShape == null
                    ? []
                    : CreateCteSidecarHashPayloadValues(build.PayloadShape, appendRow)))
            .ToArray();
    }

    private static ExecutionExpression CreateCteSidecarKeyExpression(
        CteSidecarIndexSpec spec,
        IReadOnlyList<ExecutionRowValue> values)
    {
        var valuesByName = values.ToDictionary(
            static value => value.FieldName,
            static value => value.Value,
            StringComparer.OrdinalIgnoreCase);
        var keyParts = spec.KeyColumns
            .Select(column => valuesByName.TryGetValue(column, out var value)
                ? value
                : throw new InvalidOperationException($"CTE sidecar key column '{column}' was not present in the append row values."))
            .ToArray();

        if (keyParts.Length == 1)
            return keyParts[0];

        if (IsValueTupleHashJoinKeyType(spec.KeyType))
            return new ExecutionValueTupleKey(keyParts, spec.KeyType);

        return new ExecutionMethodCall(
            CreateNullableHashJoinKeyMethod,
            keyParts,
            null,
            typeof(object));
    }

    private static string CreateCteSidecarIndexVariableName(string tableName, CteSidecarIndexSpec spec)
    {
        var keySuffix = string.Join(
            string.Empty,
            spec.KeyColumns.Select(static column => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(column)));
        return CreateIdentifierCandidate(
            $"{tableName}{spec.Kind}Sidecar{spec.IndexSlot.ToString(CultureInfo.InvariantCulture)}{keySuffix}",
            0);
    }

    private static ExecutionCteSidecarIndexKind ToExecutionIndexKind(CteSidecarIndexKind kind)
    {
        return kind switch
        {
            CteSidecarIndexKind.Hash => ExecutionCteSidecarIndexKind.Hash,
            CteSidecarIndexKind.KeySet => ExecutionCteSidecarIndexKind.KeySet,
            _ => throw UnsupportedShape.Of($"CTE sidecar index kind {kind}")
        };
    }
}
