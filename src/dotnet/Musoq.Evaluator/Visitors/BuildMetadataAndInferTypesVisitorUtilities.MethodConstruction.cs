using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Utility methods extracted from BuildMetadataAndInferTypesVisitor to improve maintainability and testability.
/// </summary>
public static partial class BuildMetadataAndInferTypesVisitorUtilities
{
    internal static bool TryReduceDimensions(MethodInfo method, ArgsListNode args, [NotNullWhen(true)] out MethodInfo? reducedMethod)
    {
        var parameters = method.GetParameters();
        var paramsParameter = parameters
            .FirstOrDefault(f => f.GetCustomAttribute<ParamArrayAttribute>() != null);

        if (paramsParameter is null)
        {
            reducedMethod = null;
            return false;
        }

        var paramsParameterIndex = paramsParameter.Position;
        var typesToReduce = args.Args.Skip(paramsParameterIndex).Select(f => f.ReturnType).ToArray();

        var nonNullTypes = typesToReduce
            .OfType<Type>()
            .Where(t => t is not NullNode.NullType)
            .ToArray();

        Type? typeToReduce;
        if (nonNullTypes.Length > 1)
            typeToReduce = nonNullTypes.First().MakeArrayType();
        else if (nonNullTypes.Length == 1)
            typeToReduce = nonNullTypes.First();
        else
            typeToReduce = typeof(object);

        var lastNonNullType = typeToReduce;
        while (typeToReduce is not null)
        {
            lastNonNullType = typeToReduce;
            typeToReduce = typeToReduce.GetElementType();
        }

        reducedMethod = method.MakeGenericMethod(lastNonNullType);
        return true;
    }

    internal static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, Type entity,
        [NotNullWhen(true)] out MethodInfo? constructedMethod)
    {
        var genericArguments = methodInfo.GetGenericArguments();
        var genericArgumentsDistinct = new List<Type>();
        var parameters = methodInfo.GetParameters();

        foreach (var genericArgument in genericArguments)
        {
            var i = 0;
            var shiftArgsWhenInjectSpecificSourcePresent = 0;

            if (parameters[0].GetCustomAttribute<InjectSpecificSourceAttribute>() != null)
            {
                i = 1;
                shiftArgsWhenInjectSpecificSourcePresent = 1;
                if ((genericArgument.IsGenericParameter || genericArgument.IsGenericMethodParameter) &&
                    parameters[0].ParameterType.IsGenericParameter) genericArgumentsDistinct.Add(entity);
            }

            for (; i < parameters.Length; i++)
            {
                var parameter = parameters[i];

                if (parameter.IsOptional &&
                    args.Args.Length < parameters.Length - shiftArgsWhenInjectSpecificSourcePresent) continue;

                var returnType = args.Args.Where((_, index) => index == i - shiftArgsWhenInjectSpecificSourcePresent)
                    .Single().ReturnType ?? typeof(object);
                var elementType = returnType.GetElementType();

                if (returnType.IsGenericType && parameter.ParameterType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == parameter.ParameterType.GetGenericTypeDefinition())
                {
                    genericArgumentsDistinct.Add(returnType.GetGenericArguments()[0]);
                    continue;
                }

                if (parameter.ParameterType.IsGenericType &&
                    parameter.ParameterType.IsAssignableTo(typeof(IEnumerable<>).MakeGenericType(genericArgument)) &&
                    elementType is not null)
                {
                    genericArgumentsDistinct.Add(elementType);
                    continue;
                }

                if (parameter.ParameterType.IsGenericType)
                {
                    var assignableInterfaces = returnType
                        .GetInterfaces()
                        .Where(type => type.IsConstructedGenericType)
                        .Select(type => new { type, definition = type.GetGenericTypeDefinition() })
                        .ToArray();

                    var firstAssignableInterface =
                        assignableInterfaces.FirstOrDefault(f => f.definition.IsAssignableFrom(typeof(IEnumerable<>)));

                    if (firstAssignableInterface is null) continue;

                    var elementTypeOfFirstAssignableInterface = firstAssignableInterface.type.GetElementType() ??
                                                                firstAssignableInterface.type.GetGenericArguments()[0];

                    genericArgumentsDistinct.Add(elementTypeOfFirstAssignableInterface);
                }

                if (parameter.ParameterType == genericArgument) genericArgumentsDistinct.Add(returnType);
            }
        }

        var hasNullType = genericArgumentsDistinct.Any(t => t is NullNode.NullType);

        var genericArgumentsConcreteTypes = genericArgumentsDistinct
            .Where(t => t is not NullNode.NullType)
            .Distinct()
            .ToArray();

        if (genericArgumentsConcreteTypes.Length == 0)
            genericArgumentsConcreteTypes = [typeof(object)];
        else if (hasNullType)
        {
            for (var i = 0; i < genericArgumentsConcreteTypes.Length; i++)
            {
                if (genericArgumentsConcreteTypes[i].IsValueType &&
                    Nullable.GetUnderlyingType(genericArgumentsConcreteTypes[i]) == null)
                {
                    genericArgumentsConcreteTypes[i] =
                        typeof(Nullable<>).MakeGenericType(genericArgumentsConcreteTypes[i]);
                }
            }
        }

        constructedMethod = methodInfo.MakeGenericMethod(genericArgumentsConcreteTypes);
        return true;
    }
}
