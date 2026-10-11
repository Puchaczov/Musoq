using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors;

internal static class ScalarGenericMethodConstruction
{
    internal static bool TryConstruct(MethodInfo method, ArgsListNode arguments, Type entity,
        [NotNullWhen(true)] out MethodInfo? constructed)
    {
        var inferred = new Type?[method.GetGenericArguments().Length];
        var argumentIndex = 0;
        foreach (var parameter in method.GetParameters())
        {
            if (parameter.GetCustomAttribute<InjectTypeAttribute>() is { } injection)
            {
                if (injection is InjectSpecificSourceAttribute && !Bind(parameter.ParameterType, entity, inferred))
                {
                    constructed = null;
                    return false;
                }
                continue;
            }
            if (argumentIndex >= arguments.Args.Length)
                break;
            if (global::Musoq.Plugins.VariadicParameter.IsVariadic(parameter))
            {
                var element = global::Musoq.Plugins.VariadicParameter.ElementType(parameter.ParameterType)!;
                while (argumentIndex < arguments.Args.Length)
                {
                    var valueType = ScalarArgumentTypeMetadata.Resolve(arguments.Args[argumentIndex++]);
                    if (valueType is NullNode.NullType)
                        continue;
                    if (valueType == null || !Bind(element, valueType, inferred))
                    {
                        constructed = null;
                        return false;
                    }
                }
                continue;
            }
            var actual = ScalarArgumentTypeMetadata.Resolve(arguments.Args[argumentIndex++]);
            if (actual is NullNode.NullType)
                actual = typeof(DBNull);
            if (actual == null || !Bind(parameter.ParameterType, actual, inferred))
            {
                constructed = null;
                return false;
            }
        }
        constructed = null;
        if (inferred.Any(static type => type == null))
            return false;
        try { constructed = method.MakeGenericMethod(inferred.Select(static type => type!).ToArray()); }
        catch (ArgumentException) { return false; }
        return true;
    }

    private static bool Bind(Type parameter, Type actual, Type?[] inferred)
    {
        if (parameter.IsGenericParameter)
        {
            actual = Musoq.Schema.StructuralInputs.GeneratedRecordConstraintWitness.Resolve(parameter, actual);
            ref var binding = ref inferred[parameter.GenericParameterPosition];
            if (binding != null && binding != actual)
                return false;
            binding = actual;
            return true;
        }
        if (!parameter.ContainsGenericParameters)
            return true;
        if (parameter.IsArray && actual.IsArray)
            return Bind(parameter.GetElementType()!, actual.GetElementType()!, inferred);
        if (!parameter.IsGenericType)
            return false;
        var definition = parameter.GetGenericTypeDefinition();
        var candidate = actual.IsGenericType && actual.GetGenericTypeDefinition() == definition
            ? actual : actual.GetInterfaces().FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == definition);
        if (candidate == null)
            return false;
        var templates = parameter.GetGenericArguments();
        var values = candidate.GetGenericArguments();
        for (var index = 0; index < templates.Length; index++)
            if (!Bind(templates[index], values[index], inferred))
                return false;
        return true;
    }
}
