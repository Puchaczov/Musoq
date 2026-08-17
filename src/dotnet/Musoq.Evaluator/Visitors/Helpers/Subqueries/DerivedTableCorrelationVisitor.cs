using System.Diagnostics.CodeAnalysis;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed class DerivedTableCorrelationVisitor : NoOpExpressionVisitor
{
    private DerivedTableCorrelationAnalyzer? _analyzer;

    public void Bind(DerivedTableCorrelationAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    public override void Visit(AccessColumnNode node)
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

    private DerivedTableCorrelationAnalyzer RequireAnalyzer()
    {
        return _analyzer ?? throw new InvalidOperationException("Derived table correlation visitor must be bound before use.");
    }

    private static bool TryGetRootAlias(DotNode node, [NotNullWhen(true)] out string? alias)
    {
        Node root = node;
        while (root is DotNode dot)
            root = dot.Root;

        alias = root is IdentifierNode identifier ? identifier.Name : null;
        return !string.IsNullOrWhiteSpace(alias);
    }
}
