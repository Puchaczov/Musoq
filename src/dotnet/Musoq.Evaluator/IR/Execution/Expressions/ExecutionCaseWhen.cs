using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCaseWhen(
    IReadOnlyList<ExecutionCaseWhenBranch> Branches,
    ExecutionExpression? ElseExpression,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionCaseWhen(
        IReadOnlyList<ExecutionCaseWhenBranch> branches,
        ExecutionExpression? elseExpression,
        Type returnType)
        : this(branches, elseExpression, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
