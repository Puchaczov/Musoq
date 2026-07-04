using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private delegate ExecutionPlanBuildResult BuildCtePlanDelegate(
        PhysicalCteNode cte,
        string identifier,
        PhysicalToExecutionLoweringSession session);

    private sealed class CteLoweringCoordinator(BuildCtePlanDelegate buildCte)
    {
        public bool TryBuild(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            if (context.Plan is PhysicalCteNode cte)
            {
                result = buildCte(cte, context.Identifier, context.Session);
                return true;
            }

            result = null!;
            return false;
        }
    }
}
