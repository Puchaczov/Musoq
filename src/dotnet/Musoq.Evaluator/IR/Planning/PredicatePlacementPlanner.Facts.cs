using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using AliasRefExtractor = Musoq.Evaluator.IR.Expressions.AliasRefExtractor;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PredicatePlacementPlanner
{
    private static PredicatePlacementPlan CreatePlan(
        PredicatePlacementOrigin origin,
        int index,
        string predicateText,
        string[] aliases,
        PredicateEarliestPlacement placement,
        PlanningConfidence confidence,
        string reason)
    {
        return new PredicatePlacementPlan(
            CreatePredicateId(origin, index),
            origin,
            predicateText,
            aliases,
            placement,
            confidence,
            reason)
        {
            ConjunctGroupId = CreateConjunctGroupId(origin, index, aliases),
            AliasOwners = aliases.Select(alias => $"{alias}:{predicateText}").ToArray(),
            IsDeterministic = true,
            NullSensitivity = aliases.Length == 0
                ? PredicateNullSensitivity.NullInsensitive
                : PredicateNullSensitivity.NullSensitive,
            BlockedReasons = CreateBlockedReasons(placement, reason)
        };
    }

    private static PredicatePlacementPlan CreatePlan(
        PredicatePlacementOrigin origin,
        int index,
        IrExpression predicate,
        PredicateEarliestPlacement placement,
        PlanningConfidence confidence,
        string reason)
    {
        var aliases = ExtractAliases(predicate);
        return CreatePlan(
            origin,
            index,
            IrExpressionPrinter.Print(predicate),
            aliases,
            placement,
            confidence,
            reason) with
        {
            AliasOwners = ExtractAliasOwners(predicate),
            IsDeterministic = IsDeterministicExpression(predicate),
            NullSensitivity = IsNullSensitiveExpression(predicate)
                ? PredicateNullSensitivity.NullSensitive
                : PredicateNullSensitivity.NullInsensitive,
            BlockedReasons = CreateBlockedReasons(placement, reason, predicate)
        };
    }

    private static string CreateConjunctGroupId(
        PredicatePlacementOrigin origin,
        int index,
        IReadOnlyList<string> aliases)
    {
        var aliasPart = aliases.Count == 0
            ? "constant"
            : string.Join("+", aliases);

        return $"{origin}:conjunct:{index}:{aliasPart}";
    }

    private static string[] ExtractAliasOwners(IrExpression predicate)
    {
        return ColumnRefExtractor.Extract(predicate)
            .Select(static column => string.IsNullOrWhiteSpace(column.Alias)
                ? column.ColumnName
                : $"{column.Alias}.{column.ColumnName}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static column => column, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string[] CreateBlockedReasons(
        PredicateEarliestPlacement placement,
        string reason)
    {
        return IsBlockedPlacement(placement) ? [reason] : [];
    }

    private static string[] CreateBlockedReasons(
        PredicateEarliestPlacement placement,
        string reason,
        IrExpression predicate)
    {
        var reasons = new List<string>();

        if (IsBlockedPlacement(placement))
            reasons.Add(reason);

        AddDeterminismBlockedReasons(predicate, reasons);
        return reasons
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsBlockedPlacement(PredicateEarliestPlacement placement)
    {
        return placement is PredicateEarliestPlacement.RuntimeFilter
            or PredicateEarliestPlacement.PostJoin
            or PredicateEarliestPlacement.PostAggregate
            or PredicateEarliestPlacement.PostWindow
            or PredicateEarliestPlacement.SourceRuntimeFilter;
    }

    private static bool IsNullSensitiveExpression(IrExpression expression)
    {
        return AliasRefExtractor.Extract(expression).Count > 0;
    }
}
