using Musoq.Evaluator.IR.Execution.Lowering;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Coordinators;

internal sealed class CteLoweringService(
    ICteLoweringOperations operations) : ICteLoweringService
{
    public LoweringAttempt<ExecutionPlan> BuildCte(
        PhysicalCteNode cte, string identifier, LoweringScope scope) =>
        operations.BuildCte(cte, identifier, scope);
}
