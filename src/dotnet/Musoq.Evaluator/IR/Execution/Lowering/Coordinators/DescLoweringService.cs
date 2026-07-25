using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal interface IDescLoweringOperations : IDescLoweringService
{
}

internal sealed class DescLoweringService(IDescLoweringOperations operations) : IDescLoweringService
{
    public LoweringAttempt<ExecutionPlan> BuildDesc(PhysicalDescNode desc, string identifier) =>
        operations.BuildDesc(desc, identifier);
}
