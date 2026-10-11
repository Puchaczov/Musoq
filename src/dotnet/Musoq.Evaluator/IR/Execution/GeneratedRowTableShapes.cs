using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal static class GeneratedRowTableShapes
{
    public static Dictionary<string, GeneratedRowShape> Collect(ExecutionBlock block)
    {
        var result = new Dictionary<string, GeneratedRowShape>(StringComparer.Ordinal);

        foreach (var node in ExecutionIrAnalysis.FlattenNodes(block))
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

}
