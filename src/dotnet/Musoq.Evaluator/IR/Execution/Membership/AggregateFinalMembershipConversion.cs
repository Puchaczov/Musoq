using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateFinalMembershipConversion
{
    public static bool CanConvert(InCheck membership, AggregateFinalizationGroupKeys keys,
        IReadOnlyList<AggregateBinding> bindings, IReadOnlyDictionary<string, AggregateBinding> identifiers) =>
        PhysicalLoweringImplementation.CanFinalizeAggregateExpression(membership.Expression, keys, bindings, identifiers) &&
        membership.Values.All(value => PhysicalLoweringImplementation.CanFinalizeAggregateExpression(value, keys, bindings, identifiers));

    public static LoweringAttempt<ExecutionExpression> Convert(InCheck membership, AggregateFinalizationContext context)
    {
        var operand = PhysicalLoweringImplementation.ConvertAggregateFinalExpression(membership.Expression, context);
        if (!operand.IsBuilt) return operand;
        var values = new List<ExecutionExpression>();
        foreach (var value in membership.Values)
        {
            var converted = PhysicalLoweringImplementation.ConvertAggregateFinalExpression(value, context);
            if (!converted.IsBuilt) return converted;
            values.Add(converted.Value);
        }
        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionInCheck(operand.Value, values,
            membership.ReturnType, isNegated: membership.IsNegated)
        { UsesSqlNullSemantics = membership.SqlSemantics, TypedBinding = ExecutionMembershipBindingResolver.Bind(membership) });
    }
}
