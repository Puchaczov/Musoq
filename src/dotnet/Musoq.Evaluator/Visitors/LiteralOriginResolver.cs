using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class LiteralOriginResolver
{
    private readonly Dictionary<string, Node> _declarations = new(StringComparer.Ordinal);
    private readonly SourceText? _sourceText;

    public LiteralOriginResolver(RootNode query, SourceText? sourceText)
    {
        _sourceText = sourceText;
        CollectDeclarations(query);
    }

    public bool TryResolve(Node node, out LiteralOrigin origin)
    {
        return TryResolve(node, new HashSet<string>(StringComparer.Ordinal), out origin);
    }

    private bool TryResolve(Node node, HashSet<string> resolving, out LiteralOrigin origin)
    {
        if (TryCreateLiteral(node, out origin))
            return true;

        var referenceName = node switch
        {
            ScriptVariableReferenceNode reference => reference.Name,
            ParameterReferenceNode reference => reference.Name,
            _ => null
        };
        if (referenceName is null ||
            !_declarations.TryGetValue(referenceName, out var declaration) ||
            !resolving.Add(referenceName))
        {
            origin = null!;
            return false;
        }

        var resolved = TryResolve(declaration, resolving, out origin);
        resolving.Remove(referenceName);
        return resolved;
    }

    private bool TryCreateLiteral(Node node, out LiteralOrigin origin)
    {
        if (node is not WordNode and not StringNode ||
            _sourceText is not { } sourceText ||
            !node.HasSpan ||
            node.Span.Start < 0 ||
            node.Span.End > sourceText.Text.Length)
        {
            origin = null!;
            return false;
        }

        var source = sourceText.Text.Substring(node.Span.Start, node.Span.Length);
        if (source.Length >= 2 && source[0] == '\'' && source[^1] == '\'')
        {
            origin = new LiteralOrigin(node, GetValue(node), node.Span, source,
                node.Span.Start + 1, source.Length - 2, false);
            return true;
        }

        if (source.Length >= 3 && (source[0] == 'r' || source[0] == 'R') &&
            source[1] == '\'' && source[^1] == '\'')
        {
            origin = new LiteralOrigin(node, GetValue(node), node.Span, source,
                node.Span.Start + 2, source.Length - 3, true);
            return true;
        }

        origin = null!;
        return false;
    }

    private void CollectDeclarations(Node node)
    {
        if (node is ScriptVariableDeclarationNode declaration)
            _declarations[declaration.Name] = declaration.Initializer;

        foreach (var child in ParserNodeTraversalRegistry.EnumerateChildren(node))
            CollectDeclarations(child);
    }

    private static string GetValue(Node node) => node switch
    {
        WordNode word => word.Value,
        StringNode stringNode => stringNode.Value,
        _ => string.Empty
    };
}
