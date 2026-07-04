using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private bool TryGetGeneratedRowShape(ExecutionVariable table, out GeneratedRowShape rowShape)
    {
        return RenderSession.TableRowShapesByVariableName.TryGetValue(table.Name, out rowShape!);
    }

    private static Dictionary<string, GeneratedRowShape> CreateTableRowShapeMap(ExecutionBlock block)
    {
        var result = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);

        foreach (var node in FlattenNodes(block))
        {
            switch (node)
            {
                case ExecutionCreateTable createTable:
                    result[createTable.Table.Name] = createTable.RowShape;
                    break;
                case ExecutionProjectTable projectTable:
                    result[projectTable.Target.Name] = projectTable.RowShape;
                    break;
                case ExecutionMaterializeRecordListToTable materialize:
                    result[materialize.Target.Name] = materialize.RowShape;
                    break;
                case ExecutionDistinctTable distinct when result.TryGetValue(distinct.Source.Name, out var distinctShape):
                    result[distinct.Target.Name] = distinctShape;
                    break;
                case ExecutionSortTable sort when result.TryGetValue(sort.Source.Name, out var sortShape):
                    result[sort.Target.Name] = sortShape;
                    break;
                case ExecutionTopNTable topN when result.TryGetValue(topN.Source.Name, out var topNShape):
                    result[topN.Target.Name] = topNShape;
                    break;
                case ExecutionTopOffsetTable topOffset when result.TryGetValue(topOffset.Source.Name, out var topOffsetShape):
                    result[topOffset.Target.Name] = topOffsetShape;
                    break;
                case ExecutionSkipTable skip when result.TryGetValue(skip.Source.Name, out var skipShape):
                    result[skip.Target.Name] = skipShape;
                    break;
                case ExecutionTakeTable take when result.TryGetValue(take.Source.Name, out var takeShape):
                    result[take.Target.Name] = takeShape;
                    break;
                case ExecutionSliceTable slice when result.TryGetValue(slice.Source.Name, out var sliceShape):
                    result[slice.Target.Name] = sliceShape;
                    break;
                case ExecutionSetOperation setOperation when result.TryGetValue(setOperation.Left.Name, out var setOperationShape):
                    result[setOperation.Target.Name] = setOperationShape;
                    break;
            }
        }

        return result;
    }

    private static IEnumerable<GeneratedRowOrderComparerInput> CollectGeneratedRowOrderComparerInputs(
        ExecutionBlock block,
        IReadOnlyDictionary<string, GeneratedRowShape> rowShapesByTableName)
    {
        foreach (var node in FlattenNodes(block))
        {
            var input = node switch
            {
                ExecutionSortTable sort => TryCreateGeneratedRowOrderComparerInput(
                    sort.Source,
                    sort.Target,
                    sort.Keys,
                    rowShapesByTableName,
                    out var sortInput)
                    ? sortInput
                    : null,
                ExecutionTopNTable topN => TryCreateGeneratedRowOrderComparerInput(
                    topN.Source,
                    topN.Target,
                    topN.Keys,
                    rowShapesByTableName,
                    out var topNInput)
                    ? topNInput
                    : null,
                ExecutionTopOffsetTable topOffset => TryCreateGeneratedRowOrderComparerInput(
                    topOffset.Source,
                    topOffset.Target,
                    topOffset.Keys,
                    rowShapesByTableName,
                    out var topOffsetInput)
                    ? topOffsetInput
                    : (GeneratedRowOrderComparerInput?)null,
                _ => null
            };

            if (input.HasValue)
                yield return input.Value;
        }
    }

    private static bool TryCreateGeneratedRowOrderComparerInput(
        ExecutionVariable source,
        ExecutionVariable target,
        IReadOnlyList<ExecutionOrderField> keys,
        IReadOnlyDictionary<string, GeneratedRowShape> rowShapesByTableName,
        out GeneratedRowOrderComparerInput input)
    {
        if (!rowShapesByTableName.TryGetValue(source.Name, out var rowShape) ||
            !CanUseGeneratedRowOrderComparer(keys, rowShape))
        {
            input = default;
            return false;
        }

        input = new GeneratedRowOrderComparerInput(source.Name, target.Name, rowShape, keys);
        return true;
    }
}
