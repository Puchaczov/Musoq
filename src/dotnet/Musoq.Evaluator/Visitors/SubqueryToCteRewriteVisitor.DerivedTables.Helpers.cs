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

    private static EqualityNode CreateAlwaysTruePredicate()
    {
        return new EqualityNode(new IntegerNode(1), new IntegerNode(1));
    }

    private static void ThrowImplicitLateralDerivedTable(
        DerivedTableFromNode derived,
        SubqueryCorrelationInfo correlation)
    {
        ThrowUnsupportedDerivedCorrelation(derived,
            $"Plain derived tables are not lateral. Use CROSS APPLY or OUTER APPLY for references to outer alias '{correlation.CorrelatedAliases.First()}'.");
    }

    [DoesNotReturn]
    private static void ThrowUnsupportedDerivedCorrelation(DerivedTableFromNode derived, string message)
    {
        throw CreateUnsupportedDerivedCorrelation(derived, message);
    }

    private static Exceptions.VisitorException CreateUnsupportedDerivedCorrelation(DerivedTableFromNode derived, string message)
    {
        return SubqueryDiagnosticFactory.InvalidSubquery(
            "derived table rewrite",
            message,
            derived);
    }

    private sealed record DerivedTableRewriteResult(
        FromNode From,
        bool WasDerivedTable,
        Node? JoinPredicate);

    private sealed record DerivedCorrelationRewrite(
        Node Body,
        Node? JoinPredicate);
}
