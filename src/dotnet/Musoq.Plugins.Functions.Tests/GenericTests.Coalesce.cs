using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public sealed partial class GenericTests
{
    [TestMethod]
    [DynamicData(nameof(NumericCoalesceCases))]
    public void Coalesce_NumericOverloadCases_ReturnExpected(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        Assert.AreEqual(expected, InvokeCoalesce(method, arguments), name);
    }

    public static IEnumerable<object?[]> NumericCoalesceCases()
    {
        foreach (var method in GetNumericCoalesceMethods())
        {
            var elementType = VariadicParameter.ElementType(method.GetParameters()[0].ParameterType)!;

            yield return ReflectionCase(
                $"{FormatCoalesceName(method)}_FirstNotNull_ReturnsFirst",
                method,
                [CreateArray(elementType, 1, 2)],
                CreateTypedValue(1, method.ReturnType));

            yield return ReflectionCase(
                $"{FormatCoalesceName(method)}_FirstNull_ReturnsSecond",
                method,
                [CreateArray(elementType, null, 2)],
                CreateTypedValue(2, method.ReturnType));
        }
    }

    private static IEnumerable<MethodInfo> GetNumericCoalesceMethods()
    {
        return typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.DeclaringType == typeof(LibraryBase))
            .Where(method => method.Name == "Coalesce")
            .Where(method => !method.IsGenericMethodDefinition)
            .OrderBy(FormatCoalesceName, StringComparer.Ordinal);
    }

    private static Array CreateArray(Type elementType, params int?[] values)
    {
        var array = Array.CreateInstance(elementType, values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index] == null)
                continue;

            array.SetValue(CreateTypedValue(values[index]!.Value, elementType), index);
        }

        return array;
    }

    private static object CreateTypedValue(int value, Type type)
    {
        return Convert.ChangeType(value, GetUnderlyingType(type), CultureInfo.InvariantCulture);
    }

    private static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static string FormatCoalesceName(MethodInfo method)
    {
        var elementType = VariadicParameter.ElementType(method.GetParameters()[0].ParameterType)!;
        return $"Coalesce_{GetUnderlyingType(elementType).Name}";
    }

    private object? InvokeCoalesce(MethodInfo method, object?[] arguments)
    {
        var parameterType = method.GetParameters()[0].ParameterType;
        var elementType = GetUnderlyingType(VariadicParameter.ElementType(parameterType)!);
        var resultType = GetUnderlyingType(method.ReturnType);
        return Type.GetTypeCode(elementType) switch
        {
            TypeCode.Byte => InvokeNativeCoalesce<byte, byte>(Library, method, arguments),
            TypeCode.SByte => InvokeNativeCoalesce<sbyte, sbyte>(Library, method, arguments),
            TypeCode.Int16 => InvokeNativeCoalesce<short, short>(Library, method, arguments),
            TypeCode.UInt16 => InvokeNativeCoalesce<ushort, ushort>(Library, method, arguments),
            TypeCode.Int32 => InvokeNativeCoalesce<int, int>(Library, method, arguments),
            TypeCode.UInt32 => InvokeNativeCoalesce<uint, decimal>(Library, method, arguments),
            TypeCode.Int64 => InvokeNativeCoalesce<long, decimal>(Library, method, arguments),
            TypeCode.UInt64 => InvokeNativeCoalesce<ulong, decimal>(Library, method, arguments),
            TypeCode.Decimal => InvokeNativeCoalesce<decimal, decimal>(Library, method, arguments),
            _ => throw new InvalidOperationException($"No typed Coalesce fixture for {elementType} -> {resultType}.")
        };
    }

    private delegate TResult? NativeCoalesce<T, TResult>(ReadOnlySpan<T?> values)
        where T : struct where TResult : struct;

    private static object? InvokeNativeCoalesce<T, TResult>(LibraryBase library, MethodInfo method, object?[] arguments)
        where T : struct where TResult : struct
    {
        if (method.GetParameters()[0].ParameterType.IsArray)
            return method.CreateDelegate<Func<T?[], TResult?>>(library)((T?[])arguments[0]!);
        var operation = method.CreateDelegate<NativeCoalesce<T, TResult>>(library);
        return operation((T?[])arguments[0]!);
    }

    private static object?[] ReflectionCase(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        return [name, method, arguments, expected];
    }
}
