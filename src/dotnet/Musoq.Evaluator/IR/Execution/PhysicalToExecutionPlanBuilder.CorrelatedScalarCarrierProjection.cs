using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static ExecutionExpression NarrowCorrelatedScalarCarrierProjection(
        ExecutionExpression matchedValue,
        OuterApplyNullSubstitutionResult unmatched)
    {
        if (unmatched.IsUnknown ||
            !IsCorrelatedScalarCarrier(unmatched.Expression.ReturnType.ClrType) ||
            Nullable.GetUnderlyingType(matchedValue.ReturnType.ClrType) != unmatched.Expression.ReturnType.ClrType)
        {
            return matchedValue;
        }

        return matchedValue with { ReturnType = unmatched.Expression.ReturnType };
    }

    private static bool IsCorrelatedScalarCarrier(Type type)
    {
        return type.IsGenericType &&
               type.GetGenericTypeDefinition() == typeof(CorrelatedScalarSubqueryResult<>);
    }
}
