using System.Collections.Generic;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static bool IsDeterministicExpression(IrExpression expression)
    {
        return IrExpressionDeterminism.IsDeterministic(expression);
    }

    private static void AddDeterminismBlockedReasons(IrExpression expression, List<string> reasons)
    {
        IrExpressionDeterminism.AddBlockedReasons(expression, reasons, "Predicate");
    }
}
