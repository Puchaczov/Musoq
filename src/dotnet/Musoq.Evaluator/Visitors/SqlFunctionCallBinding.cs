using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema.Helpers;

namespace Musoq.Evaluator.Visitors;

internal static class SqlFunctionCallBinding
{
    public static MethodInfo RequireCallableDeclaration(AccessMethodNode node, MethodInfo method)
    {
        if (node.IsCompilerGenerated)
            return method;

        var declarations = SqlFunctionDeclarations.Read(method);
        var sqlName = MethodNameNormalizer.Normalize(node.Name);
        if (!declarations.IsEmpty && !declarations.Any(function =>
                function.Visibility == FunctionVisibility.Public && function.Kind != SqlFunctionKind.Window &&
                (MethodNameNormalizer.Normalize(function.Name) == sqlName ||
                 node.IsDistinct && function.Kind == SqlFunctionKind.Aggregate &&
                 MethodNameNormalizer.Normalize(function.Name) == sqlName + "distinct")))
            throw new CannotResolveMethodException(
                $"Function '{node.Name}' is an implementation operation and cannot be called from user SQL.",
                node.SpanOrEmpty());

        return method;
    }
}
