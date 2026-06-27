using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool CanUseCteSidecarHashPayloads(
        ExecutionBlock block,
        string tableName,
        GeneratedRowShape rowShape)
    {
        if (!rowShape.SupportsGeneratedFieldAccess)
            return false;

        var appendRows = ExecutionIrAnalysis
            .CollectNodes<ExecutionAppendRow>(block)
            .Where(append => string.Equals(append.Table.Name, tableName, StringComparison.Ordinal) &&
                             string.Equals(append.RowShape.TypeName, rowShape.TypeName, StringComparison.Ordinal))
            .ToArray();

        return appendRows.Length > 0 &&
               appendRows.All(append => append.Contexts.Count == rowShape.Contexts.Count);
    }

    private static HashPayloadShape CreateCteSidecarHashPayloadShape(
        GeneratedRowShape rowShape,
        CteSidecarIndexSpec spec)
    {
        return new HashPayloadShape(
            CreateCteSidecarHashPayloadTypeName(rowShape, spec),
            rowShape.Fields,
            CreateFusedHashPayloadContextBindings(rowShape.Contexts));
    }

    private CteSidecarIndexBuild CreateCteSidecarIndexBuild(
        string tableName,
        GeneratedRowShape rowShape,
        CteSidecarIndexSpec spec,
        bool canUseHashPayloads)
    {
        var payloadShape = spec.Kind == CteSidecarIndexKind.Hash && canUseHashPayloads
            ? CreateCteSidecarHashPayloadShape(rowShape, spec)
            : null;
        if (payloadShape != null)
            _cteSidecarHashPayloadsBySlot[spec.IndexSlot] = payloadShape;

        return new CteSidecarIndexBuild(
            spec,
            new ExecutionVariable(CreateCteSidecarIndexVariableName(tableName, spec), typeof(object)),
            payloadShape);
    }

    private static IReadOnlyList<ExecutionRowValue> CreateCteSidecarHashPayloadValues(
        HashPayloadShape payloadShape,
        ExecutionAppendRow appendRow)
    {
        var valuesByName = appendRow.Values.ToDictionary(
            static value => value.FieldName,
            static value => value.Value,
            StringComparer.OrdinalIgnoreCase);
        var values = new List<ExecutionRowValue>(payloadShape.Fields.Count + payloadShape.Contexts.Count);

        foreach (var field in payloadShape.Fields)
        {
            if (!valuesByName.TryGetValue(field.Name, out var value) &&
                !valuesByName.TryGetValue(field.QualifiedName, out value))
            {
                throw new InvalidOperationException($"CTE sidecar hash payload field '{field.Name}' was not present in the append row values.");
            }

            values.Add(new ExecutionRowValue(field.Name, value));
        }

        if (appendRow.Contexts.Count != payloadShape.Contexts.Count)
            throw new InvalidOperationException("CTE sidecar hash payload context count does not match the append row context count.");

        values.AddRange(appendRow.Contexts.Select((context, index) => new ExecutionRowValue(
            payloadShape.Contexts[index].Name,
            context)));

        return values;
    }

    private bool TryUseCteSidecarHashPayloadJoinSource(
        JoinSource source,
        CteSidecarIndexSpec sidecar,
        out JoinSource payloadSource)
    {
        payloadSource = source;
        if (sidecar.Kind != CteSidecarIndexKind.Hash ||
            source.Shape is not TableRowShape tableRow ||
            !_cteSidecarHashPayloadsBySlot.TryGetValue(sidecar.IndexSlot, out var payloadShape))
        {
            return false;
        }

        var payloadTableRow = CreateCteSidecarHashPayloadTableRowShape(tableRow, payloadShape);
        payloadSource = source with
        {
            Shape = payloadTableRow,
            Variable = source.Variable with
            {
                Type = typeof(Row),
                GeneratedRowTypeName = payloadShape.TypeName
            },
            Shapes =
            [
                ..source.Shapes.Where(shape => !Equals(shape, source.Shape)),
                payloadShape,
                payloadTableRow
            ]
        };
        return true;
    }

    private static TableRowShape CreateCteSidecarHashPayloadTableRowShape(
        TableRowShape tableRow,
        HashPayloadShape payloadShape)
    {
        var fields = payloadShape.Fields.Select(field => field with
        {
            QualifiedName = $"{tableRow.Alias}.{field.Name}",
            AccessStrategy = new GeneratedFieldAccess(GeneratedRowNamingPolicy.GetGeneratedFieldName(field))
        }).ToArray();
        var contexts = payloadShape.Contexts.Select(context => context with
        {
            QualifiedName = $"{tableRow.Alias}.{context.Name}"
        }).ToArray();

        return tableRow with
        {
            Fields = fields,
            Contexts = contexts
        };
    }

    private static string CreateCteSidecarHashPayloadTypeName(
        GeneratedRowShape rowShape,
        CteSidecarIndexSpec spec)
    {
        var prefix = rowShape.TypeName.EndsWith("Row0", StringComparison.Ordinal)
            ? rowShape.TypeName[..^4]
            : rowShape.TypeName;

        return CreateIdentifierCandidate(
            $"{prefix}HashPayload{spec.IndexSlot.ToString(CultureInfo.InvariantCulture)}",
            0);
    }
}
