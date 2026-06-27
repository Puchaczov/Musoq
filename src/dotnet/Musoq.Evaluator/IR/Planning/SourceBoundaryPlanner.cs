using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceBoundaryPlanner
{
    public static SourceBoundaryPlanningResult Plan(LogicalNode logicalPlan)
    {
        ArgumentNullException.ThrowIfNull(logicalPlan);
        var plans = new List<SourceBoundaryPlan>();
        AddBoundaryPlans(logicalPlan, plans, null);
        var strategyPlanningResult = SourceBoundaryStrategyPlanner.Plan(plans);
        var decisions = plans
            .Select(CreateDecision)
            .Concat(strategyPlanningResult.Decisions)
            .ToArray();

        return new SourceBoundaryPlanningResult(plans, strategyPlanningResult.Plans, decisions);
    }

    private static void AddBoundaryPlans(
        LogicalNode node,
        List<SourceBoundaryPlan> plans,
        ApplyKind? containingApplyKind)
    {
        switch (node)
        {
            case ApplyNode apply:
                plans.Add(CreateApplyBoundaryPlan(apply, plans.Count));
                AddBoundaryPlans(apply.Left, plans, null);
                AddBoundaryPlans(apply.Right, plans, apply.Kind);
                return;
            case InterpretSourceNode interpret:
                plans.Add(CreateInterpretBoundaryPlan(interpret, containingApplyKind ?? interpret.ApplyKind));
                break;
            case PropertySourceNode propertySource:
                plans.Add(CreatePropertyBoundaryPlan(propertySource, containingApplyKind ?? propertySource.ApplyKind));
                break;
            case AccessMethodSourceNode accessMethod:
                plans.Add(CreateAccessMethodBoundaryPlan(accessMethod, containingApplyKind ?? accessMethod.ApplyKind));
                break;
        }

        foreach (var child in node.Children)
            AddBoundaryPlans(child, plans, containingApplyKind);
    }


}
