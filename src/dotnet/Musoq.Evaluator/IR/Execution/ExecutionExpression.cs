using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public abstract record ExecutionExpression
{
    protected ExecutionExpression(ExecutionTypeRef returnType)
    {
        ReturnType = returnType;
    }

    public virtual ExecutionTypeRef ReturnType { get; init; }
}
