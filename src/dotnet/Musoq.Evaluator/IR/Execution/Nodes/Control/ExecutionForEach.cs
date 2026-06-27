using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed record ExecutionForEach(
    ExecutionVariable Item,
    ExecutionExpression Source,
    ExecutionBlock Body) : ExecutionSourceLoop(Item, Source, Body);
