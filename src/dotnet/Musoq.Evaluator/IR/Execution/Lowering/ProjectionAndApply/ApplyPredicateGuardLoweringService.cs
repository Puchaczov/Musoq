using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Physical.Rewriting;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution.Lowering.ProjectionAndApply;

internal sealed record ApplyPredicateGuardLoweringResult(
    IReadOnlyList<ExecutionNode> GuardNodes,
    IReadOnlyList<ApplyPredicateMovementPlan> LoweredPlans)
{
    public static ApplyPredicateGuardLoweringResult Empty { get; } = new([], []);
}

internal static class ApplyPredicateGuardLoweringService
{
    public static ApplyPredicateGuardLoweringResult Lower(
        IReadOnlyList<ApplyPredicateMovementPlan> plans,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (plans.Count == 0)
            return ApplyPredicateGuardLoweringResult.Empty;

        var nodes = new List<ExecutionNode>(plans.Count);
        var lowered = new List<ApplyPredicateMovementPlan>(plans.Count);
        foreach (var plan in plans)
        {
            try
            {
                var condition = OuterApplyNullSubstitutionService.NormalizeBooleanOperand(
                    ExecutionExpressionConverter.Convert(plan.Predicate, sourceLookup));
                if (!condition.IsBuilt)
                    continue;

                nodes.Add(new ExecutionContinueIf(new ExecutionUnary(
                    UnaryOpKind.Not,
                    condition.RequireValue(),
                    typeof(bool))));
                lowered.Add(plan);
            }
            catch (NotSupportedException)
            {
                // Keep the residual predicate when an execution expression cannot be materialized safely.
            }
        }

        return new ApplyPredicateGuardLoweringResult(nodes, lowered);
    }

    public static PhysicalFilterNode? RemoveLoweredPredicates(
        PhysicalFilterNode? filter,
        IReadOnlyList<ApplyPredicateMovementPlan> loweredPlans)
    {
        if (filter == null || loweredPlans.Count == 0)
            return filter;

        var loweredTexts = loweredPlans
            .SelectMany(static plan => new[] { plan.PredicateText, plan.ResidualPredicateText })
            .ToHashSet(StringComparer.Ordinal);
        var allConjuncts = SplitTopLevelConjuncts(filter.Predicate).ToArray();
        var conjuncts = allConjuncts
            .Where(conjunct => !loweredTexts.Contains(IrExpressionPrinter.Print(conjunct)))
            .ToArray();
        if (conjuncts.Length == allConjuncts.Length)
            return filter;

        if (conjuncts.Length == 0)
            return null;

        var residual = conjuncts[0];
        for (var index = 1; index < conjuncts.Length; index++)
        {
            residual = new BinaryOp(BinaryOpKind.And, residual, conjuncts[index], typeof(bool));
        }

        return new PhysicalFilterNode(residual, filter.Input);
    }

    public static PhysicalNode RemoveLoweredPredicatesFromStatement(
        PhysicalNode statement,
        IReadOnlyList<ApplyPredicateMovementPlan> loweredPlans)
    {
        if (loweredPlans.Count == 0)
            return statement;

        if (statement is PhysicalMultiStatementNode { Statements.Length: 1 } multiStatement)
        {
            return multiStatement with
            {
                Statements = [RemoveLoweredPredicatesFromStatement(multiStatement.Statements[0], loweredPlans)]
            };
        }

        return RemoveLoweredFilters(statement, loweredPlans);
    }

    private static PhysicalNode RemoveLoweredFilters(
        PhysicalNode node,
        IReadOnlyList<ApplyPredicateMovementPlan> loweredPlans)
    {
        if (node is PhysicalFilterNode filter)
        {
            var input = RemoveLoweredFilters(filter.Input, loweredPlans);
            var residual = RemoveLoweredPredicates(
                new PhysicalFilterNode(filter.Predicate, input),
                loweredPlans);
            return residual ?? input;
        }

        return PhysicalPlanRewriter.RewriteChildren(
            node,
            child => RemoveLoweredFilters(child, loweredPlans));
    }

    public static IReadOnlyList<string> CollectCteNames(PhysicalNode node)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectCteNames(node, names);
        return names.ToArray();
    }

    private static void CollectCteNames(PhysicalNode node, ISet<string> names)
    {
        if (node is PhysicalCteRefNode cteRef)
        {
            names.Add(cteRef.CteName);
            return;
        }

        foreach (var child in node.Children)
            CollectCteNames(child, names);
    }

    private static IEnumerable<IrExpression> SplitTopLevelConjuncts(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } and)
        {
            foreach (var left in SplitTopLevelConjuncts(and.Left))
                yield return left;

            foreach (var right in SplitTopLevelConjuncts(and.Right))
                yield return right;

            yield break;
        }

        yield return predicate;
    }
}
