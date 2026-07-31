using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static FinalShapePrintContext? CreateFinalShapePrintContext(ExecutionPlan plan)
    {
        if (plan.FinalResult == null)
            return null;

        var shapeTypeName = FinalSelectShapeNaming.CreateTypeName(plan.FinalResult);
        var usesGeneratedRowCarrier = FinalGeneratedRowSinkPolicy.CanUse(
            plan,
            plan.FinalResult.TableName);
        return new FinalShapePrintContext(
            plan.FinalResult.TableName,
            plan.FinalResult.Shape.TypeName,
            usesGeneratedRowCarrier ? plan.FinalResult.Shape.TypeName : shapeTypeName,
            usesGeneratedRowCarrier
                ? new Dictionary<string, string>(StringComparer.Ordinal)
                : CreateFinalShapeSourceBuffers(plan.Body, plan.FinalResult.TableName, shapeTypeName, plan.FinalResult.Shape.Fields));
    }

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
                if (!TryGetPostOperationSourceAndTarget(node, out var source, out var target) ||
                    !requiredTargets.Contains(target.Name) ||
                    !rowShapesByTableName.TryGetValue(source.Name, out var sourceShape) ||
                    !CanUseFinalShapeSourceBuffer(sourceShape, shapeFields))
                {
                    continue;
                }

                buffers ??= new Dictionary<string, string>(StringComparer.Ordinal);
                if (buffers.TryAdd(source.Name, shapeTypeName))
                    changed |= requiredTargets.Add(source.Name);
            }
        }

        return buffers ?? (IReadOnlyDictionary<string, string>)
            new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static bool TryGetPostOperationSourceAndTarget(
        ExecutionNode node,
        out ExecutionVariable source,
        out ExecutionVariable target)
    {
        switch (node)
        {
            case ExecutionDistinctTable distinct:
                source = distinct.Source;
                target = distinct.Target;
                return true;
            case ExecutionSortTable sort:
                source = sort.Source;
                target = sort.Target;
                return true;
            case ExecutionTopNTable topN:
                source = topN.Source;
                target = topN.Target;
                return true;
            case ExecutionTopOffsetTable topOffset:
                source = topOffset.Source;
                target = topOffset.Target;
                return true;
            case ExecutionSkipTable skip:
                source = skip.Source;
                target = skip.Target;
                return true;
            case ExecutionTakeTable take:
                source = take.Source;
                target = take.Target;
                return true;
            case ExecutionSliceTable slice:
                source = slice.Source;
                target = slice.Target;
                return true;
            default:
                source = null!;
                target = null!;
                return false;
        }
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

    private static bool TryGetFinalShapeContext(out FinalShapePrintContext context)
    {
        if (FinalShapeContext.Value != null)
        {
            context = FinalShapeContext.Value;
            return true;
        }

        context = null!;
        return false;
    }

    private static bool IsFinalShapeTarget(string tableName)
    {
        return TryGetFinalShapeContext(out var context) &&
               string.Equals(context.FinalTableName, tableName, StringComparison.Ordinal);
    }

    private static bool IsFinalShapeSourceBuffer(string tableName, out string shapeTypeName)
    {
        if (TryGetFinalShapeContext(out var context) &&
            context.SourceBuffers.TryGetValue(tableName, out shapeTypeName!))
        {
            return true;
        }

        shapeTypeName = string.Empty;
        return false;
    }

    private static bool IsFinalShapeAppendTarget(string tableName, out string shapeTypeName)
    {
        if (IsFinalShapeSourceBuffer(tableName, out shapeTypeName))
            return true;

        if (TryGetFinalShapeContext(out var context) &&
            string.Equals(context.FinalTableName, tableName, StringComparison.Ordinal))
        {
            shapeTypeName = context.ShapeTypeName;
            return true;
        }

        shapeTypeName = string.Empty;
        return false;
    }

    private static bool IsFinalShapePostOperation(ExecutionVariable target, ExecutionVariable source)
    {
        return IsFinalShapeTarget(target.Name) ||
               IsFinalShapeSourceBuffer(target.Name, out _) ||
               IsFinalShapeSourceBuffer(source.Name, out _);
    }
}
