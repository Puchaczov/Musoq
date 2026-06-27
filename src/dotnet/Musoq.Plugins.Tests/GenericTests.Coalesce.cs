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
        Assert.AreEqual(expected, method.Invoke(Library, arguments), name);
    }

    public static IEnumerable<object?[]> NumericCoalesceCases()
    {
        foreach (var method in GetNumericCoalesceMethods())
        {
            var elementType = method.GetParameters()[0].ParameterType.GetElementType()!;

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
        var elementType = method.GetParameters()[0].ParameterType.GetElementType()!;
        return $"Coalesce_{GetUnderlyingType(elementType).Name}";
    }

    private static object?[] ReflectionCase(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        return [name, method, arguments, expected];
    }
}