using System.Collections.Generic;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record ScalarSubqueryEmptyResultSpec(
    string CteName,
    string ValueColumnName,
    AggregateKernelDescriptor Kernel);

internal sealed record ScalarSubqueryEmptyResultLowering(
    ExecutionExpression Value,
    IReadOnlyList<ExecutionNode> PreludeNodes);
