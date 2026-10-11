using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class ScalarGenericDimensionReduction
{
    internal static bool TryReduce(MethodInfo method, ArgsListNode args, [NotNullWhen(true)] out MethodInfo? reducedMethod)
    {
        var parameters = method.GetParameters();
        var paramsParameter = parameters
            .FirstOrDefault(f => global::Musoq.Plugins.VariadicParameter.IsVariadic(f));

        if (paramsParameter is null || method.GetGenericArguments().Length != 1)
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

        try
        {
            reducedMethod = method.MakeGenericMethod(lastNonNullType);
            return true;
        }
        catch (ArgumentException)
        {
            reducedMethod = null;
            return false;
        }
    }

}
