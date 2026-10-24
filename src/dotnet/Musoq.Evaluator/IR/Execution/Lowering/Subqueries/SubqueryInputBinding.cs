using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class SubqueryInputBinding
{
    internal static LoweringAttempt<ExecutionExpression> Convert(SubqueryExpression query, Func<IrExpression, LoweringAttempt<ExecutionExpression>> convert)
    {
        var bindings = new List<ExecutionExpression>();
        foreach (var binding in query.Bindings)
        {
            var result = convert(binding);
            if (!result.IsBuilt)
                return result;
            bindings.Add(result.Value);
        }
        ExecutionExpression? operand = null;
        if (query.Operand is not null)
        {
            var result = convert(query.Operand);
            if (!result.IsBuilt)
                return result;
            operand = result.Value;
        }
        return LoweringAttempt<ExecutionExpression>.Built(SubqueryExecutionLowering.Create(query, bindings, operand));
    }
}
