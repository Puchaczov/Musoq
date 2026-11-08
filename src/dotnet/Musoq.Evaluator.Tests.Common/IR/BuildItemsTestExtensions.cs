using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Build;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Physical;

namespace Musoq.Evaluator.Tests.IR;

internal static class BuildItemsTestExtensions
{
    public static PhysicalNode RequirePhysicalPlan(this BuildItems buildItems)
    {
        var plan = buildItems.PhysicalPlan;
        Assert.IsNotNull(plan);
        return plan;
    }

    public static LogicalNode RequireLogicalPlan(this BuildItems buildItems)
    {
        var plan = buildItems.LogicalPlan;
        Assert.IsNotNull(plan);
        return plan;
    }

    public static string RequirePlanningText(this BuildItems buildItems)
    {
        var text = buildItems.PlanningText;
        Assert.IsNotNull(text);
        return text;
    }

    public static string RequireExecutionPlanText(this BuildItems buildItems)
    {
        var text = buildItems.ExecutionPlanText;
        Assert.IsNotNull(text);
        return text;
    }
}
