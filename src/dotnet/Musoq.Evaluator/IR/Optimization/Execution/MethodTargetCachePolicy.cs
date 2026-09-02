using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Execution;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class MethodTargetCachePolicy
{
    public static bool ShouldCache(
        ExecutionMethodCall method,
        bool allowNonDecimalValueTypeMethod)
    {
        if (method.InjectedSource != null ||
            method.Arguments.Count != 1 ||
            !ExpressionStabilityAnalyzer.IsStableMethod(method.Method.ResolveClrMethod()))
        {
            return false;
        }

        var keyType = method.Arguments[0].ReturnType.ResolveClrType();
        if (!IsNonNullableValueType(keyType) ||
            !IsNonNullableValueType(method.ReturnType.ResolveClrType()))
        {
            return false;
        }

        return method.ReturnType.ResolveClrType() == typeof(decimal) || allowNonDecimalValueTypeMethod;
    }

    private static bool IsNonNullableValueType(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
    }
}
