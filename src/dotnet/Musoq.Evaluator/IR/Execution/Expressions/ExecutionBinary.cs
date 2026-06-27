using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionBinary(
    BinaryOpKind Kind,
    ExecutionExpression Left,
    ExecutionExpression Right,
    Type ReturnType) : ExecutionExpression(ReturnType);
