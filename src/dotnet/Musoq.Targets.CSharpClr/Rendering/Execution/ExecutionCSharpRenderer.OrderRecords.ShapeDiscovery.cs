using System.Collections.Generic;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool TryGetGeneratedRowShape(
        ExecutionVariable table,
        ExecutionRenderContext context,
        out GeneratedRowShape rowShape)
    {
        return context.Session.TableRowShapesByVariableName.TryGetValue(table.Name, out rowShape!);
    }

    private static Dictionary<string, GeneratedRowShape> CreateTableRowShapeMap(ExecutionBlock block) =>
        GeneratedRowTableShapes.Collect(block);

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
