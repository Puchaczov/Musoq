using Musoq.Evaluator.IR.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static FinalShapePrintContext? CreateFinalShapePrintContext(ExecutionPlan plan)
    {
        if (!TableViaRowsResultInfoResolver.TryResolveRenderPlan(plan, out var renderPlan))
            return null;

        var output = renderPlan.FinalSinkPlans.TableOutput;
        return new FinalShapePrintContext(
            output.Result.TableName,
            output.Result.RowTypeName,
            output.RowTypeName,
            output.SourceBuffers,
            output.OwnedBuffer);
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
