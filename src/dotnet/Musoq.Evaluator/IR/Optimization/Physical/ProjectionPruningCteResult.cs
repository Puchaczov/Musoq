using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed record ProjectionPruningCteResult(
    PhysicalCteNode Node,
    int PrunedFields,
    int RewrittenDefinitions)
{
    public static ProjectionPruningCteResult NoChange(PhysicalCteNode cte)
    {
        return new ProjectionPruningCteResult(cte, PrunedFields: 0, RewrittenDefinitions: 0);
    }
}

