using Musoq.Evaluator.IR.Logical.Nodes;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourceBoundaryPlanner
{
    private static SourceBoundaryPlan CreateApplyBoundaryPlan(ApplyNode apply, int index)
    {
        var leftAliases = CollectProducedAliases(apply.Left);
        var rightAliases = CollectProducedAliases(apply.Right);
        var dependencyAliases = CollectDependencyAliases(apply.Right);
        var mode = ResolveApplyInputMode(leftAliases, dependencyAliases);
        var confidence = mode == SourceBoundaryInputMode.Unknown ? PlanningConfidence.Low : PlanningConfidence.High;
        var inputAliases = dependencyAliases.Length == 0 ? leftAliases : dependencyAliases;

        return new SourceBoundaryPlan(
            $"apply:{index}",
            SourceBoundaryKind.Apply,
            apply.Kind,
            mode,
            ResolveInvocationShape(mode),
            ResolveRowBehavior(apply.Kind),
            ResolveResultShape(apply.Right),
            ResolveCacheability(mode),
            ResolveCacheabilityConfidence(mode),
            FormatApplyTarget(leftAliases, rightAliases),
            inputAliases,
            rightAliases,
            confidence,
            CreateApplyBoundaryReason(apply.Kind, mode, dependencyAliases));
    }

    private static SourceBoundaryPlan CreateInterpretBoundaryPlan(InterpretSourceNode interpret, ApplyKind applyKind)
    {
        var inputAliases = CollectExpressionAliases(interpret.Arguments);
        var mode = inputAliases.Length == 0
            ? SourceBoundaryInputMode.Independent
            : SourceBoundaryInputMode.Correlated;

        return new SourceBoundaryPlan(
            $"interpret:{interpret.Alias}",
            SourceBoundaryKind.InterpretSource,
            applyKind,
            mode,
            ResolveInvocationShape(mode),
            ResolveRowBehavior(applyKind),
            ResolveResultShape(interpret.ResultType),
            ResolveCacheability(mode),
            ResolveCacheabilityConfidence(mode),
            $"{interpret.SchemaName}.{interpret.Kind} as {interpret.Alias}",
            inputAliases,
            [interpret.Alias],
            PlanningConfidence.High,
            CreateInterpretBoundaryReason(interpret, applyKind, inputAliases));
    }

    private static SourceBoundaryPlan CreatePropertyBoundaryPlan(PropertySourceNode propertySource, ApplyKind applyKind)
    {
        string[] inputAliases = string.IsNullOrWhiteSpace(propertySource.SourceAlias)
            ? []
            : [propertySource.SourceAlias];
        var mode = inputAliases.Length == 0 ? SourceBoundaryInputMode.Unknown : SourceBoundaryInputMode.Correlated;

        return new SourceBoundaryPlan(
            $"property:{propertySource.Alias}",
            SourceBoundaryKind.PropertySource,
            applyKind,
            mode,
            ResolveInvocationShape(mode),
            ResolveRowBehavior(applyKind),
            ResolveResultShape(propertySource.ResultType),
            ResolveCacheability(mode),
            ResolveCacheabilityConfidence(mode),
            $"{FormatPropertySource(propertySource)} as {propertySource.Alias}",
            inputAliases,
            [propertySource.Alias],
            inputAliases.Length == 0 ? PlanningConfidence.Low : PlanningConfidence.High,
            CreatePropertyBoundaryReason(propertySource, applyKind));
    }

    private static SourceBoundaryPlan CreateAccessMethodBoundaryPlan(AccessMethodSourceNode accessMethod, ApplyKind applyKind)
    {
        var inputAliases = CollectAccessMethodInputAliases(accessMethod);
        var mode = inputAliases.Length == 0 ? SourceBoundaryInputMode.Unknown : SourceBoundaryInputMode.Correlated;

        return new SourceBoundaryPlan(
            $"access:{accessMethod.Alias}",
            SourceBoundaryKind.AccessMethodSource,
            applyKind,
            mode,
            ResolveInvocationShape(mode),
            ResolveRowBehavior(applyKind),
            ResolveResultShape(accessMethod.ResultType),
            ResolveCacheability(mode),
            ResolveCacheabilityConfidence(mode),
            $"{IrExpressionPrinter.Print(accessMethod.MethodCallExpression)} as {accessMethod.Alias}",
            inputAliases,
            [accessMethod.Alias],
            inputAliases.Length == 0 ? PlanningConfidence.Low : PlanningConfidence.High,
            CreateAccessMethodBoundaryReason(accessMethod, applyKind, inputAliases));
    }

    private static PlanningDecision CreateDecision(SourceBoundaryPlan plan)
    {
        return new PlanningDecision(
            PlanningDecisionCategory.SourceInteraction,
            "SourceBoundaryPlan",
            plan.BoundaryId,
            $"{plan.Kind}/{plan.ApplyKind}/{plan.InputMode}",
            plan.Confidence,
            plan.Reason);
    }
}
