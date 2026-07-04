using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins.Attributes;

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
        if (methodCall.Method.GetCustomAttribute<NonDeterministicAttribute>() != null)
            return ParallelEligibilityCheck.Skipped($"Expression contains non-deterministic method {methodCall.Method.Name}.");

        if (methodCall.Method.GetParameters()
            .Any(static parameter => parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null))
        {
            return ParallelEligibilityCheck.Skipped($"Expression calls {methodCall.Method.Name}, which injects query statistics.");
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
