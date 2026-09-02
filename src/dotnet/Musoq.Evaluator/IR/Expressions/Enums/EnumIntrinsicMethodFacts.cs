using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Musoq.Evaluator.IR.Expressions;

internal static class EnumIntrinsicMethodFacts
{
    private static readonly IReadOnlyDictionary<EnumIntrinsicKind, MethodInfo> NonNullableMethods =
        CreateMethods("NonNullable");

    private static readonly IReadOnlyDictionary<EnumIntrinsicKind, MethodInfo> NullableMethods =
        CreateMethods("Nullable");

    internal static MethodInfo Bind(EnumIntrinsicKind kind, Type operandType)
    {
        var nullableCarrier = Nullable.GetUnderlyingType(operandType);
        var carrierType = nullableCarrier ?? operandType;
        if (!carrierType.IsPrimitive || carrierType == typeof(bool) || carrierType == typeof(char))
            throw new InvalidOperationException(
                $"Enum intrinsic '{kind}' requires an integral carrier, received '{operandType}'.");

        var definitions = nullableCarrier == null ? NonNullableMethods : NullableMethods;
        return definitions[kind].MakeGenericMethod(carrierType);
    }

    internal static bool TryGetKind(MethodInfo method, out EnumIntrinsicKind kind)
    {
        if (method.DeclaringType != typeof(EnumIntrinsicMarkers))
        {
            kind = default;
            return false;
        }

        var name = method.Name;
        foreach (var candidate in Enum.GetValues<EnumIntrinsicKind>())
        {
            if (!name.StartsWith(candidate.ToString(), StringComparison.Ordinal))
                continue;

            kind = candidate;
            return true;
        }

        kind = default;
        return false;
    }

    private static IReadOnlyDictionary<EnumIntrinsicKind, MethodInfo> CreateMethods(string suffix)
    {
        var methods = typeof(EnumIntrinsicMarkers).GetMethods(BindingFlags.Public | BindingFlags.Static);
        var result = new Dictionary<EnumIntrinsicKind, MethodInfo>();
        foreach (var kind in Enum.GetValues<EnumIntrinsicKind>())
            result.Add(
                kind,
                methods.Single(method => string.Equals(
                    method.Name,
                    $"{kind}{suffix}",
                    StringComparison.Ordinal)));
        return result;
    }
}

internal static class EnumIntrinsicMarkers
{
    public static T EnumValueNonNullable<T>(T value) where T : struct => value;

    public static T? EnumValueNullable<T>(T? value) where T : struct => value;

    public static string? EnumNameNonNullable<T>(T value) where T : struct =>
        throw UnexpectedExecution();

    public static string? EnumNameNullable<T>(T? value) where T : struct =>
        throw UnexpectedExecution();

    public static bool IsDefinedNonNullable<T>(T value) where T : struct =>
        throw UnexpectedExecution();

    public static bool IsDefinedNullable<T>(T? value) where T : struct =>
        throw UnexpectedExecution();

    public static bool HasAnyFlagsNonNullable<T>(T value, T mask) where T : struct =>
        throw UnexpectedExecution();

    public static bool HasAnyFlagsNullable<T>(T? value, T mask) where T : struct =>
        throw UnexpectedExecution();

    public static bool HasAllFlagsNonNullable<T>(T value, T mask) where T : struct =>
        throw UnexpectedExecution();

    public static bool HasAllFlagsNullable<T>(T? value, T mask) where T : struct =>
        throw UnexpectedExecution();

    private static InvalidOperationException UnexpectedExecution() =>
        new("An enum compiler-intrinsic marker reached runtime execution without lowering.");
}
