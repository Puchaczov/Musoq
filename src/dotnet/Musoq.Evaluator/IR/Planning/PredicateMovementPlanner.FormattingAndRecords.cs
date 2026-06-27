using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicateMovementPlanner
{
    private static IEnumerable<IrExpression> SplitConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            foreach (var left in SplitConjuncts(and.Left))
                yield return left;

            foreach (var right in SplitConjuncts(and.Right))
                yield return right;

            yield break;
        }

        yield return predicate;
    }

    private static string[] ExtractAliases(IrExpression predicate)
    {
        return AliasRefExtractor.Extract(predicate).ToArray();
    }

    private static bool ContainsProducedAlias(LogicalNode node, string alias)
    {
        return node switch
        {
            SchemaScanNode scan => string.Equals(scan.Alias, alias, StringComparison.OrdinalIgnoreCase),
            InterpretSourceNode interpret => string.Equals(interpret.Alias, alias, StringComparison.OrdinalIgnoreCase),
            PropertySourceNode property => string.Equals(property.Alias, alias, StringComparison.OrdinalIgnoreCase),
            AccessMethodSourceNode accessMethod => string.Equals(accessMethod.Alias, alias, StringComparison.OrdinalIgnoreCase),
            CteRefNode cteRef => string.Equals(cteRef.Alias, alias, StringComparison.OrdinalIgnoreCase),
            ValuesScanNode values => string.Equals(values.Alias, alias, StringComparison.OrdinalIgnoreCase),
            _ => node.Children.Any(child => ContainsProducedAlias(child, alias))
        };
    }

    private static string CreateMovedWhereKey(string alias, IrExpression predicate)
    {
        return $"{alias}:{IrExpressionPrinter.Print(predicate)}";
    }

    private static string CreateMovementId(
        PredicatePlacementOrigin origin,
        PredicateMovementSide side,
        string alias,
        IrExpression predicate)
    {
        return $"{origin}:{side}:{alias}:{IrExpressionPrinter.Print(predicate)}";
    }

    private static string CreateSkippedId(
        PredicatePlacementOrigin origin,
        string alias,
        IrExpression predicate)
    {
        return $"{origin}:Skipped:{alias}:{IrExpressionPrinter.Print(predicate)}";
    }

    private sealed record AliasEligibility(bool IsEligible, string Reason)
    {
        public static AliasEligibility Eligible()
        {
            return new AliasEligibility(true, string.Empty);
        }

        public static AliasEligibility NotEligible(string reason)
        {
            return new AliasEligibility(false, reason);
        }
    }

    private sealed record SideResolution(bool IsEligible, PredicateMovementSide Side, string Reason)
    {
        public SideResolution AddReason(string reason)
        {
            if (!IsEligible)
                return this;

            return this with { Reason = $"{Reason} {reason}" };
        }

        public static SideResolution Eligible(PredicateMovementSide side, string reason)
        {
            return new SideResolution(true, side, reason);
        }

        public static SideResolution NotEligible(string reason)
        {
            return new SideResolution(false, PredicateMovementSide.Left, reason);
        }
    }

    private sealed record PlanCreationResult(bool IsCreated, PredicateMovementPlan? Plan, string SkipReason)
    {
        public static PlanCreationResult Created(PredicateMovementPlan plan)
        {
            return new PlanCreationResult(true, plan, string.Empty);
        }

        public static PlanCreationResult Skipped(string reason)
        {
            return new PlanCreationResult(false, null, reason);
        }
    }

    private sealed record ExpressionSafety(bool IsSafe, string Reason)
    {
        public static ExpressionSafety Safe()
        {
            return new ExpressionSafety(true, "Expression is deterministic.");
        }

        public static ExpressionSafety Unsafe(string reason)
        {
            return new ExpressionSafety(false, reason);
        }
    }
}
