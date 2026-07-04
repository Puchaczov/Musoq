using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Execution;
using Musoq.Plugins.Attributes;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Execution;

internal static class MethodTargetCachePolicy
{
    public static bool ShouldCache(
        ExecutionMethodCall method,
        bool allowNonDecimalValueTypeMethod)
    {
        if (method.InjectedSource != null ||
            method.Arguments.Count != 1 ||
            method.Method.GetCustomAttribute<NonDeterministicAttribute>() != null ||
            method.Method.GetParameters().Any(static parameter => parameter.GetCustomAttribute<InjectQueryStatsAttribute>() != null))
        {
            return false;
        }

        var keyType = method.Arguments[0].ReturnType;
        if (!IsNonNullableValueType(keyType) ||
            !IsNonNullableValueType(method.ReturnType))
        {
            return false;
        }

        return method.ReturnType == typeof(decimal) || allowNonDecimalValueTypeMethod;
    }

    private static bool IsNonNullableValueType(Type type)
    {
        return type.IsValueType && Nullable.GetUnderlyingType(type) == null;
    }
}
