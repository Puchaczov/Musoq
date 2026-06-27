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

        if (method.Method.DeclaringType != typeof(LibraryBase))
            return false;

        return method.Method.Name switch
        {
            nameof(LibraryBase.Contains) => method.Arguments.Count == 2 &&
                                            method.Arguments[0].ReturnType == typeof(string) &&
                                            method.Arguments[1].ReturnType == typeof(string),
            nameof(LibraryBase.StartsWith) => method.Arguments.Count == 2 &&
                                              method.Arguments[0].ReturnType == typeof(string) &&
                                              method.Arguments[1].ReturnType == typeof(string),
            nameof(LibraryBase.ToDecimal) => method.Arguments.Count == 1 &&
                                             CanRenderToDecimalWithoutTarget(method.Arguments[0].ReturnType),
            _ => false
        };
    }

    public static bool CanRenderPerInvocation(ExecutionMethodCall method)
    {
        var targetType = method.Method.DeclaringType;
        return method.Method.GetCustomAttribute<NonDeterministicAttribute>() != null &&
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
}
