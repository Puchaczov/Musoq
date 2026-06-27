using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionAsOfTieBreak(
    ExecutionExpression Key,
    bool Descending,
    NullOrdering NullOrdering);
