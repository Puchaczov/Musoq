using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static bool TryCreateFusedHashPayload(
        FusedCteHashBuildSource fusion,
        TableRowShape tableRow,
        out HashPayloadShape payloadShape,
        out TableRowShape payloadTableRow,
        out IReadOnlyList<ExecutionRowValue> payloadValues)
    {
        payloadShape = null!;
        payloadTableRow = null!;
        payloadValues = [];

        if (fusion.HashPayloadShape == null ||
            tableRow.Fields.Count != fusion.RowShape.Fields.Count ||
            tableRow.Contexts.Count != fusion.RowShape.Contexts.Count)
        {
            return false;
        }

        payloadShape = fusion.HashPayloadShape;
        var plannedPayloadShape = payloadShape;
        if (!TryCreateFusedHashPayloadValues(fusion, plannedPayloadShape, out payloadValues))
            return false;

        payloadTableRow = tableRow with
        {
            Contexts = plannedPayloadShape.Contexts.Select(context => context with
            {
                AccessStrategy = context.AccessStrategy
            }).ToArray()
        };
        return true;
    }

    private static bool TryCreateFusedHashPayloadValues(
        FusedCteHashBuildSource fusion,
        HashPayloadShape payloadShape,
        out IReadOnlyList<ExecutionRowValue> payloadValues)
    {
        var values = new List<ExecutionRowValue>(payloadShape.Fields.Count + payloadShape.Contexts.Count);
        foreach (var field in payloadShape.Fields)
        {
            var index = FindFusedPayloadFieldIndex(fusion.RowShape, field);
            if (index < 0)
            {
                payloadValues = [];
                return false;
            }

            values.Add(fusion.RowValues[index]);
        }

        values.AddRange(fusion.ContextValues.Select((context, index) => new ExecutionRowValue(
            payloadShape.Contexts[index].Name,
            context)));
        payloadValues = values;
        return true;
    }

    private static int FindFusedPayloadFieldIndex(
        GeneratedRowShape rowShape,
        FieldBinding field)
    {
        for (var index = 0; index < rowShape.Fields.Count; index++)
        {
            var candidate = rowShape.Fields[index];
            if (FieldBindingsMatch(candidate, field))
                return index;
        }

        return -1;
    }

    private static bool FieldBindingsMatch(FieldBinding left, FieldBinding right)
    {
        if (string.Equals(left.Name, right.Name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(left.QualifiedName, right.QualifiedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return left.AccessStrategy is GeneratedFieldAccess leftGenerated &&
               right.AccessStrategy is GeneratedFieldAccess rightGenerated &&
               string.Equals(leftGenerated.FieldName, rightGenerated.FieldName, StringComparison.Ordinal);
    }

    private static HashPayloadShape? CreateFusedHashPayloadShape(
        string alias,
        GeneratedRowShape rowShape)
    {
        return new HashPayloadShape(
            CreateFusedHashPayloadTypeName(alias),
            rowShape.Fields,
            CreateFusedHashPayloadContextBindings(rowShape.Contexts));
    }

    private static FieldBinding[] CreateFusedHashPayloadContextBindings(
        IReadOnlyList<FieldBinding> contexts)
    {
        return contexts
            .Select((context, index) => context with
            {
                AccessStrategy = new GeneratedFieldAccess($"__context{index.ToString(System.Globalization.CultureInfo.InvariantCulture)}")
            })
            .ToArray();
    }

    private static string CreateFusedHashPayloadTypeName(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return "HashPayload0";

        var candidate = $"{char.ToUpperInvariant(alias[0])}{alias[1..]}HashPayload0";
        return CreateIdentifierCandidate(candidate, 0);
    }
}
