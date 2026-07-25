using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class CteSidecarIndexLoweringFactory
{
    private readonly Dictionary<string, int> _appendRowOrdinalsByTable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _indexAddOrdinalsByTable = new(StringComparer.Ordinal);

    public IReadOnlyList<ExecutionNode> CreateIndexBuildNodes(
        ExecutionCteSidecarIndexBuildCandidate candidate)
    {
        return candidate.Indexes
            .Select(CreateCteSidecarCreateNode)
            .ToArray();
    }

    public IReadOnlyList<ExecutionNode> CreateAppendRewriteNodes(
        ExecutionCteSidecarAppendRewriteCandidate candidate)
    {
        var appendRow = candidate.AppendRow;
        var rowOrdinal = NextAppendRowOrdinal(appendRow.Table.Name);
        var row = new ExecutionVariable(
            CreateIdentifierCandidate($"{appendRow.Table.Name}SidecarRow{rowOrdinal}", 0),
            typeof(Row),
            appendRow.RowShape.TypeName);
        var nodes = new List<ExecutionNode>(2 + candidate.Indexes.Count)
        {
            new ExecutionCreateGeneratedRow(
                row,
                appendRow.RowShape,
                appendRow.Values,
                appendRow.Contexts,
                appendRow.ContextLayout),
            new ExecutionAppendExistingRow(appendRow.Table, row, appendRow.AppendMode)
        };

        foreach (var index in candidate.Indexes)
            AddCteSidecarIndexAdd(nodes, appendRow, row, index);

        return nodes;
    }

    private static ExecutionNode CreateCteSidecarCreateNode(
        ExecutionCteSidecarIndexCreateSpec spec)
    {
        return spec.Kind switch
        {
            ExecutionCteSidecarIndexKind.Hash => new ExecutionCreateHash(
                spec.Index,
                spec.KeyType,
                spec.RowType ?? ExecutionClrBindingFactory.FromClr(typeof(Row)),
                spec.CapacityHint,
                spec.GeneratedRowTypeName),
            ExecutionCteSidecarIndexKind.KeySet => new ExecutionCreateKeySet(
                spec.Index,
                spec.KeyType,
                spec.CapacityHint),
            _ => throw CreateUnsupportedCteSidecarIndexKind(spec.Kind)
        };
    }

    private void AddCteSidecarIndexAdd(
        List<ExecutionNode> nodes,
        ExecutionAppendRow appendRow,
        ExecutionVariable row,
        ExecutionCteSidecarAppendIndexSpec spec)
    {
        var ordinal = NextIndexAddOrdinal(appendRow.Table.Name);
        switch (spec.Kind)
        {
            case ExecutionCteSidecarIndexKind.Hash:
                var hashRow = row;
                var hashRowTypeName = appendRow.RowShape.TypeName;
                if (spec.PayloadShape != null)
                {
                    hashRow = new ExecutionVariable(
                        CreateIdentifierCandidate($"{appendRow.Table.Name}SidecarPayload{ordinal}", 0),
                        typeof(Row),
                        spec.PayloadShape.TypeName);
                    nodes.Add(new ExecutionCreateHashPayload(
                        hashRow,
                        spec.PayloadShape,
                        spec.PayloadValues));
                    hashRowTypeName = spec.PayloadShape.TypeName;
                }

                nodes.Add(new ExecutionHashAdd(
                    spec.Index,
                    spec.Key,
                    hashRow,
                    spec.KeyType,
                    ExecutionClrBindingFactory.FromClr(typeof(Row)),
                    hashRowTypeName,
                    KeyVariableName: CreateIdentifierCandidate($"{spec.Index.Name}Key{ordinal}", 0),
                    BucketVariableName: CreateIdentifierCandidate($"{spec.Index.Name}Bucket{ordinal}", 0),
                    NullHandling: ExecutionKeyBuildNullHandling.ConditionalSkip));
                break;
            case ExecutionCteSidecarIndexKind.KeySet:
                nodes.Add(new ExecutionKeySetAdd(
                    spec.Index,
                    spec.Key,
                    spec.KeyType,
                    KeyVariableName: CreateIdentifierCandidate($"{spec.Index.Name}Key{ordinal}", 0),
                    NullHandling: ExecutionKeyBuildNullHandling.ConditionalSkip));
                break;
            default:
                throw CreateUnsupportedCteSidecarIndexKind(spec.Kind);
        }
    }

    private string NextAppendRowOrdinal(string tableName)
    {
        return NextOrdinal(_appendRowOrdinalsByTable, tableName).ToString(CultureInfo.InvariantCulture);
    }

    private string NextIndexAddOrdinal(string tableName)
    {
        return NextOrdinal(_indexAddOrdinalsByTable, tableName).ToString(CultureInfo.InvariantCulture);
    }

    private static int NextOrdinal(
        Dictionary<string, int> ordinalsByTable,
        string tableName)
    {
        ordinalsByTable.TryGetValue(tableName, out var ordinal);
        ordinalsByTable[tableName] = ordinal + 1;
        return ordinal;
    }

    private static string CreateIdentifierCandidate(string value, int disambiguator)
    {
        return ExecutionSymbolicNamePolicy.CreateLoweringIdentifierCandidate(value, disambiguator);
    }

    private static Exception CreateUnsupportedCteSidecarIndexKind(ExecutionCteSidecarIndexKind kind)
    {
        return UnsupportedShape.Of($"CTE sidecar index kind {kind}");
    }
}
