using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

internal sealed class PhysicalLoweringRegistry(
    IReadOnlyList<PhysicalPlanLoweringDescriptor> planDescriptors,
    IReadOnlyList<PhysicalTableLoweringDescriptor> tableDescriptors)
{
    public IReadOnlyList<PhysicalPlanLoweringDescriptor> PlanDescriptors { get; } = planDescriptors;

    public IReadOnlyList<PhysicalTableLoweringDescriptor> TableDescriptors { get; } = tableDescriptors;

    public bool TryBuildPlan(
        PhysicalToExecutionLoweringContext context,
        out ExecutionPlanBuildResult result)
    {
        foreach (var descriptor in PlanDescriptors)
        {
            var current = descriptor.TryBuild(context);
            if (current == null)
                continue;

            result = current;
            return true;
        }

        result = null!;
        return false;
    }

    public bool TryBuildTable(
        PhysicalToExecutionTableLoweringContext context,
        out TableBuildResult result)
    {
        foreach (var descriptor in TableDescriptors)
        {
            var current = descriptor.TryBuild(context);
            if (current == null)
                continue;

            result = current;
            return true;
        }

        result = null!;
        return false;
    }
}

internal sealed record PhysicalPlanLoweringDescriptor(
    string Name,
    Func<PhysicalToExecutionLoweringContext, ExecutionPlanBuildResult?> TryBuild);

internal sealed record PhysicalTableLoweringDescriptor(
    string Name,
    Func<PhysicalToExecutionTableLoweringContext, TableBuildResult?> TryBuild);
