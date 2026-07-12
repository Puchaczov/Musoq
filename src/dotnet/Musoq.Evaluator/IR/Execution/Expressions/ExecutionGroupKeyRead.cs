using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGroupKeyRead(
    ExecutionVariable Group,
    string KeyName,
    ExecutionTypeRef ReturnType,
    AggregateGroupKeyField? Key = null) : ExecutionExpression(ReturnType)
{
    internal ExecutionGroupKeyRead(
        ExecutionVariable group,
        string keyName,
        Type returnType,
        AggregateGroupKeyField? key = null)
        : this(group, keyName, ExecutionTypeRef.FromClr(returnType), key)
    {
    }
}
