using System.Collections.Generic;
using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionPlanPrinter
{
    private static IReadOnlyDictionary<string, string> CreateTypedRowBuffers(ExecutionPlan plan)
    {
        return ExecutionTypedRowBufferResolver
            .Resolve(plan.Body, plan.FinalResult?.TableName)
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.TypeName,
                StringComparer.Ordinal);
    }

    private static bool TryGetTypedRowBuffer(string tableName, out string rowTypeName)
    {
        if (TypedRowBuffers.Value != null &&
            TypedRowBuffers.Value.TryGetValue(tableName, out rowTypeName!))
        {
            return true;
        }

        rowTypeName = string.Empty;
        return false;
    }

    private static bool IsTypedRowBufferPostOperation(ExecutionVariable target, ExecutionVariable source)
    {
        return TryGetTypedRowBuffer(target.Name, out _) ||
               TryGetTypedRowBuffer(source.Name, out _);
    }
}
