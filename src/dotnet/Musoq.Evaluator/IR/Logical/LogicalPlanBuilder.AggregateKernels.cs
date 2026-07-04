using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;
using AggregateRefRewriter = Musoq.Evaluator.IR.Expressions.AggregateRefRewriter;
using ColumnRefExtractor = Musoq.Evaluator.IR.Expressions.ColumnRefExtractor;

namespace Musoq.Evaluator.IR.Logical;

public sealed partial class LogicalPlanBuilder
{
    private static AggregateKernelDescriptor? TryCreateAggregateKernelDescriptor(
        MethodInfo method,
        IReadOnlyList<IrExpression> arguments)
    {
        if (method.GetCustomAttribute<AggregateFunctionAttribute>() is not null)
            return AggregateKernelDescriptor.Create(method);

        return method.DeclaringType?
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(candidate =>
                string.Equals(candidate.Name, method.Name, StringComparison.Ordinal) &&
                candidate.GetCustomAttribute<AggregateFunctionAttribute>() is not null)
            .Where(candidate => CanBindAggregateKernelDeclaration(candidate, arguments))
            .Select(AggregateKernelDescriptor.Create)
            .FirstOrDefault();
    }

    private static bool CanBindAggregateKernelDeclaration(
        MethodInfo declaration,
        IReadOnlyList<IrExpression> arguments)
    {
        var parameters = declaration.GetParameters();
        if (arguments.Count > parameters.Length)
            return false;

        for (var index = 0; index < arguments.Count; index++)
        {
            var parameter = parameters[index];
            var argument = arguments[index];
            if (parameter.GetCustomAttribute<AggregateParentAttribute>() is not null)
            {
                if (!TryReadAggregateParentDepth(argument, out _))
                    return false;

                continue;
            }

            if (!IsAssignableToParameter(argument.ReturnType, parameter.ParameterType))
                return false;
        }

        return parameters
            .Skip(arguments.Count)
            .All(parameter =>
                parameter.HasDefaultValue ||
                parameter.GetCustomAttribute<AggregateParentAttribute>() is not null);
    }

    private static int ResolveAggregateParentDepth(
        AggregateKernelDescriptor? kernel,
        IReadOnlyList<IrExpression> arguments)
    {
        switch (kernel?.ParentParameterIndex)
        {
            case null:
                return 0;
        }

        var parentParameterIndex = kernel.ParentParameterIndex.GetValueOrDefault();
        if (parentParameterIndex >= 0 &&
            parentParameterIndex < arguments.Count &&
            TryReadAggregateParentDepth(arguments[parentParameterIndex], out var parentDepth))
        {
            return parentDepth;
        }

        foreach (var argument in arguments.Skip(1))
        {
            if (TryReadAggregateParentDepth(argument, out parentDepth))
                return parentDepth;
        }

        return 0;
    }

    private static bool TryReadAggregateParentDepth(
        IrExpression argument,
        out int parentDepth)
    {
        parentDepth = 0;

        if (argument is not Literal literal)
            return false;

        switch (literal.Value)
        {
            case int intParentDepth:
                parentDepth = Math.Max(intParentDepth, 0);
                return true;
            case long longParentDepth and <= int.MaxValue:
                parentDepth = (int)Math.Max(longParentDepth, 0);
                return true;
            default:
                return false;
        }
    }

    private static string[] GetAggregateIdentifierCandidates(
        MethodCall methodCall,
        AggregateRefreshLookup refreshLookup)
    {
        var literalIdentifier = ExtractLiteralAggregateIdentifier(methodCall);
        var normalizedIdentifier = AggregateRefRewriter.ExtractIdentifier(methodCall);
        if (string.IsNullOrWhiteSpace(literalIdentifier))
            return string.IsNullOrWhiteSpace(normalizedIdentifier) ? [] : [normalizedIdentifier];

        if (string.IsNullOrWhiteSpace(normalizedIdentifier))
            return [literalIdentifier];

        return string.Equals(literalIdentifier, normalizedIdentifier, StringComparison.Ordinal)
            ? [literalIdentifier]
            : ShouldPreferLiteralAggregateIdentifier(literalIdentifier, normalizedIdentifier, refreshLookup)
                ? [literalIdentifier, normalizedIdentifier]
                : [normalizedIdentifier, literalIdentifier];
    }

    private static string? ExtractLiteralAggregateIdentifier(MethodCall methodCall)
    {
        return methodCall.Arguments
            .OfType<Literal>()
            .Select(literal => literal.Value as string)
            .FirstOrDefault(identifier => !string.IsNullOrWhiteSpace(identifier));
    }

    private static bool ShouldPreferLiteralAggregateIdentifier(
        string? literalIdentifier,
        string? normalizedIdentifier,
        AggregateRefreshLookup refreshLookup)
    {
        return !string.IsNullOrWhiteSpace(literalIdentifier) &&
               !string.IsNullOrWhiteSpace(normalizedIdentifier) &&
               refreshLookup.Captures.ContainsKey(literalIdentifier) &&
               refreshLookup.AmbiguousNormalizedIdentifiers.Contains(normalizedIdentifier);
    }

