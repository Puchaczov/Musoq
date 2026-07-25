using Musoq.Plugins;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionExpression NarrowCorrelatedScalarCarrierProjection(
        ExecutionExpression matchedValue,
        OuterApplyNullSubstitutionResult unmatched)
    {
        if (unmatched.IsUnknown ||
            !IsCorrelatedScalarCarrier(unmatched.Expression.ReturnType.ResolveClrType()) ||
            Nullable.GetUnderlyingType(matchedValue.ReturnType.ResolveClrType()) != unmatched.Expression.ReturnType.ResolveClrType())
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
