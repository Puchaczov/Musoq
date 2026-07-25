using Musoq.Evaluator.IR.Physical.Nodes;
namespace Musoq.Evaluator.IR.Execution.Lowering;

internal interface IDescLoweringService
{
    LoweringAttempt<ExecutionPlan> BuildDesc(PhysicalDescNode desc, string identifier);
}

internal sealed class DescPlanLowerer(
    IDescLoweringService service)
{
    public LoweringAttempt<ExecutionPlan> TryBuild(PhysicalToExecutionLoweringContext context)
    {
        return context.Plan is PhysicalDescNode desc
            ? service.BuildDesc(desc, context.Identifier)
            : LoweringAttempt<ExecutionPlan>.NoMatch();
    }
}
