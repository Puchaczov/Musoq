using System.Diagnostics.CodeAnalysis;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed class SubqueryAliasReferenceVisitor : NoOpExpressionVisitor
{
    private SubqueryCorrelationAnalyzer? _analyzer;

    public void Bind(SubqueryCorrelationAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public override void Visit(AccessColumnNode node)
    {
        RecordAliasReference(node.Alias);
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        RecordAliasReference(node.TableAlias);
    }

    public override void Visit(AccessCallChainNode node)
    {
        RecordAliasReference(node.Alias);
    }

    public override void Visit(DotNode node)
    {
        if (TryGetRootAlias(node, out var alias))
            RecordAliasReference(alias);
    }

    public override void Visit(AccessMethodFromNode node)
    {
        RecordAliasReference(node.SourceAlias);
    }

    public override void Visit(PropertyFromNode node)
    {
        RecordAliasReference(node.SourceAlias);
    }

    private void RecordAliasReference(string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        RequireAnalyzer().RecordAliasReference(alias);
    }

    private SubqueryCorrelationAnalyzer RequireAnalyzer()
    {
        return _analyzer ?? throw new InvalidOperationException("Subquery alias reference visitor must be bound before use.");
    }

    private static bool TryGetRootAlias(DotNode node, [NotNullWhen(true)] out string? alias)
    {
        Node root = node;

        while (root is DotNode dotNode)
            root = dotNode.Root;

        switch (root)
        {
            case AccessColumnNode accessColumn:
                alias = accessColumn.Alias;
                return !string.IsNullOrWhiteSpace(alias);

            case IdentifierNode identifier:
                alias = identifier.Name;
                return !string.IsNullOrWhiteSpace(alias);

            default:
                alias = null;
                return false;
        }
    }
}
