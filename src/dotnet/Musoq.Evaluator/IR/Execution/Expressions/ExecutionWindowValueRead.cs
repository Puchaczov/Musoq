using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowValueRead(
    ExecutionVariable Results,
    ExecutionVariable Index,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionWindowValueRead(ExecutionVariable results, ExecutionVariable index, Type returnType)
        : this(results, index, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
