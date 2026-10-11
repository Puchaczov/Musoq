using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;

namespace Musoq.Evaluator.IR.Execution;

internal static class WindowRegistrationLoweringHelpers
{
    internal static Type ResolveWindowPartitionKeyElementType(ExecutionExpression partitionKey)
    {
        return IsSafeTypedWindowKeyElement(partitionKey.ReturnType.ResolveClrType())
            ? partitionKey.ReturnType.ResolveClrType()
            : typeof(object);
    }

    internal static bool CanUseTypedWindowKeyElement(Type type)
    {
        return IsSafeTypedWindowKeyElement(type);
    }

    internal static Type CreatePluginWindowResultArrayType(WindowRegistration registration)
    {
        return registration.ReturnType.MakeArrayType();
    }

    internal static string? CreateWindowPartitionSignature(WindowRegistration registration)
    {
        return registration.PartitionKeys.Length == 0
            ? null
            : string.Concat("P|", ExecutionExpressionFingerprint.ForWindowExpressionList(registration.PartitionKeys));
    }

    internal static string? CreateWindowOrderSignature(WindowRegistration registration)
    {
        if (registration.OrderKeys.Length == 0)
            return null;

        return string.Concat(
            "O|",
            string.Join(
                "|",
                registration.OrderKeys.Select(key =>
                    string.Concat(
                        key.Descending ? "D:" : "A:", key.NullOrdering, ":",
                        ExecutionExpressionFingerprint.ForWindowExpression(key.Expression)))));
    }

    internal static string CreateWindowPartitionListSignature(WindowRegistration registration)
    {
        return registration.PartitionKeys.Length == 0
            ? "P|"
            : string.Concat("P|", ExecutionExpressionFingerprint.ForWindowExpressionList(registration.PartitionKeys));
    }

    internal static string? CreateWindowSortedPartitionListSignature(WindowRegistration registration)
    {
        var orderSignature = CreateWindowOrderSignature(registration);
        return orderSignature == null
            ? null
            : string.Concat("S|", CreateWindowPartitionListSignature(registration), "|", orderSignature);
    }

    private static bool IsSafeTypedWindowKeyElement(Type type)
    {
        if (type.IsGenericType && type.Namespace == typeof(ValueTuple).Namespace &&
            type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
        {
            return type.GetGenericArguments().All(IsSafeTypedWindowKeyElement);
        }

        if (Nullable.GetUnderlyingType(type) is { } underlyingType)
            return IsSafeTypedWindowKeyElement(underlyingType);

        return type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(bool) ||
               type == typeof(char) ||
               type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double);
    }

}
