using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScriptVariableRead(
    string Name,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionScriptVariableRead(string name, Type returnType)
        : this(name, ExecutionTypeRef.FromClr(returnType))
    {
    }
}
