using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowRowRead(
    ExecutionVariable IndexedRow,
    ExecutionTypeRef ReturnType) : ExecutionExpression(ReturnType)
{
    internal ExecutionIndexedHashRowRowRead(ExecutionVariable indexedRow, Type returnType)
        : this(indexedRow, ExecutionClrBindingFactory.FromClr(returnType))
    {
    }
}
