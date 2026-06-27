using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionWindowValueRead(
    ExecutionVariable Results,
    ExecutionVariable Index,
    Type ReturnType) : ExecutionExpression(ReturnType);
