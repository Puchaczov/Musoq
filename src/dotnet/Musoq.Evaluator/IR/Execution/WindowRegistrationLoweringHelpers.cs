using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Plugins;

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

    internal static Type ResolveWindowOrderKeyElementType(IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        if (orderKeys.Count == 1)
        {
            var keyType = orderKeys[0].Expression.ReturnType.ResolveClrType();
            return IsSafeTypedWindowOrderKeyElement(keyType) ? keyType : typeof(object);
        }

        if (!CanUseValueTupleWindowOrderKey(orderKeys))
            return typeof(object);

        return CreateValueTupleType(orderKeys.Select(key => key.Expression.ReturnType.ResolveClrType()).ToArray());
    }

    internal static Type CreatePluginWindowResultArrayType(WindowRegistration registration)
    {
        return registration.ReturnType.MakeArrayType();
    }

    internal static bool TryGetPluginWindowTypes(
        MethodInfo factoryMethod,
        out Type inputType,
        out Type resultType)
    {
        var windowFunctionType = factoryMethod.ReturnType.IsGenericType &&
                                 factoryMethod.ReturnType.GetGenericTypeDefinition() == typeof(IWindowFunction<,>)
            ? factoryMethod.ReturnType
            : factoryMethod.ReturnType
                .GetInterfaces()
                .FirstOrDefault(static type =>
                    type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IWindowFunction<,>));

        if (windowFunctionType == null)
        {
            inputType = typeof(object);
            resultType = typeof(object);
            return false;
        }

        var arguments = windowFunctionType.GetGenericArguments();
        inputType = arguments[0];
        resultType = arguments[1];
        return true;
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

    private static bool CanUseValueTupleWindowOrderKey(IReadOnlyList<ExecutionWindowOrderKey> orderKeys)
    {
        if (orderKeys.Count is < 2 or > 7)
            return false;

        var firstDirection = orderKeys[0].Descending;
        return orderKeys.All(key =>
            key.Descending == firstDirection &&
            IsSafeTypedWindowOrderKeyElement(key.Expression.ReturnType.ResolveClrType()));
    }

    private static bool IsSafeTypedWindowKeyElement(Type type)
    {
        if (type.IsGenericType && type.Namespace == typeof(ValueTuple).Namespace &&
            type.Name.StartsWith("ValueTuple`", StringComparison.Ordinal))
        {
            return type.GetGenericArguments().All(IsSafeTypedWindowKeyElement);
        }

        if (Nullable.GetUnderlyingType(type) != null)
            return false;

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

    private static bool IsSafeTypedWindowOrderKeyElement(Type type)
    {
        return IsSafeTypedWindowKeyElement(type) && ImplementsComparableOfSelf(type);
    }

    private static bool ImplementsComparableOfSelf(Type type)
    {
        var comparableType = typeof(IComparable<>).MakeGenericType(type);
        return comparableType.IsAssignableFrom(type);
    }

    private static Type CreateValueTupleType(Type[] keyTypes)
    {
        return keyTypes.Length switch
        {
            2 => typeof(ValueTuple<,>).MakeGenericType(keyTypes.ToArray()),
            3 => typeof(ValueTuple<,,>).MakeGenericType(keyTypes.ToArray()),
            4 => typeof(ValueTuple<,,,>).MakeGenericType(keyTypes.ToArray()),
            5 => typeof(ValueTuple<,,,,>).MakeGenericType(keyTypes.ToArray()),
            6 => typeof(ValueTuple<,,,,,>).MakeGenericType(keyTypes.ToArray()),
            7 => typeof(ValueTuple<,,,,,,>).MakeGenericType(keyTypes.ToArray()),
            _ => throw new NotSupportedException($"Execution IR window value-tuple keys support 2 through 7 parts. Found {keyTypes.Length}.")
        };
    }
}
