using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Schema.Managers;

internal enum OverloadPreference
{
    Equivalent,
    Left,
    Right,
    Incomparable
}

internal sealed record CallableSignaturePreference(
    IReadOnlyList<Type> ParameterTypes,
    bool IsVariadic,
    bool IsGeneric,
    int OmittedDefaults);

internal static class CallableOverloadPreference
{
    internal static OverloadPreference Compare(
        IReadOnlyList<Type> arguments,
        CallableSignaturePreference left,
        CallableSignaturePreference right)
    {
        var leftBetter = false;
        var rightBetter = false;
        for (var index = 0; index < arguments.Count; index++)
        {
            var preference = CompareConversion(arguments[index], left.ParameterTypes[index], right.ParameterTypes[index]);
            if (preference == OverloadPreference.Incomparable)
                return preference;
            leftBetter |= preference == OverloadPreference.Left;
            rightBetter |= preference == OverloadPreference.Right;
        }

        if (leftBetter && rightBetter)
            return OverloadPreference.Incomparable;
        if (leftBetter)
            return OverloadPreference.Left;
        if (rightBetter)
            return OverloadPreference.Right;
        if (left.IsVariadic != right.IsVariadic)
            return left.IsVariadic ? OverloadPreference.Right : OverloadPreference.Left;
        if (left.IsGeneric != right.IsGeneric)
            return left.IsGeneric ? OverloadPreference.Right : OverloadPreference.Left;
        if (left.OmittedDefaults != right.OmittedDefaults)
            return left.OmittedDefaults < right.OmittedDefaults ? OverloadPreference.Left : OverloadPreference.Right;
        return OverloadPreference.Equivalent;
    }

    internal static OverloadPreference CompareConversion(Type argument, Type left, Type right)
    {
        left = CloseTemplate(left, argument);
        right = CloseTemplate(right, argument);
        if (left == right)
            return OverloadPreference.Equivalent;

        if (!IsNullType(argument))
        {
            var leftRank = GetConversionRank(argument, left);
            var rightRank = GetConversionRank(argument, right);
            if (leftRank != rightRank)
                return leftRank < rightRank ? OverloadPreference.Left : OverloadPreference.Right;
        }

        var leftToRight = SchemaConversionClassifier.TryGetCost(left, right, out _);
        var rightToLeft = SchemaConversionClassifier.TryGetCost(right, left, out _);
        if (leftToRight && !rightToLeft)
            return OverloadPreference.Left;
        if (rightToLeft && !leftToRight)
            return OverloadPreference.Right;
        return OverloadPreference.Incomparable;
    }

    internal static Type? FindCommonNumericType(Type left, Type right)
    {
        Type[] numericTypes = [typeof(char), typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
            typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)];
        if (!numericTypes.Contains(left) || !numericTypes.Contains(right))
            return null;

        var candidates = numericTypes.Where(type =>
            SchemaConversionClassifier.TryGetCost(left, type, out _) &&
            SchemaConversionClassifier.TryGetCost(right, type, out _)).ToArray();
        var best = candidates.Where(candidate => !candidates.Any(other => other != candidate &&
            SchemaConversionClassifier.TryGetCost(other, candidate, out _) &&
            !SchemaConversionClassifier.TryGetCost(candidate, other, out _))).ToArray();
        return best.Length == 1 ? best[0] : null;
    }

    private static int GetConversionRank(Type argument, Type target)
    {
        if (argument == target)
            return 0;
        if (Nullable.GetUnderlyingType(target) == argument)
            return 1;
        var sourceValue = Nullable.GetUnderlyingType(argument) ?? argument;
        var targetValue = Nullable.GetUnderlyingType(target) ?? target;
        if (MethodsMetadata.ValidImplicitConversions.TryGetValue(sourceValue, out var targets) && targets.Contains(targetValue))
            return 2;
        return 3;
    }

    private static Type CloseTemplate(Type template, Type argument)
    {
        if (!template.ContainsGenericParameters)
            return template;
        if (template.IsGenericParameter)
            return IsNullType(argument) ? typeof(object) : argument;
        if (template.IsArray && argument.IsArray)
            return CloseTemplate(template.GetElementType()!, argument.GetElementType()!).MakeArrayType();
        if (!template.IsGenericType)
            return template;

        var definition = template.GetGenericTypeDefinition();
        if (definition == typeof(Nullable<>) && argument.IsValueType && Nullable.GetUnderlyingType(argument) is null)
            return typeof(Nullable<>).MakeGenericType(CloseTemplate(template.GetGenericArguments()[0], argument));
        var view = argument.IsGenericType && argument.GetGenericTypeDefinition() == definition
            ? argument
            : argument.GetInterfaces().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == definition);
        if (view is null)
            return template;
        var templateArguments = template.GetGenericArguments();
        var actualArguments = view.GetGenericArguments();
        return definition.MakeGenericType(templateArguments.Select((type, index) => CloseTemplate(type, actualArguments[index])).ToArray());
    }

    private static bool IsNullType(Type type) => type is NullNode.NullType || type == typeof(NullNode.NullType);
}
