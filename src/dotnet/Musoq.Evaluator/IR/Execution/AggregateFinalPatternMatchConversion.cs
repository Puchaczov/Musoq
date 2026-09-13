using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateFinalPatternMatchConversion
{
    public static bool CanConvert(
        PatternMatch patternMatch,
        AggregateFinalizationGroupKeys groupKeys,
        IReadOnlyList<AggregateBinding> bindings,
        IReadOnlyDictionary<string, AggregateBinding> bindingsByIdentifier) =>
        PhysicalLoweringImplementation.CanFinalizeAggregateExpression(
            patternMatch.Expression, groupKeys, bindings, bindingsByIdentifier) &&
        PhysicalLoweringImplementation.CanFinalizeAggregateExpression(
            patternMatch.Pattern, groupKeys, bindings, bindingsByIdentifier);

    public static LoweringAttempt<ExecutionExpression> Convert(
        PatternMatch patternMatch,
        AggregateFinalizationContext context)
    {
        var expression = PhysicalLoweringImplementation.ConvertAggregateFinalExpression(
            patternMatch.Expression,
            context);
        if (!expression.IsBuilt)
            return expression;

        var pattern = PhysicalLoweringImplementation.ConvertAggregateFinalExpression(
            patternMatch.Pattern,
            context);
        if (!pattern.IsBuilt)
            return pattern;

        return LoweringAttempt<ExecutionExpression>.Built(new ExecutionPatternMatch(
            expression.Value,
            pattern.Value,
            patternMatch.Kind,
            patternMatch.ReturnType));
    }
}
