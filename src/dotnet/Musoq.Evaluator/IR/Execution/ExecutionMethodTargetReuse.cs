using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Execution;

internal static class ExecutionMethodTargetReuse
{
    public static bool CanRenderWithoutTarget(ExecutionMethodCall method)
    {
        if (CanRenderPerInvocation(method))
            return true;

        if (method.Method.ResolveClrMethod().DeclaringType != typeof(LibraryBase))
            return false;

        return method.Method.MethodName switch
        {
            nameof(LibraryBase.Contains) => method.Arguments.Count == 2 &&
                                            method.Arguments[0].ReturnType.ResolveClrType() == typeof(string) &&
                                            method.Arguments[1].ReturnType.ResolveClrType() == typeof(string),
            nameof(LibraryBase.StartsWith) => method.Arguments.Count == 2 &&
                                              method.Arguments[0].ReturnType.ResolveClrType() == typeof(string) &&
                                              method.Arguments[1].ReturnType.ResolveClrType() == typeof(string),
            nameof(LibraryBase.ToDecimal) => method.Arguments.Count == 1 &&
                                             CanRenderToDecimalWithoutTarget(method.Arguments[0].ReturnType.ResolveClrType()),
            nameof(LibraryBase.__CorrelatedScalarSubqueryResult) =>
                IsCorrelatedScalarSubqueryResultAccessor(method),
            _ => false
        };
    }

    public static bool CanRenderPerInvocation(ExecutionMethodCall method)
    {
        var targetType = method.Method.ResolveClrMethod().DeclaringType;
        return method.Method.ResolveClrMethod().GetCustomAttribute<NonDeterministicAttribute>() != null &&
               targetType != null &&
               !targetType.IsAbstract &&
               typeof(LibraryBase).IsAssignableFrom(targetType) &&
               targetType.GetConstructor(Type.EmptyTypes) != null;
    }

    public static bool TryGetReusableTargetType(
        MethodInfo method,
        [NotNullWhen(true)] out Type? targetType)
    {
        targetType = method.DeclaringType;
        if (method.IsStatic ||
            targetType == null ||
            targetType.IsAbstract ||
            !typeof(LibraryBase).IsAssignableFrom(targetType) ||
            method.GetCustomAttribute<NonDeterministicAttribute>() != null ||
            targetType.GetConstructor(Type.EmptyTypes) == null)
        {
            targetType = null;
            return false;
        }

        return true;
    }

    private static bool CanRenderToDecimalWithoutTarget(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(decimal);
    }

    private static bool IsCorrelatedScalarSubqueryResultAccessor(ExecutionMethodCall method)
    {
        if (method.Arguments.Count != 1)
            return false;

        var argumentType = method.Arguments[0].ReturnType.ResolveClrType();
        argumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
        return argumentType.IsGenericType &&
               argumentType.GetGenericTypeDefinition() == typeof(CorrelatedScalarSubqueryResult<>);
    }
}
