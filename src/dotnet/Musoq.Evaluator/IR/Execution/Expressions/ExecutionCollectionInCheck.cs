using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCollectionInCheck(
    ExecutionExpression Expression,
    ExecutionScriptParameterRead Collection,
    Type ElementType,
    Type ReturnType) : ExecutionExpression(ReturnType);
