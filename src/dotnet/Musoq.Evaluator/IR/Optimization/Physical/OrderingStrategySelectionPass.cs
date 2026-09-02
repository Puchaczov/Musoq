using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Physical.Rewriting;
using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Optimization.Physical;

internal sealed class OrderingStrategySelectionPass : IPhysicalOptimizationPass
{
    public string Name => "OrderingStrategySelection";

    public OptimizationResult<PhysicalNode> Optimize(PhysicalNode plan, OptimizationContext context)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var state = PhysicalOptimizationState.From(context);
        var rewritten = Rewrite(plan, state);

        return ReferenceEquals(plan, rewritten)
            ? OptimizationResult<PhysicalNode>.NoChange(plan, "No bounded ordering candidates were rewritten.")
            : OptimizationResult<PhysicalNode>.Changed(rewritten, "Selected bounded ordering strategies.");
    }

    private static PhysicalNode Rewrite(PhysicalNode node, PhysicalOptimizationState state)
    {
        if (node is PhysicalTakeNode take)
            return RewriteTake(take, state);

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => Rewrite(child, state));
    }

    private static PhysicalNode RewriteTake(PhysicalTakeNode take, PhysicalOptimizationState state)
    {
        var input = Rewrite(take.Input, state);

        if (input is PhysicalSkipNode { Input: PhysicalSortNode sort } skip)
        {
            if (ContainsNonWindowRowNumberProjection(sort.Input))
            {
                AddDecision(
                    state,
                    TakeStrategyKind.Take,
                    "A non-window RowNumber projection requires complete ordering before SKIP/TAKE.");
                return ReferenceEquals(input, take.Input)
                    ? take
                    : new PhysicalTakeNode(take.Count, input);
            }

            AddDecision(state, TakeStrategyKind.TopOffset, "Sort -> Skip -> Take can use bounded top-offset ordering.");
            return new PhysicalTopOffsetNode(skip.Count, take.Count, sort.Keys, sort.Input);
        }

        if (input is PhysicalSortNode sortOnly)
        {
            if (ContainsNonWindowRowNumberProjection(sortOnly.Input))
            {
                AddDecision(
                    state,
                    TakeStrategyKind.Take,
                    "A non-window RowNumber projection requires complete ordering before TAKE.");
                return ReferenceEquals(input, take.Input)
                    ? take
                    : new PhysicalTakeNode(take.Count, input);
            }

            AddDecision(state, TakeStrategyKind.TopN, "Sort -> Take can use bounded top-N ordering.");
            return new PhysicalTopNNode(take.Count, sortOnly.Keys, sortOnly.Input);
        }

        AddDecision(state, TakeStrategyKind.Take, "Take has no adjacent sort boundary to collapse.");
        return ReferenceEquals(input, take.Input)
            ? take
            : new PhysicalTakeNode(take.Count, input);
    }

    private static void AddDecision(
        PhysicalOptimizationState state,
        TakeStrategyKind kind,
        string reason)
    {
        state.AddDecision(new PlanningDecision(
            PlanningDecisionCategory.OrderingStrategy,
            "OrderingStrategySelection",
            "order",
            kind.ToString(),
            PlanningConfidence.High,
            reason));
    }

    private static bool ContainsNonWindowRowNumberProjection(PhysicalNode node)
    {
        if (node is PhysicalProjectNode project &&
            project.Fields.Any(static field =>
                field.Expression is MethodCall methodCall && IsRowNumberMethod(methodCall.Method)))
        {
            return true;
        }

        return node.Children.Any(ContainsNonWindowRowNumberProjection);
    }

    private static bool IsRowNumberMethod(System.Reflection.MethodInfo method)
    {
        if (!string.Equals(method.Name, "RowNumber", StringComparison.Ordinal))
            return false;

        var declaringType = method.DeclaringType;
        return declaringType is not null && typeof(LibraryBase).IsAssignableFrom(declaringType);
    }
}
