using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionIndexedHashRowCreate(
    ExecutionVariable Row,
    ExecutionVariable Index,
    Type ReturnType,
    string? GeneratedRowTypeName = null) : ExecutionExpression(ReturnType);
