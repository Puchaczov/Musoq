using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private bool CanApplyHashBuildRowWidthPruning()
    {
        return !ExecutionStrategies.HasRowWidthPruningPlans ||
               ExecutionStrategies.GetAppliedRowWidthPruning(BoundaryRowShapeKind.HashJoinBuild) != null;
    }
}
