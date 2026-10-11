using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Parser;
using Musoq.Evaluator.Exceptions;

namespace Musoq.Evaluator.Visitors;

internal static class AggregateDeclarationSpecialization
{
    public static MethodInfo Bind(MethodInfo declaration, IReadOnlyList<Type?> argumentTypes)
    {
        var bindings = new Dictionary<Type, Type>();
        var parameters = declaration.GetParameters();
        for (var index = 0; index < argumentTypes.Count; index++)
        {
            var argument = argumentTypes[index];
            if (argument is not null && argument != typeof(NullNode.NullType))
                BindParameter(parameters[index].ParameterType, argument, bindings);
        }

        var arguments = declaration.GetGenericArguments().Select(parameter => ResolveArgument(declaration, parameter, bindings)).ToArray();
        return declaration.MakeGenericMethod(arguments);
    }

    private static Type ResolveArgument(MethodInfo declaration, Type parameter, IReadOnlyDictionary<Type, Type> bindings)
    {
        if (bindings.TryGetValue(parameter, out var bound))
            return bound;
        throw new SourcePlanContractException($"Aggregate declaration {declaration.Name} type parameter {parameter.Name} cannot be inferred.", TextSpan.Empty);
    }

    private static void BindParameter(Type parameter, Type argument, Dictionary<Type, Type> bindings)
    {
        if (parameter.IsGenericParameter)
        {
            if (bindings.TryGetValue(parameter, out var previous) && previous != argument)
                throw new SourcePlanContractException($"Aggregate type parameter {parameter.Name} has incompatible concrete arguments.", TextSpan.Empty);
            bindings[parameter] = argument;
            return;
        }

        if (!parameter.IsGenericType)
            return;
        if (parameter.GetGenericTypeDefinition() == typeof(Nullable<>) && Nullable.GetUnderlyingType(argument) is null)
        {
            BindParameter(parameter.GetGenericArguments()[0], argument, bindings);
            return;
        }
        if (!argument.IsGenericType || parameter.GetGenericTypeDefinition() != argument.GetGenericTypeDefinition())
            return;

        var parameters = parameter.GetGenericArguments();
        var arguments = argument.GetGenericArguments();
        for (var index = 0; index < parameters.Length; index++)
            BindParameter(parameters[index], arguments[index], bindings);
    }
}
