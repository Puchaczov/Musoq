using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionScriptParameterRead(
    string Name,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionScriptParameterRead(string name, Type returnType)
        : this(name, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
