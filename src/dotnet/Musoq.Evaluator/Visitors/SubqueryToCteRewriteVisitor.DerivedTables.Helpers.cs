using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static SubqueryCorrelationInfo AnalyzeDerivedTableCorrelation(
        Node body,
        IReadOnlySet<string> visibleOuterAliases)
    {
        return DerivedTableCorrelationAnalyzer.Analyze(body, visibleOuterAliases);
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
            case Parser.JoinFromNode join:
                CollectFromAliases(join.Source, aliases);
                CollectFromAliases(join.With, aliases);
                return;
            case Parser.ApplyFromNode apply:
                CollectFromAliases(apply.Source, aliases);
                CollectFromAliases(apply.With, aliases);
                return;
            default:
                if (!string.IsNullOrWhiteSpace(from.Alias))
                    aliases.Add(from.Alias);
                return;
        }
    }

    private static HashSet<string> MergeAliases(
        IReadOnlySet<string> first,
        IReadOnlySet<string> second)
    {
        var aliases = CreateAliasSet(first);
        foreach (var alias in second)
            aliases.Add(alias);
        return aliases;
    }

    private static HashSet<string> CreateAliasSet()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> CreateAliasSet(IEnumerable<string> aliases)
    {
        return new HashSet<string>(aliases.Where(alias => !string.IsNullOrWhiteSpace(alias)), StringComparer.OrdinalIgnoreCase);
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedDerivedCorrelation(
        DerivedTableFromNode derived,
        string message,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        throw CreateUnsupportedDerivedCorrelation(derived, message, arguments);
    }

    private static Exceptions.VisitorException CreateUnsupportedDerivedCorrelation(
        DerivedTableFromNode derived,
        string message,
        IReadOnlyDictionary<string, string>? arguments = null)
    {
        return SubqueryDiagnosticFactory.InvalidSubquery(
            "derived table rewrite",
            message,
            derived,
            arguments);
    }

    private sealed record DerivedTableRewriteResult(
        FromNode From,
        bool WasDerivedTable,
        Node? JoinPredicate);

    private sealed record DerivedCorrelationRewrite(Node Body, Node? JoinPredicate, string[] CorrelationKey);

    private sealed record DerivedCorrelationQueryRewrite(QueryNode Query, Node? JoinPredicate, string[] CorrelationKey);
}
