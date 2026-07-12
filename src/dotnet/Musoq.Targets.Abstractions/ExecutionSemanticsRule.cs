using System;

namespace Musoq.Targets.Abstractions;

public enum ExecutionSemanticsRuleId
{
    NullLogic,
    NullOrdering,
    IntegerRuntimeAddSubtractMultiply,
    IntegerConstantFoldingAddSubtractMultiply,
    IntegerAggregateAddSubtractMultiply,
    IntegerDivide,
    IntegerModulo,
    FloatingPoint,
    Decimal,
    StringEqualityOrderingHashing,
    TemporalValueSemantics,
    StrictCast,
    GroupingDistinctJoinSetEquality
}

public sealed record ExecutionSemanticsRule(
    ExecutionSemanticsRuleId Id,
    string Behavior)
{
    public string StableId => ExecutionSemanticsRuleIds.ToStableId(Id);
}

public static class ExecutionSemanticsRuleIds
{
    public static string ToStableId(ExecutionSemanticsRuleId id) => id switch
    {
        ExecutionSemanticsRuleId.NullLogic => "null.logic",
        ExecutionSemanticsRuleId.NullOrdering => "null.ordering",
        ExecutionSemanticsRuleId.IntegerRuntimeAddSubtractMultiply => "integer.runtime.add-subtract-multiply",
        ExecutionSemanticsRuleId.IntegerConstantFoldingAddSubtractMultiply => "integer.constant-folding.add-subtract-multiply",
        ExecutionSemanticsRuleId.IntegerAggregateAddSubtractMultiply => "integer.aggregate.add-subtract-multiply",
        ExecutionSemanticsRuleId.IntegerDivide => "integer.divide",
        ExecutionSemanticsRuleId.IntegerModulo => "integer.modulo",
        ExecutionSemanticsRuleId.FloatingPoint => "floating-point",
        ExecutionSemanticsRuleId.Decimal => "decimal",
        ExecutionSemanticsRuleId.StringEqualityOrderingHashing => "string.equality-ordering-hashing",
        ExecutionSemanticsRuleId.TemporalValueSemantics => "datetime-datetimeoffset-guid-timespan",
        ExecutionSemanticsRuleId.StrictCast => "strict-cast",
        ExecutionSemanticsRuleId.GroupingDistinctJoinSetEquality => "grouping-distinct-join-set-equality",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    internal static bool TryParse(string stableId, out ExecutionSemanticsRuleId id)
    {
        foreach (var candidate in Enum.GetValues<ExecutionSemanticsRuleId>())
        {
            if (string.Equals(ToStableId(candidate), stableId, StringComparison.Ordinal))
            {
                id = candidate;
                return true;
            }
        }

        id = default;
        return false;
    }
}
