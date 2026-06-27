using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Plugins.Attributes;

namespace Musoq.Plugins;

/// <summary>
///     Describes the static typed kernel behind an aggregate declaration method.
/// </summary>
/// <param name="FunctionName">SQL-facing aggregate function name.</param>
/// <param name="DeclarationMethod">Metadata declaration method exposed by a library.</param>
/// <param name="KernelType">Static kernel type.</param>
/// <param name="StateType">Typed aggregate state type.</param>
/// <param name="SetMethod">Static state mutation method.</param>
/// <param name="GetMethod">Static state result method.</param>
/// <param name="MergeMethod">Optional static state merge method.</param>
/// <param name="InputShape">Exact runtime input shape passed to Set.</param>
/// <param name="ResultType">Declared public aggregate result type.</param>
/// <param name="UnderlyingResultType">Underlying result type with nullable wrappers removed.</param>
/// <param name="ResultDescriptor">Public result shape and empty-input behavior.</param>
/// <param name="ParentParameterIndex">Declaration method parent-depth parameter index, when present.</param>
/// <param name="Inline">Whether generated code may inline this kernel.</param>
public sealed record AggregateKernelDescriptor(
    string FunctionName,
    MethodInfo DeclarationMethod,
    Type KernelType,
    Type StateType,
    MethodInfo SetMethod,
    MethodInfo GetMethod,
    MethodInfo? MergeMethod,
    AggregateInputShape InputShape,
    Type ResultType,
    Type UnderlyingResultType,
    AggregateResultDescriptor ResultDescriptor,
    int? ParentParameterIndex,
    bool Inline)
{
    private const string StateTypeName = "State";
    private const string SetMethodName = "Set";
    private const string GetMethodName = "Get";
    private const string MergeMethodName = "Merge";

    /// <summary>
    ///     Gets whether the kernel can merge independent state instances.
    /// </summary>
    public bool SupportsMerge => MergeMethod is not null;

    /// <summary>
    ///     Creates and validates a descriptor for an aggregate declaration method.
    /// </summary>
    /// <param name="declarationMethod">Aggregate declaration method marked with <see cref="AggregateFunctionAttribute"/>.</param>
    /// <returns>A validated aggregate kernel descriptor.</returns>
    public static AggregateKernelDescriptor Create(MethodInfo declarationMethod)
    {
        ArgumentNullException.ThrowIfNull(declarationMethod);
        var attribute = declarationMethod.GetCustomAttribute<AggregateFunctionAttribute>();
        if (attribute is null)
            throw CreateInvalidKernelException(declarationMethod, "Aggregate declaration is missing AggregateFunctionAttribute.");

        var stateType = ResolveStateType(attribute, declarationMethod);
        var valueParameters = GetValueParameters(declarationMethod);
        var parentParameterIndex = ValidateParentParameter(declarationMethod);
        var inputShape = AggregateInputShape.Tuple(valueParameters.Select(static parameter => parameter.ParameterType).ToArray());
        var setMethod = ResolveSetMethod(attribute.KernelType, stateType, valueParameters, declarationMethod);
        var getMethod = ResolveGetMethod(attribute.KernelType, stateType, declarationMethod);
        var mergeMethod = ResolveMergeMethod(attribute.KernelType, stateType, declarationMethod);
        var functionName = string.IsNullOrWhiteSpace(attribute.Name)
            ? declarationMethod.Name
            : attribute.Name!;
        var resultType = declarationMethod.ReturnType;

        return new AggregateKernelDescriptor(
            functionName,
            declarationMethod,
            attribute.KernelType,
            stateType,
            setMethod,
            getMethod,
            mergeMethod,
            inputShape,
            resultType,
            Nullable.GetUnderlyingType(resultType) ?? resultType,
            new AggregateResultDescriptor(
                resultType,
                Nullable.GetUnderlyingType(resultType) ?? resultType,
                attribute.EmptyResultBehavior),
            parentParameterIndex,
            attribute.Inline);
    }

    private static Type ResolveStateType(
        AggregateFunctionAttribute attribute,
        MethodInfo declarationMethod)
    {
        if (attribute.StateType is not null)
            return attribute.StateType;

        var stateType = attribute.KernelType.GetNestedType(
            StateTypeName,
            BindingFlags.Public | BindingFlags.NonPublic);
        if (stateType is not null)
            return CloseNestedStateType(attribute.KernelType, stateType);

        throw CreateInvalidKernelException(
            declarationMethod,
            $"Aggregate kernel {attribute.KernelType.FullName} must expose a nested State type or set AggregateFunctionAttribute.StateType.");
    }

    private static Type CloseNestedStateType(
        Type kernelType,
        Type stateType)
    {
        if (!stateType.ContainsGenericParameters || !kernelType.IsConstructedGenericType)
            return stateType;

        var stateGenericArguments = stateType.GetGenericArguments();
        var kernelGenericArguments = kernelType.GetGenericArguments();
        return stateGenericArguments.Length == kernelGenericArguments.Length
            ? stateType.MakeGenericType(kernelGenericArguments)
            : stateType;
    }

    private static ParameterInfo[] GetValueParameters(MethodInfo declarationMethod)
    {
        return declarationMethod
            .GetParameters()
            .Where(static parameter => parameter.GetCustomAttribute<AggregateParentAttribute>() is null)
            .ToArray();
    }

    private static int? ValidateParentParameter(MethodInfo declarationMethod)
    {
        int? parentIndex = null;
        var parameters = declarationMethod.GetParameters();

        for (var index = 0; index < parameters.Length; index++)
        {
            var parameter = parameters[index];
            if (parameter.GetCustomAttribute<AggregateParentAttribute>() is null)
                continue;

            if (parentIndex is not null)
                throw CreateInvalidKernelException(declarationMethod, "Aggregate declarations can have only one AggregateParent parameter.");

            if (parameter.ParameterType != typeof(int))
                throw CreateInvalidKernelException(declarationMethod, "AggregateParent parameter must be an int compile-time depth.");

            if (parameter is { HasDefaultValue: true, DefaultValue: int and < 0 })
            {
                throw CreateInvalidKernelException(declarationMethod, "AggregateParent default depth cannot be negative.");
            }

            parentIndex = index;
        }

        return parentIndex;
    }

    private static MethodInfo ResolveSetMethod(
        Type kernelType,
        Type stateType,
        IReadOnlyList<ParameterInfo> valueParameters,
        MethodInfo declarationMethod)
    {
        var setMethod = EnumerateStaticMethods(kernelType, SetMethodName)
            .SingleOrDefault(method => IsMatchingSetMethod(method, stateType, valueParameters));
        if (setMethod is not null)
            return setMethod;

        throw CreateInvalidKernelException(
            declarationMethod,
            $"Aggregate kernel {kernelType.FullName} must expose Set(ref State, args...) matching declaration argument types.");
    }

    private static MethodInfo ResolveGetMethod(
        Type kernelType,
        Type stateType,
        MethodInfo declarationMethod)
    {
        var getMethod = EnumerateStaticMethods(kernelType, GetMethodName)
            .SingleOrDefault(method => IsMatchingGetMethod(method, stateType, declarationMethod.ReturnType));
        if (getMethod is not null)
            return getMethod;

        throw CreateInvalidKernelException(
            declarationMethod,
            $"Aggregate kernel {kernelType.FullName} must expose Get(in State) returning {declarationMethod.ReturnType.FullName}.");
    }

    private static MethodInfo? ResolveMergeMethod(
        Type kernelType,
        Type stateType,
        MethodInfo declarationMethod)
    {
        var mergeMethods = EnumerateStaticMethods(kernelType, MergeMethodName)
            .Where(method => IsMatchingMergeMethod(method, stateType))
            .ToArray();

        return mergeMethods.Length switch
        {
            0 => null,
            1 => mergeMethods[0],
            _ => throw CreateInvalidKernelException(
                declarationMethod,
                $"Aggregate kernel {kernelType.FullName} exposes multiple matching Merge(ref State, in State) methods.")
        };
    }

    private static IEnumerable<MethodInfo> EnumerateStaticMethods(
        Type type,
        string name)
    {
        return type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => string.Equals(method.Name, name, StringComparison.Ordinal));
    }

    private static bool IsMatchingSetMethod(
        MethodInfo method,
        Type stateType,
        IReadOnlyList<ParameterInfo> valueParameters)
    {
        var parameters = method.GetParameters();
        if (method.ReturnType != typeof(void) ||
            parameters.Length != valueParameters.Count + 1 ||
            !IsByRefStateParameter(parameters[0], stateType))
        {
            return false;
        }

        for (var index = 0; index < valueParameters.Count; index++)
        {
            if (parameters[index + 1].ParameterType != valueParameters[index].ParameterType)
                return false;
        }

        return true;
    }

    private static bool IsMatchingGetMethod(
        MethodInfo method,
        Type stateType,
        Type resultType)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == resultType &&
               parameters.Length == 1 &&
               IsByRefStateParameter(parameters[0], stateType);
    }

    private static bool IsMatchingMergeMethod(
        MethodInfo method,
        Type stateType)
    {
        var parameters = method.GetParameters();
        return method.ReturnType == typeof(void) &&
               parameters.Length == 2 &&
               IsByRefStateParameter(parameters[0], stateType) &&
               IsByRefStateParameter(parameters[1], stateType);
    }

    private static bool IsByRefStateParameter(
        ParameterInfo parameter,
        Type stateType)
    {
        return parameter.ParameterType.IsByRef &&
               parameter.ParameterType.GetElementType() == stateType;
    }

    private static InvalidOperationException CreateInvalidKernelException(
        MethodInfo declarationMethod,
        string reason)
    {
        return new InvalidOperationException(
            $"Aggregate declaration {declarationMethod.DeclaringType?.FullName}.{declarationMethod.Name} is invalid. {reason}");
    }
}
