using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Analysis;

/// <summary>
/// Classifies a scalar used by a recursive member before a planner changes its
/// evaluation lifetime.  Recursive fixpoint iterations are a hard boundary:
/// only stable, frontier-independent scalars may cross it.
/// </summary>
internal enum RecursiveScalarInvariantKind
{
    FrontierIndependent,
    FrontierDependent,
    Volatile,
    Stream,
    Aggregate,
    Window,
    Unknown
}

internal sealed record RecursiveScalarInvariantFact(
    RecursiveScalarInvariantKind Kind,
    bool IsAnchorInvariant,
    bool IsStable,
    string Reason)
{
    public bool CanHoist => Kind == RecursiveScalarInvariantKind.FrontierIndependent &&
                           IsAnchorInvariant &&
                           IsStable;
}

internal static class RecursiveScalarInvariantFacts
{
    public static RecursiveScalarInvariantFact Classify(
        IrExpression expression,
        IReadOnlySet<string> frontierAliases,
        bool isAnchorInvariant = true)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentNullException.ThrowIfNull(frontierAliases);

        if (expression is AggregateRef)
        {
            return new(
                RecursiveScalarInvariantKind.Aggregate,
                isAnchorInvariant,
                false,
                "Aggregate values belong to the current recursive iteration.");
        }

        if (expression is WindowFunctionRef)
        {
            return new(
                RecursiveScalarInvariantKind.Window,
                isAnchorInvariant,
                false,
                "Window values belong to the current recursive iteration.");
        }

        if (expression is CteTableRef)
        {
            return new(
                RecursiveScalarInvariantKind.Stream,
                isAnchorInvariant,
                false,
                "Row streams cannot cross a recursive fixpoint boundary.");
        }

        if (AliasRefExtractor.Extract(expression).Any(frontierAliases.Contains))
        {
            return new(
                RecursiveScalarInvariantKind.FrontierDependent,
                false,
                false,
                "The scalar depends on the current recursive frontier row.");
        }

        if (!ExpressionStabilityAnalyzer.IsStable(expression))
        {
            return new(
                RecursiveScalarInvariantKind.Volatile,
                isAnchorInvariant,
                false,
                "The scalar is volatile or has an unknown evaluation contract.");
        }

        return new(
            RecursiveScalarInvariantKind.FrontierIndependent,
            isAnchorInvariant,
            true,
            "The scalar is stable and independent of the recursive frontier.");
    }

    public static bool CanHoist(
        IrExpression expression,
        IReadOnlySet<string> frontierAliases,
        bool isAnchorInvariant = true) =>
        Classify(expression, frontierAliases, isAnchorInvariant).CanHoist;
}
