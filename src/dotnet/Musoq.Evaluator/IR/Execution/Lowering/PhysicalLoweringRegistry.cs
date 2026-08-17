using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution.Lowering;

internal sealed class PhysicalLoweringRegistry(
    IReadOnlyList<PhysicalPlanLoweringDescriptor> planDescriptors,
    IReadOnlyList<PhysicalTableLoweringDescriptor> tableDescriptors)
{
    public IReadOnlyList<PhysicalPlanLoweringDescriptor> PlanDescriptors { get; } = planDescriptors;

    public IReadOnlyList<PhysicalTableLoweringDescriptor> TableDescriptors { get; } = tableDescriptors;

    public LoweringAttempt<ExecutionPlan> TryBuildPlan(
        PhysicalToExecutionLoweringContext context)
    {
        foreach (var descriptor in PlanDescriptors)
        {
            var current = descriptor.TryBuild(context);
            if (current.Kind == LoweringAttemptKind.NoMatch)
                continue;

            return current;
        }

        return LoweringAttempt<ExecutionPlan>.NoMatch();
    }

    public LoweringAttempt<LoweredTable> TryBuildTable(
        PhysicalToExecutionTableLoweringContext context)
    {
        foreach (var descriptor in TableDescriptors)
        {
            var current = descriptor.TryBuild(context);
            if (current.Kind == LoweringAttemptKind.NoMatch)
                continue;

            return current;
        }

        return LoweringAttempt<LoweredTable>.NoMatch();
    }
}

internal sealed record PhysicalPlanLoweringDescriptor(
    string Name,
    Func<PhysicalToExecutionLoweringContext, LoweringAttempt<ExecutionPlan>> TryBuild);

internal sealed record PhysicalTableLoweringDescriptor(
    string Name,
    Func<PhysicalToExecutionTableLoweringContext, LoweringAttempt<LoweredTable>> TryBuild);
