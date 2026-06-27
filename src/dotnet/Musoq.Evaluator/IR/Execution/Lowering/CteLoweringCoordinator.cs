using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed class CteLoweringCoordinator(PhysicalToExecutionPlanBuilder builder)
    {
        public bool TryBuild(PhysicalToExecutionLoweringContext context, out ExecutionPlanBuildResult result)
        {
            if (context.Plan is PhysicalCteNode cte)
            {
                result = builder.BuildCte(cte, context.Identifier);
                return true;
            }

            result = null!;
            return false;
        }
    }
}
