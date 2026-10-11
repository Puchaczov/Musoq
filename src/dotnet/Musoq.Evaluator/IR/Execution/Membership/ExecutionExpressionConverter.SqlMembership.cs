using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public static partial class ExecutionExpressionConverter
{
    private static ExecutionInCheck ConvertInCheck(InCheck membership,
        IReadOnlyDictionary<string, RowShape> sourceShapes,
        IReadOnlyDictionary<string, int>? cteTableIndexes,
        IReadOnlyDictionary<Type, ExecutionVariable>? methodTargets)
    {
        var constantSet = TryCreateConstantInSet(membership);
        ExecutionMembershipBinding? binding = null;
        if (constantSet is null && membership.Values.Count > 0)
            binding = ExecutionMembershipBindingResolver.Bind(membership);
        return new ExecutionInCheck(Convert(membership.Expression, sourceShapes, cteTableIndexes, methodTargets),
            membership.Values.Select(value => Convert(value, sourceShapes, cteTableIndexes, methodTargets)).ToArray(),
            membership.ReturnType, constantSet, membership.IsNegated)
        { UsesSqlNullSemantics = membership.SqlSemantics, TypedBinding = binding };
    }
}
