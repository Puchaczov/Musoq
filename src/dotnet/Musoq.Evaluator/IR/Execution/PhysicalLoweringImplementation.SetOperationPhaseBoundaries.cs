using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IReadOnlyList<ExecutionNode> CreateSetOperationArmNodes(
        PhysicalNode physicalPlan,
        IReadOnlyList<ExecutionNode> nodes,
        string queryIdSuffix)
    {
        var body = ExecutionPhaseBoundaryPlanner.AddScopeClauseBoundaries(
            physicalPlan,
            nodes,
            queryIdSuffix);
        return
        [
            new ExecutionPhaseBoundary(QueryPhase.Begin, queryIdSuffix),
            ..body,
            new ExecutionPhaseBoundary(QueryPhase.End, queryIdSuffix)
        ];
    }
}
