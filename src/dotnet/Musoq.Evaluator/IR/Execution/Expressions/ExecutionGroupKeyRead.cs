using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionGroupKeyRead(
    ExecutionVariable Group,
    string KeyName,
    Type ReturnType,
    AggregateGroupKeyField? Key = null) : ExecutionExpression(ReturnType);
