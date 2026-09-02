using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static (string Alias, string Name) ExtractPath(Node node)
    {
        return node switch
        {
            AccessColumnNode column => NormalizeAccessColumn(column),
            PropertyValueNode property => (string.Empty, property.Name),
            IdentifierNode identifier => (string.Empty, identifier.Name),
            WordNode word => (string.Empty, word.Value),
            DotNode dot => MergePath(ExtractPath(dot.Root), ComposePathSegment(ExtractPath(dot.Expression))),
            _ => throw new UnsupportedIrShapeException(
                $"Cannot extract dotted path from AST node of type '{node.GetType().Name}'.")
        };
    }

    private static (string Alias, string Name) ExtractPathWithIndexers(Node node)
    {
        return node switch
        {
            AccessColumnNode column => NormalizeAccessColumn(column),
            PropertyValueNode property => (string.Empty, property.Name),
            AccessObjectArrayNode arrayAccess => (
                arrayAccess.IsColumnAccess ? arrayAccess.TableAlias ?? string.Empty : string.Empty,
                $"{arrayAccess.Name}[{arrayAccess.Token.Index}]"),
            AccessObjectKeyNode keyAccess => (string.Empty, $"{keyAccess.Name}['{keyAccess.Token.Key}']"),
            IdentifierNode identifier => (string.Empty, identifier.Name),
            WordNode word => (string.Empty, word.Value),
            DotNode dot => MergePath(
                ExtractPathWithIndexers(dot.Root),
                ComposePathSegment(ExtractPathWithIndexers(dot.Expression))),
            _ => throw new UnsupportedIrShapeException(
                $"Cannot extract dotted path from AST node of type '{node.GetType().Name}'.")
        };
    }

    private static bool ContainsIndexerNode(Node node)
    {
        return node switch
        {
            AccessObjectArrayNode => true,
            AccessObjectKeyNode => true,
            DotNode dot => ContainsIndexerNode(dot.Root) || ContainsIndexerNode(dot.Expression),
            _ => false
        };
    }

    private static string ComposePathSegment((string Alias, string Name) path)
    {
        if (string.IsNullOrWhiteSpace(path.Alias))
            return path.Name;

        if (string.IsNullOrWhiteSpace(path.Name))
            return path.Alias;

        return $"{path.Alias}.{path.Name}";
    }

    private static (string Alias, string Name) NormalizeAccessColumn(AccessColumnNode column)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
            return (column.Alias, column.Name);

        return SplitLeadingAlias(column.Name, column.Alias);
    }

    private static (string Alias, string Name) SplitLeadingAlias(string name, string alias)
    {
        var dotIndex = name.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0 || dotIndex >= name.Length - 1)
            return (alias, name);

        return (name[..dotIndex], name[(dotIndex + 1)..]);
    }

    private static (string Alias, string Name) MergePath(
        (string Alias, string Name) left,
        string rightSegment)
    {
        if (string.IsNullOrWhiteSpace(rightSegment))
            return left;

        if (string.IsNullOrWhiteSpace(left.Name))
            return (left.Alias, rightSegment);

        return (left.Alias, $"{left.Name}.{rightSegment}");
    }
}
