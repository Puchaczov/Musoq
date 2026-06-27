using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;
using EvaluatorParser = Musoq.Evaluator.Parser;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal sealed class DerivedTableCorrelationAnalyzer : RawTraverseVisitor<DerivedTableCorrelationVisitor>
{
    private readonly Stack<IReadOnlySet<string>> _localScopes = new();
    private readonly HashSet<string> _localAliases = CreateAliasSet();
    private readonly HashSet<string> _correlatedAliases = CreateAliasSet();
    private readonly IReadOnlySet<string> _visibleOuterAliases;

    private DerivedTableCorrelationAnalyzer(IReadOnlySet<string> visibleOuterAliases)
        : base(new DerivedTableCorrelationVisitor())
    {
        _visibleOuterAliases = visibleOuterAliases;
        Visitor.Bind(this);
    }

    public static SubqueryCorrelationInfo Analyze(Node body, IReadOnlySet<string> visibleOuterAliases)
    {
        if (visibleOuterAliases.Count == 0)
        {
            var emptyAliases = CreateAliasSet();
            return new SubqueryCorrelationInfo(
                body,
                emptyAliases,
                emptyAliases,
                emptyAliases,
                emptyAliases,
                SubqueryCorrelationFactBuilder.Build(body, emptyAliases, emptyAliases, emptyAliases),
                false);
        }

        var analyzer = new DerivedTableCorrelationAnalyzer(visibleOuterAliases);
        body.Accept(analyzer);
        return analyzer.Build(body);
    }

    public override void Visit(QueryNode node)
    {
        var aliases = CollectFromAliases(node.From);
        foreach (var alias in aliases)
            _localAliases.Add(alias);

        _localScopes.Push(aliases);
        try
        {
            base.Visit(node);
        }
        finally
        {
            _localScopes.Pop();
        }
    }

    public void RecordAliasReference(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            return;

        if (_localScopes.Any(scope => scope.Contains(alias)))
            return;

        if (_visibleOuterAliases.Contains(alias))
            _correlatedAliases.Add(alias);
    }

    private SubqueryCorrelationInfo Build(Node node)
    {
        var localAliases = CreateAliasSet(_localAliases);
        var outerAliases = CreateAliasSet(_visibleOuterAliases);
        var correlatedAliases = CreateAliasSet(_correlatedAliases);

        return new SubqueryCorrelationInfo(
            node,
            localAliases,
            outerAliases,
            correlatedAliases,
            CreateAliasSet(),
            SubqueryCorrelationFactBuilder.Build(node, localAliases, outerAliases, correlatedAliases),
            false);
    }

    private static HashSet<string> CollectFromAliases(FromNode from)
    {
        var aliases = CreateAliasSet();
        CollectFromAliases(from, aliases);
        return aliases;
    }

    private static void CollectFromAliases(FromNode from, HashSet<string> aliases)
    {
        switch (from)
        {
            case null:
                return;
            case ExpressionFromNode expressionFrom:
                CollectFromAliases(expressionFrom.Expression, aliases);
                return;
            case JoinNode joinNode:
                CollectFromAliases(joinNode.Join, aliases);
                return;
            case ApplyNode applyNode:
                CollectFromAliases(applyNode.Apply, aliases);
                return;
            case EvaluatorParser.JoinFromNode join:
                CollectFromAliases(join.Source, aliases);
                CollectFromAliases(join.With, aliases);
                return;
            case EvaluatorParser.ApplyFromNode apply:
                CollectFromAliases(apply.Source, aliases);
                CollectFromAliases(apply.With, aliases);
                return;
            default:
                if (!string.IsNullOrWhiteSpace(from.Alias))
                    aliases.Add(from.Alias);
                return;
        }
    }

    private static HashSet<string> CreateAliasSet()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> CreateAliasSet(IEnumerable<string> aliases)
    {
        return new HashSet<string>(aliases.Where(alias => !string.IsNullOrWhiteSpace(alias)), StringComparer.OrdinalIgnoreCase);
    }
}
