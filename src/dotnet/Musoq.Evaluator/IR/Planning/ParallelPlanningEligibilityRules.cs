using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal static class ParallelPlanningEligibilityRules
{
    public static ParallelEligibilityCheck CanUseFilterProjectExpression(
        IrExpression? expression,
        PlanningRowShape sourceShape)
    {
        return CanUseExpression(expression, fieldRead => CanUseFilterProjectFieldRead(fieldRead, sourceShape));
    }

    public static ParallelEligibilityCheck CanUseAggregateGroupKeyExpression(IrExpression? expression)
    {
        return CanUseExpression(expression, static _ => ParallelEligibilityCheck.Enabled);
    }

    private static ParallelEligibilityCheck CanUseExpression(
        IrExpression? expression,
        Func<ColumnRef, ParallelEligibilityCheck> fieldReadEligibility)
    {
        return expression switch
        {
            null => ParallelEligibilityCheck.Enabled,
            ColumnRef { Stability: Musoq.Schema.ColumnStability.Volatile } columnRef =>
                ParallelEligibilityCheck.Skipped($"Expression reads volatile field {columnRef.Alias}.{columnRef.ColumnName}."),
            ColumnRef columnRef => fieldReadEligibility(columnRef),
            Literal => ParallelEligibilityCheck.Enabled,
            WildcardLiteral => ParallelEligibilityCheck.Enabled,
            ScriptParameterRef => ParallelEligibilityCheck.Enabled,
            ScriptVariableRef => ParallelEligibilityCheck.Enabled,
            RowPresence => ParallelEligibilityCheck.Enabled,
            Coalesce { Expressions.Length: 0 } => ParallelEligibilityCheck.Skipped("Coalesce expression has no operands."),
            AggregateRef => ParallelEligibilityCheck.Skipped("Expression reads aggregate state."),
            WindowFunctionRef => ParallelEligibilityCheck.Skipped("Expression reads a window value."),
            CteTableRef => ParallelEligibilityCheck.Skipped("Expression reads a CTE table directly."),
            MethodCall methodCall => CanUseMethodCall(methodCall, fieldReadEligibility),
            _ when IrExpressionTraversal.IsKnownExpressionKind(expression) => Combine(
                IrExpressionTraversal
                    .Children(expression)
                    .Select(child => CanUseExpression(child, fieldReadEligibility))
                    .ToArray()),
            _ => ParallelEligibilityCheck.Skipped($"Expression kind {expression.GetType().Name} is not parallel-safe.")
        };
    }

    public static bool ContainsMethodCall(IrExpression? expression)
    {
        return IrExpressionFacts.ContainsMethodCall(expression);
    }

    private static ParallelEligibilityCheck CanUseMethodCall(
        MethodCall methodCall,
        Func<ColumnRef, ParallelEligibilityCheck> fieldReadEligibility)
    {
        if (ExpressionStabilityAnalyzer.TryGetMethodInstabilityReason(
                methodCall.Method,
                "Expression",
                out var instabilityReason))
        {
            return ParallelEligibilityCheck.Skipped(instabilityReason);
        }

        return Combine(IrExpressionTraversal
            .Children(methodCall)
            .Select(argument => CanUseExpression(argument, fieldReadEligibility))
            .ToArray());
    }

    private static ParallelEligibilityCheck CanUseFilterProjectFieldRead(
        ColumnRef fieldRead,
        PlanningRowShape sourceShape)
    {
        var field = PlanningRowShapeLookup.ResolveField(sourceShape, fieldRead);
        if (field == null)
            return ParallelEligibilityCheck.Enabled;

        return field.AccessKind is PlanningFieldAccessKind.ExpandoDictionary
            or PlanningFieldAccessKind.ReflectedMember
            or PlanningFieldAccessKind.NestedClrMember
            or PlanningFieldAccessKind.NestedPositional
            ? ParallelEligibilityCheck.Skipped($"Expression reads field {fieldRead.Alias}.{fieldRead.ColumnName} through dynamic or reflected access.")
            : ParallelEligibilityCheck.Enabled;
    }

    private static ParallelEligibilityCheck Combine(params ParallelEligibilityCheck[] checks)
    {
        return checks.FirstOrDefault(static check => !check.IsEligible) ?? ParallelEligibilityCheck.Enabled;
    }
}
