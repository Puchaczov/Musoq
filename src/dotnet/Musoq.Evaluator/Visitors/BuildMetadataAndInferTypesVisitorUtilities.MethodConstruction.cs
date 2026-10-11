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
        => ScalarGenericDimensionReduction.TryReduce(method, args, out reducedMethod);

    internal static bool TryConstructGenericMethod(MethodInfo methodInfo, ArgsListNode args, Type entity,
        [NotNullWhen(true)] out MethodInfo? constructedMethod)
    {
        if (args.Args.Any(static argument => argument is RecordLiteralNode) || methodInfo.GetGenericArguments().Length > 1 || methodInfo.GetParameters().Any(static parameter => global::Musoq.Plugins.VariadicParameter.IsNativeSpan(parameter.ParameterType) || parameter.ParameterType.IsGenericType))
            return ScalarGenericMethodConstruction.TryConstruct(methodInfo, args, entity, out constructedMethod);
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
