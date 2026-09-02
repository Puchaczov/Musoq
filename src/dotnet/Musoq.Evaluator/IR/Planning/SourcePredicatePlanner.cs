using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Visitors;
using Musoq.Parser.Nodes;
using Musoq.Schema;
using SchemaFromNode = Musoq.Parser.Nodes.From.SchemaFromNode;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class SourcePredicatePlanner
{
    private sealed record PredicateConversionResult(
        IrExpression[] PushedPredicates,
        string? UnsupportedReason);

    public static SourcePredicatePlanningResult Plan(
        IReadOnlyDictionary<SchemaFromNode, WhereNode> rawWhereNodes,
        IReadOnlyDictionary<string, ISchemaColumn[]> inferredColumns)
    {
        ArgumentNullException.ThrowIfNull(rawWhereNodes);
        ArgumentNullException.ThrowIfNull(inferredColumns);
        if (rawWhereNodes.Count == 0)
        {
            return new SourcePredicatePlanningResult(
                new Dictionary<string, SourcePredicatePlan>(StringComparer.Ordinal),
                new Dictionary<string, IrExpression[]>(StringComparer.Ordinal),
                []);
        }

        var converter = CreateExpressionConverter(inferredColumns);
        var plans = new Dictionary<string, SourcePredicatePlan>(StringComparer.Ordinal);
        var pushedPredicates = new Dictionary<string, IrExpression[]>(StringComparer.Ordinal);
        var decisions = new List<PlanningDecision>();

        foreach (var (sourceNode, rawWhereNode) in rawWhereNodes)
        {
            if (string.IsNullOrWhiteSpace(sourceNode.Id))
                continue;

            var pushedWhereNode = CreatePushedWhereNode(sourceNode, rawWhereNode);
            var conversionResult = CreatePushedPredicates(
                converter,
                pushedWhereNode.Expression);
            var reason = CreatePredicateReason(
                rawWhereNode,
                pushedWhereNode,
                conversionResult.PushedPredicates,
                conversionResult.UnsupportedReason);
            var confidence = ResolveConfidence(conversionResult);

            plans[sourceNode.Id] = new SourcePredicatePlan(
                sourceNode.Id,
                sourceNode.Alias,
                pushedWhereNode,
                conversionResult.PushedPredicates,
                reason,
                confidence);
            pushedPredicates[sourceNode.Id] = conversionResult.PushedPredicates;

            decisions.Add(new PlanningDecision(
                PlanningDecisionCategory.PredicatePushdown,
                "SourcePredicatePlan",
                sourceNode.Id,
                conversionResult.PushedPredicates.Length == 0 ? "RetainedRuntimeOnly" : "Pushed",
                confidence,
                reason));
        }

        return new SourcePredicatePlanningResult(plans, pushedPredicates, decisions);
    }

    private static WhereNode CreatePushedWhereNode(SchemaFromNode sourceNode, WhereNode rawWhereNode)
    {
        var rewriter = new RewriteWhereExpressionToPassItToDataSourceVisitor(sourceNode);
        var rewriteTraverser = new RewriteWhereExpressionToPassItToDataSourceTraverseVisitor(rewriter);

        rawWhereNode.Accept(rewriteTraverser);

        return rewriter.WhereNode;
    }

    private static PredicateConversionResult CreatePushedPredicates(
        ExpressionConverter converter,
        Node expression)
    {
        try
        {
            var predicate = converter.Convert(expression);
            return new PredicateConversionResult(ExtractNonNeutralPredicates(predicate).ToArray(), null);
        }
        catch (NotSupportedException ex)
        {
            return new PredicateConversionResult([], $"Predicate expression could not be converted: {ex.Message}");
        }
    }

    private static IEnumerable<IrExpression> ExtractNonNeutralPredicates(IrExpression predicate)
    {
        if (predicate is BinaryOp { Kind: BinaryOpKind.And } andExpression)
        {
            foreach (var nestedPredicate in ExtractNonNeutralPredicates(andExpression.Left))
                yield return nestedPredicate;

            foreach (var nestedPredicate in ExtractNonNeutralPredicates(andExpression.Right))
                yield return nestedPredicate;

            yield break;
        }

        if (IsNeutralPredicate(predicate))
            yield break;

        yield return predicate;
    }

    private static bool IsNeutralPredicate(IrExpression predicate)
    {
        if (predicate is not BinaryOp { Kind: BinaryOpKind.Equal, Left: Literal left, Right: Literal right })
            return false;

        return IsOneLiteral(left.Value) && IsOneLiteral(right.Value);
    }

    private static bool IsOneLiteral(object? value)
    {
        return value switch
        {
            byte literal => literal == 1,
            sbyte literal => literal == 1,
            short literal => literal == 1,
            ushort literal => literal == 1,
            int literal => literal == 1,
            uint literal => literal == 1,
            long literal => literal == 1,
            ulong literal => literal == 1,
            float literal => Math.Abs(literal - 1f) < float.Epsilon,
            double literal => Math.Abs(literal - 1d) < double.Epsilon,
            decimal literal => literal == 1m,
            string literal => string.Equals(literal, "1", StringComparison.Ordinal),
            _ => false
        };
    }

    private static string CreatePredicateReason(
        WhereNode rawWhereNode,
        WhereNode pushedWhereNode,
        IrExpression[] pushedPredicates,
        string? unsupportedReason)
    {
        if (!string.IsNullOrWhiteSpace(unsupportedReason))
            return unsupportedReason;

        if (pushedPredicates.Length > 0)
            return $"Pushed {pushedPredicates.Length} source-local predicate(s); runtime filter remains for full predicate semantics.";

        return string.Equals(rawWhereNode.Expression.ToString(), pushedWhereNode.Expression.ToString(), StringComparison.Ordinal)
            ? "No non-neutral source-local predicate was available for pushdown."
            : "Predicate was retained for runtime because source-local rewrite produced a neutral predicate.";
    }

    private static PlanningConfidence ResolveConfidence(PredicateConversionResult conversionResult)
    {
        if (!string.IsNullOrWhiteSpace(conversionResult.UnsupportedReason))
            return PlanningConfidence.Low;

        return conversionResult.PushedPredicates.Length == 0
            ? PlanningConfidence.Medium
            : PlanningConfidence.High;
    }
}
