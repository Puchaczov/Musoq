using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ScalarSubqueryEmptyResultSpec(
    ProjectedField Projection,
    IReadOnlyList<AggregateBinding> Bindings
);

internal sealed record ScalarSubqueryEmptyResultLowering(
    ExecutionExpression Value,
    IReadOnlyList<ExecutionNode> PreludeNodes
);
