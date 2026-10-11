using System.Collections.Generic;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.CodeGeneration;

internal static partial class FinalOutputPlanner
{
    private static IReadOnlyDictionary<string, string> CreateFinalShapeSourceBuffers(
        ExecutionBlock block,
        string finalTableName,
        string shapeTypeName,
        IReadOnlyList<FieldBinding> shapeFields)
    {
        var rowShapesByTableName = CreateTableRowShapeMap(block);
        Dictionary<string, string>? buffers = null;

        var requiredTargets = new HashSet<string>(StringComparer.Ordinal)
        {
            finalTableName
        };

        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var node in block.Nodes)
            {
                if (GetPostOperationTransfer(node) is not { } transfer ||
                    !requiredTargets.Contains(transfer.Target.Name) ||
                    !rowShapesByTableName.TryGetValue(transfer.Source.Name, out var sourceShape) ||
                    !CanUseFinalShapeSourceBuffer(sourceShape, shapeFields))
                {
                    continue;
                }

                buffers ??= new Dictionary<string, string>(StringComparer.Ordinal);
                if (buffers.TryAdd(transfer.Source.Name, shapeTypeName))
                    changed |= requiredTargets.Add(transfer.Source.Name);
            }
        }

        return buffers ?? (IReadOnlyDictionary<string, string>)
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static (ExecutionVariable Source, ExecutionVariable Target)? GetPostOperationTransfer(ExecutionNode node)
    {
        return node switch
        {
            ExecutionDistinctTable distinct => (distinct.Source, distinct.Target),
            ExecutionSortTable sort => (sort.Source, sort.Target),
            ExecutionTopNTable topN => (topN.Source, topN.Target),
            ExecutionTopOffsetTable topOffset => (topOffset.Source, topOffset.Target),
            ExecutionSkipTable skip => (skip.Source, skip.Target),
            ExecutionTakeTable take => (take.Source, take.Target),
            ExecutionSliceTable slice => (slice.Source, slice.Target),
            _ => null
        };
    }

    private static Dictionary<string, GeneratedRowShape> CreateTableRowShapeMap(ExecutionBlock block)
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
                case var postOperation when GetPostOperationTransfer(postOperation) is { } transfer &&
                                            result.TryGetValue(transfer.Source.Name, out var sourceShape):
                    result[transfer.Target.Name] = sourceShape;
                    break;
                case ExecutionSetOperation setOperation when result.TryGetValue(setOperation.Left.Name, out var setOperationShape):
                    result[setOperation.Target.Name] = setOperationShape;
                    break;
            }
        }

        return result;
    }

    private static bool CanUseFinalShapeSourceBuffer(
        GeneratedRowShape sourceShape,
        IReadOnlyList<FieldBinding> shapeFields)
    {
        if (sourceShape.Fields.Count != shapeFields.Count)
            return false;

        for (var index = 0; index < shapeFields.Count; index++)
        {
            var sourceField = sourceShape.Fields[index];
            var shapeField = shapeFields[index];
            if (sourceField.Type != shapeField.Type ||
                !string.Equals(ExecutionSymbolicNamePolicy.GetGeneratedFieldName(sourceField), ExecutionSymbolicNamePolicy.GetGeneratedFieldName(shapeField), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

}