    private static IrExpression[] CreateAggregateGetArguments(
        MethodInfo getMethod,
        IReadOnlyList<IrExpression> methodCallArguments,
        RefreshMethodCapture refresh)
    {
        var getParameters = GetRuntimeParameters(getMethod).ToArray();
        var setParameters = GetRuntimeParameters(refresh.SetMethod).ToArray();
        var setArguments = GetAggregateRuntimeSetArguments(refresh.SetArguments);
        var arguments = new IrExpression[getParameters.Length];
        var usedSetArgumentIndexes = new HashSet<int>();

        for (var index = 0; index < getParameters.Length; index++)
        {
            var setArgumentIndex = Array.FindIndex(
                setParameters,
                parameter => string.Equals(parameter.Name, getParameters[index].Name, StringComparison.OrdinalIgnoreCase));

            var argument = ResolveAggregateGetArgument(
                getParameters[index],
                setArgumentIndex,
                methodCallArguments,
                setArguments,
                usedSetArgumentIndexes,
                index);

            if (argument is not null)
            {
                arguments[index] = argument;
                continue;
            }

            throw new NotSupportedException(
                $"Cannot bind aggregate getter argument '{getParameters[index].Name}' for method {getMethod.Name}.");
        }

        return arguments;
    }

    private static IReadOnlyList<IrExpression> GetAggregateRuntimeSetArguments(
        IReadOnlyList<IrExpression> setArguments)
    {
        return setArguments.Count > 0 && setArguments[0] is Literal { Value: string }
            ? setArguments.Skip(1).ToArray()
            : setArguments;
    }

    private static IrExpression? ResolveAggregateGetArgument(
        ParameterInfo getParameter,
        int setArgumentIndex,
        IReadOnlyList<IrExpression> methodCallArguments,
        IReadOnlyList<IrExpression> setArguments,
        HashSet<int> usedSetArgumentIndexes,
        int fallbackIndex)
    {
        switch (setArgumentIndex)
        {
            case >= 0 when setArgumentIndex < setArguments.Count:
            {
                var setArgument = setArguments[setArgumentIndex];
                if (CanRenderDuringAggregateFinalization(setArgument) &&
                    IsAssignableToParameter(setArgument.ReturnType, getParameter.ParameterType))
                {
                    usedSetArgumentIndexes.Add(setArgumentIndex);
                    return setArgument;
                }

                break;
            }
        }

        var safeArgumentIndex = FindFinalizationSafeSetArgument(getParameter, setArguments, usedSetArgumentIndexes);
        switch (safeArgumentIndex)
        {
            case >= 0:
                usedSetArgumentIndexes.Add(safeArgumentIndex);
                return setArguments[safeArgumentIndex];
        }

        if (fallbackIndex >= methodCallArguments.Count)
            return CreateDefaultArgumentOrNull(getParameter);

        if (fallbackIndex == 0 && methodCallArguments[fallbackIndex] is Literal { Value: string })
            return CreateDefaultArgumentOrNull(getParameter);

        var fallbackArgument = methodCallArguments[fallbackIndex];
        if (!CanRenderDuringAggregateFinalization(fallbackArgument) ||
            !IsAssignableToParameter(fallbackArgument.ReturnType, getParameter.ParameterType))
            return CreateDefaultArgumentOrNull(getParameter);

        return fallbackArgument;
    }

    private static Literal? CreateDefaultArgumentOrNull(ParameterInfo parameter)
    {
        switch (parameter.HasDefaultValue)
        {
            case false:
                return null;
            default:
                return new Literal(parameter.DefaultValue, parameter.ParameterType);
        }
    }

    private static int FindFinalizationSafeSetArgument(
        ParameterInfo parameter,
        IReadOnlyList<IrExpression> setArguments,
        HashSet<int> usedSetArgumentIndexes)
    {
        for (var index = 0; index < setArguments.Count; index++)
        {
            if (usedSetArgumentIndexes.Contains(index))
                continue;

            if (!CanRenderDuringAggregateFinalization(setArguments[index]))
                continue;

            if (!IsAssignableToParameter(setArguments[index].ReturnType, parameter.ParameterType))
                continue;

            return index;
        }

        return -1;
    }

    private static bool CanRenderDuringAggregateFinalization(IrExpression expression)
    {
        return ColumnRefExtractor.Extract(expression).Count == 0;
    }

    private static bool IsAssignableToParameter(Type? argumentType, Type parameterType)
    {
        switch (argumentType)
        {
            case null:
                return false;
        }

        var effectiveParameterType = Nullable.GetUnderlyingType(parameterType) ?? parameterType;
        var effectiveArgumentType = Nullable.GetUnderlyingType(argumentType) ?? argumentType;
        return effectiveParameterType.IsAssignableFrom(effectiveArgumentType);
    }

    private static IEnumerable<ParameterInfo> GetRuntimeParameters(MethodInfo method)
    {
        return method.GetParameters()
            .Where(parameter => parameter.GetCustomAttributes(true).OfType<InjectTypeAttribute>().FirstOrDefault() is null);
    }
}
