using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering;
internal interface ICteLoweringService
{
    LoweringAttempt<ExecutionPlan> BuildCte(
        PhysicalCteNode cte,
        string identifier,
        LoweringScope scope);
}
internal interface ICteLoweringOperations : ICteLoweringService
{
}
internal sealed class CtePlanLowerer(
    ICteLoweringService service)
{
    public LoweringAttempt<ExecutionPlan> TryBuild(PhysicalToExecutionLoweringContext context)
    {
        return context.Plan is PhysicalCteNode cte
            ? service.BuildCte(cte, context.Identifier, context.Scope)
            : LoweringAttempt<ExecutionPlan>.NoMatch();
    }
}
