using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static TableBuildResult AddCteClauseBoundaries(
        PhysicalCteDefinition definition,
        int index,
        TableBuildResult result,
        bool includeScopeBoundaries = false)
    {
        if (!result.IsBuilt)
            return result;

        var suffix = ExecutionPhaseBoundaryPlanner.CreateCteSuffix(index);
        var nodes = ExecutionPhaseBoundaryPlanner.AddScopeClauseBoundaries(
            definition.Plan,
            result.Nodes,
            suffix);
        return result with
        {
            Nodes = includeScopeBoundaries
                ? [
                    new ExecutionPhaseBoundary(QueryPhase.Begin, suffix),
                    ..nodes,
                    new ExecutionPhaseBoundary(QueryPhase.End, suffix)
                ]
                : nodes
        };
    }
}
