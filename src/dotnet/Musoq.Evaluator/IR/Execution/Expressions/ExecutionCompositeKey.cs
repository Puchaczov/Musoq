using System.Collections.Generic;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionCompositeKey(
    IReadOnlyList<ExecutionExpression> Parts) : ExecutionExpression(ExecutionTypeRef.FromClr(typeof(object)));
