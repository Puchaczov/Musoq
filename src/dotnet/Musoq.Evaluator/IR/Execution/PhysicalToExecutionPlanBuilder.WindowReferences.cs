using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static BuildResult<ExecutionExpression> ConvertWindowFunctionRef(
        WindowFunctionRef windowRef,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        if (!windowResults.TryGetValue(windowRef.WindowIndex, out var results))
        {
            return BuildResult<ExecutionExpression>.Unsupported(
                $"Execution IR window lowering cannot bind window reference {windowRef.WindowIndex.ToString(CultureInfo.InvariantCulture)} to a supported registration.");
        }

        return BuildResult<ExecutionExpression>.Success(new ExecutionWindowValueRead(results, windowIndex, windowRef.ReturnType));
    }
}
