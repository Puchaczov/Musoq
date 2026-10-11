using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;
using Musoq.Schema.Helpers;

namespace Musoq.Schema.Managers;

public partial class MethodsMetadata
{
    private const int ObjectErasureConversionCost = 1000;
    private const int GenericExactShapeConversionCost = 1;
    private const int GenericNullableLiftConversionCost = 2;

    private int MeasureMethodCloseness(MethodInfo registeredMethod, IReadOnlyList<Type> methodArgs)
    {
        var metadata = GetCachedParameterMetadata(registeredMethod);
        var parameters = metadata.Parameters;
        var parametersToInject = metadata.ParametersToInject;
        var notAnnotatedParametersCount = metadata.NotAnnotatedParametersCount;
        var howClosePassedTypesAre = 0;

        if (notAnnotatedParametersCount < methodArgs.Count)
            return int.MaxValue;

        for (var i = 0; i < methodArgs.Count; i++)
        {
            var rawParam = parameters[i + parametersToInject].ParameterType;
            var rawArg = methodArgs[i];

            if (IsNullType(rawArg))
            {
                if (!CanSafelyPassNull(rawParam, rawArg))
                    return int.MaxValue;

                howClosePassedTypesAre += GetNullArgumentConversionCost(rawParam);
                continue;
            }

            var param = rawParam.GetUnderlyingNullable();
            var arg = rawArg.GetUnderlyingNullable();

            if (param == arg)
                howClosePassedTypesAre += 0;
            else if (param.IsAssignableFrom(arg))
                howClosePassedTypesAre += GetAssignableConversionCost(arg, param);
            else if (CanImplicitlyConvert(arg, param))
                howClosePassedTypesAre += GetNumericConversionCost(arg, param);
            else if (!param.IsGenericParameter && IsGenericShapeCompatible(param, arg))
                howClosePassedTypesAre += GenericExactShapeConversionCost;
            else if (param.IsGenericParameter && TypeConformsToConstraints(param, arg))
            {
                var nullableParameter = Nullable.GetUnderlyingType(rawParam) != null;
                var nullableArgument = Nullable.GetUnderlyingType(rawArg) != null;
                howClosePassedTypesAre += nullableParameter == nullableArgument
                    ? GenericExactShapeConversionCost
                    : GenericNullableLiftConversionCost;
            }
            else
                return int.MaxValue;
        }

        return howClosePassedTypesAre;
    }

    private static int GetNullArgumentConversionCost(Type parameterType)
    {
        if (parameterType.IsGenericType && parameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            return 0;

        if (parameterType.IsGenericParameter)
            return 50;

        return parameterType.IsValueType ? int.MaxValue : 100;
    }

    private static int GetMethodResolutionPriority(MethodInfo method)
    {
        if (method.GetCustomAttribute<AggregateFunctionAttribute>() != null)
            return 0;

        return method.GetParameters().Any(static parameter => global::Musoq.Plugins.VariadicParameter.IsNativeSpan(parameter.ParameterType)) ? 5 : 10;
    }

    private static int GetAssignableConversionCost(Type argument, Type parameter)
    {
        if (parameter == typeof(object))
            return ObjectErasureConversionCost;
        return GetInheritanceDepth(argument, parameter);
    }

    private static int GetInheritanceDepth(Type derived, Type target)
    {
        if (derived == target) return 0;

        var depth = 0;
        var current = derived;

        while (current != null && current != target)
        {
            depth++;
            current = current.BaseType;
        }

        return current == null ? -1 : depth;
    }

    private static bool IsParameterTypeCompatible(Type rawParam, Type param, Type arg)
    {
        return IsGenericShapeCompatible(param, arg) || IsTypePossibleToConvert(param, arg) ||
               CanSafelyPassNull(rawParam, arg) ||
               (param.IsGenericParameter && TypeConformsToConstraints(param, arg)) ||
               ((arg.IsArray || arg.GetInterface("IEnumerable") != null) && param is { IsGenericType: true, Name: "IEnumerable`1" }) ||
               (param.IsGenericType && arg.IsGenericType && param.Name == "IEnumerable`1" &&
                arg.Name == "IEnumerable`1") ||
               (param.IsArray && IsGenericArrayElement(param) && arg.IsArray) ||
               (arg.IsArray && IsGenericArrayElement(arg));
    }

    private static bool IsGenericShapeCompatible(Type template, Type actual)
    {
        if (!template.ContainsGenericParameters)
            return false;
        if (template.IsGenericParameter)
            return TypeConformsToConstraints(template, actual);
        if (template.IsArray)
            return actual.IsArray && template.GetArrayRank() == actual.GetArrayRank() &&
                   IsGenericShapeCompatible(template.GetElementType()!, actual.GetElementType()!);
        if (!template.IsGenericType)
            return false;
        var definition = template.GetGenericTypeDefinition();
        var candidate = actual.IsGenericType && actual.GetGenericTypeDefinition() == definition
            ? actual : actual.GetInterfaces().FirstOrDefault(type => type.IsGenericType &&
                type.GetGenericTypeDefinition() == definition);
        if (candidate is null)
            return false;
        var parameters = template.GetGenericArguments();
        var values = candidate.GetGenericArguments();
        for (var index = 0; index < parameters.Length; index++)
            if (parameters[index].ContainsGenericParameters
                ? !IsGenericShapeCompatible(parameters[index], values[index])
                : parameters[index] != values[index])
                return false;
        return true;
    }

    private static bool CanUseSomeArgumentsAsDefaultParameters(IReadOnlyCollection<Type> methodArgs,
        int parametersCount, int optionalParametersCount)
    {
        return methodArgs.Count >= parametersCount - optionalParametersCount && methodArgs.Count <= parametersCount;
    }

    private static bool IsGenericArrayElement(Type arrayType)
    {
        return arrayType.GetElementType() is { IsGenericParameter: true };
    }

    private static bool HasMoreArgumentsThanMethodDefinitionContains(IReadOnlyList<Type> methodArgs,
        int parametersCount)
    {
        return methodArgs.Count > parametersCount;
    }
    private static bool CanBeAssignedFromGeneric(Type paramType, Type arrayType)
    {
        var isParamArray = paramType.IsArray;

        if (!isParamArray)
            return false;

        var paramElementType = paramType.GetElementType()!;
        var isParamGeneric = paramElementType.IsGenericParameter || paramElementType.IsArray;
        var isArrayArray = arrayType.IsArray;

        return isParamGeneric && isArrayArray;
    }

    private bool IsMethodCompatibleWithEntityType(MethodInfo methodInfo, Type entityType)
    {
        var injectTypeAttributes = GetInjectTypeAttribute(methodInfo);
        var injectTypeAttribute = injectTypeAttributes.SingleOrDefault(
            f => f is InjectSpecificSourceAttribute,
            injectTypeAttributes.FirstOrDefault());

        if (injectTypeAttribute is null or InjectQueryStatsAttribute)
            return true;

        return IsEntityTypeInjectableIntoMethod(entityType, injectTypeAttribute);
    }

    private static bool IsEntityTypeInjectableIntoMethod(Type entityType, InjectTypeAttribute injectTypeAttribute)
    {
        return entityType.IsAssignableTo(injectTypeAttribute.InjectType);
    }

    private InjectTypeAttribute[] GetInjectTypeAttribute(MethodInfo methodInfo)
    {
        return GetCachedParameterMetadata(methodInfo).InjectTypeAttributes;
    }
}
