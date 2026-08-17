using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class PredicateConstantResolver
{
    private readonly IReadOnlyDictionary<string, Node> _declarations;

    public PredicateConstantResolver(RootNode query)
    {
        var declarations = new Dictionary<string, Node>(StringComparer.Ordinal);
        Collect(query, declarations);
        _declarations = declarations;
    }

    public Node? Resolve(Node node)
    {
        return Resolve(node, new HashSet<string>(StringComparer.Ordinal));
    }

    private Node? Resolve(Node node, HashSet<string> resolving)
    {
        if (node is NullNode or ConstantValueNode)
            return node;

        var name = node switch
        {
            ParameterReferenceNode parameter => parameter.Name,
            ScriptVariableReferenceNode variable => variable.Name,
            _ => null
        };
        if (name is null || !_declarations.TryGetValue(name, out var initializer) || !resolving.Add(name))
            return null;

        var resolved = Resolve(initializer, resolving);
        resolving.Remove(name);
        return resolved;
    }

    private static void Collect(Node node, Dictionary<string, Node> declarations)
    {
        if (node is ScriptVariableDeclarationNode declaration)
            declarations[declaration.Name] = declaration.Initializer;

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            Collect(child, declarations);
    }
}
