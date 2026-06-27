using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

[TestClass]
public sealed class BitsOperationsBranchCoverageTests : PluginsTestBase
{
    private const int ShiftAmount = 1;
    private const ulong ShiftLeftValue = 1UL;
    private const ulong ShiftRightValue = 2UL;
    private const ulong BinaryLeftValue = 5UL;
    private const ulong BinaryRightValue = 4UL;
    private const ulong NotValue = 5UL;

    [TestMethod]
    [DynamicData(nameof(ShiftCases))]
    public void Shift_Cases_ReturnExpected(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        Assert.AreEqual(expected, method.Invoke(Library, arguments), name);
    }

    public static IEnumerable<object?[]> ShiftCases()
    {
        foreach (var operationName in new[] { "ShiftLeft", "ShiftRight" })
        {
            foreach (var method in GetOperationMethods(operationName, 2))
            {
                var value = operationName == "ShiftLeft" ? ShiftLeftValue : ShiftRightValue;
                var typedValue = CreateTypedValue(value, method.GetParameters()[0].ParameterType);

                yield return Case(
                    $"{FormatMethodName(method)}_Valid_ReturnsExpected",
                    method,
                    [typedValue, ShiftAmount],
                    CreateShiftExpected(operationName, value, method.ReturnType));

                yield return Case(
                    $"{FormatMethodName(method)}_Null_ReturnsNull",
                    method,
                    [null, ShiftAmount],
                    null);
            }
        }
    }

    [TestMethod]
    [DynamicData(nameof(NotCases))]
    public void Not_Cases_ReturnExpected(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        Assert.AreEqual(expected, method.Invoke(Library, arguments), name);
    }

    public static IEnumerable<object?[]> NotCases()
    {
        foreach (var method in GetOperationMethods("Not", 1))
        {
            yield return Case(
                $"{FormatMethodName(method)}_Valid_ReturnsExpected",
                method,
                [CreateTypedValue(NotValue, method.GetParameters()[0].ParameterType)],
                CreateNotExpected(method.ReturnType));

            yield return Case(
                $"{FormatMethodName(method)}_Null_ReturnsNull",
                method,
                [null],
                null);
        }
    }

    [TestMethod]
    [DynamicData(nameof(BinaryOperationCases))]
    public void BinaryOperation_Cases_ReturnExpected(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        Assert.AreEqual(expected, method.Invoke(Library, arguments), name);
    }

    public static IEnumerable<object?[]> BinaryOperationCases()
    {
        foreach (var operationName in new[] { "And", "Or", "Xor" })
        {
            foreach (var method in GetOperationMethods(operationName, 2))
            {
                var parameters = method.GetParameters();
                var left = CreateTypedValue(BinaryLeftValue, parameters[0].ParameterType);
                var right = CreateTypedValue(BinaryRightValue, parameters[1].ParameterType);

                yield return Case(
                    $"{FormatMethodName(method)}_Valid_ReturnsExpected",
                    method,
                    [left, right],
                    CreateBinaryExpected(operationName, method.ReturnType));

                yield return Case(
                    $"{FormatMethodName(method)}_LeftNull_ReturnsNull",
                    method,
                    [null, right],
                    null);

                yield return Case(
                    $"{FormatMethodName(method)}_RightNull_ReturnsNull",
                    method,
                    [left, null],
                    null);
            }
        }
    }

    private static IEnumerable<MethodInfo> GetOperationMethods(string operationName, int parameterCount)
    {
        return typeof(LibraryBase)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.DeclaringType == typeof(LibraryBase))
            .Where(method => method.Name == operationName)
            .Where(method => method.GetParameters().Length == parameterCount)
            .OrderBy(FormatMethodName, StringComparer.Ordinal);
    }

    private static object CreateShiftExpected(string operationName, ulong value, Type returnType)
    {
        var result = operationName switch
        {
            "ShiftLeft" => value << ShiftAmount,
            "ShiftRight" => value >> ShiftAmount,
            _ => throw new InvalidOperationException($"Unsupported shift operation: {operationName}.")
        };

        return CreateTypedValue(result, returnType);
    }

    private static object CreateBinaryExpected(string operationName, Type returnType)
    {
        var result = operationName switch
        {
            "And" => BinaryLeftValue & BinaryRightValue,
            "Or" => BinaryLeftValue | BinaryRightValue,
            "Xor" => BinaryLeftValue ^ BinaryRightValue,
            _ => throw new InvalidOperationException($"Unsupported binary operation: {operationName}.")
        };

        return CreateTypedValue(result, returnType);
    }

    private static object CreateNotExpected(Type returnType)
    {
        var underlyingType = GetUnderlyingType(returnType);
        if (underlyingType == typeof(byte))
            return unchecked((byte)~(byte)NotValue);
        if (underlyingType == typeof(short))
            return (short)~(short)NotValue;
        if (underlyingType == typeof(int))
            return ~(int)NotValue;
        if (underlyingType == typeof(long))
            return ~(long)NotValue;
        if (underlyingType == typeof(sbyte))
            return (sbyte)~(sbyte)NotValue;
        if (underlyingType == typeof(ushort))
            return unchecked((ushort)~(ushort)NotValue);
        if (underlyingType == typeof(uint))
            return ~(uint)NotValue;
        if (underlyingType == typeof(ulong))
            return ~NotValue;

        throw new InvalidOperationException($"Unsupported bitwise NOT return type: {returnType}.");
    }

    private static object CreateTypedValue(ulong value, Type type)
    {
        return Convert.ChangeType(value, GetUnderlyingType(type), CultureInfo.InvariantCulture);
    }

    private static Type GetUnderlyingType(Type type)
    {
        return Nullable.GetUnderlyingType(type) ?? type;
    }

    private static string FormatMethodName(MethodInfo method)
    {
        var parameterTypes = method.GetParameters()
            .Select(static parameter => GetUnderlyingType(parameter.ParameterType).Name);

        return $"{method.Name}_{string.Join('_', parameterTypes)}";
    }

    private static object?[] Case(string name, MethodInfo method, object?[] arguments, object? expected)
    {
        return [name, method, arguments, expected];
    }
}