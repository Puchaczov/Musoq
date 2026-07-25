using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static LoweringAttempt<ExecutionExpression> ConvertWindowFunctionRef(
        WindowFunctionRef windowRef,
        IReadOnlyDictionary<int, ExecutionVariable> windowResults,
        ExecutionVariable windowIndex)
    {
        if (!windowResults.TryGetValue(windowRef.WindowIndex, out var results))
        {
            return LoweringAttempt<ExecutionExpression>.Unsupported(
                $"Execution IR window lowering cannot bind window reference {windowRef.WindowIndex.ToString(CultureInfo.InvariantCulture)} to a supported registration.");
        }

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionWindowValueRead(results, windowIndex, windowRef.ReturnType));
    }
}
