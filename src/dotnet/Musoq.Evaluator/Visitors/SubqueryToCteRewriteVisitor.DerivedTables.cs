using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private FromNode RewriteDerivedTablesToCtes(
        FromNode from,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        return RewriteDerivedTablesToCtes(from, cteInnerExpressions, CreateAliasSet(), false).From;
    }

    private DerivedTableRewriteResult RewriteDerivedTablesToCtes(
        FromNode from,
        List<CteInnerExpressionNode> cteInnerExpressions,
        IReadOnlySet<string> visibleOuterAliases,
        bool allowCorrelation)
    {
        switch (from)
        {
            case null:
                throw new InvalidOperationException("Derived table rewrite requires a FROM node.");

            case DerivedTableFromNode derived:
                return RewriteDerivedTable(derived, cteInnerExpressions, visibleOuterAliases, allowCorrelation);

            case ExpressionFromNode expressionFrom:
            {
                var rewritten = RewriteDerivedTablesToCtes(
                    expressionFrom.Expression,
                    cteInnerExpressions,
                    visibleOuterAliases,
                    allowCorrelation);
                return rewritten with { From = new Parser.ExpressionFromNode(rewritten.From) };
            }

            case JoinNode joinNode:
            {
                var rewritten = RewriteDerivedTablesToCtes(
                    joinNode.Join,
                    cteInnerExpressions,
                    visibleOuterAliases,
                    false);
                return rewritten with { From = new Parser.JoinNode((Parser.JoinFromNode)rewritten.From) };
            }

            case ApplyNode applyNode:
            {
                var rewritten = RewriteDerivedTablesToCtes(
                    applyNode.Apply,
                    cteInnerExpressions,
                    visibleOuterAliases,
                    true);
                return rewritten.From switch
                {
                    Parser.ApplyFromNode apply => rewritten with { From = new Parser.ApplyNode(apply) },
                    Parser.JoinFromNode join => rewritten with { From = new Parser.JoinNode(join) },
                    _ => rewritten
                };
            }

            case Parser.JoinFromNode join:
            {
                var source = RewriteDerivedTablesToCtes(
                    join.Source,
                    cteInnerExpressions,
                    visibleOuterAliases,
                    false);
                var rightVisibleAliases = MergeAliases(visibleOuterAliases, CollectFromAliases(source.From));
                var with = RewriteDerivedTablesToCtes(
                    join.With,
                    cteInnerExpressions,
                    rightVisibleAliases,
                    false);
                return new DerivedTableRewriteResult(new Parser.JoinFromNode(source.From, with.From, join.Expression, join.JoinType, join.TieBreak, join.WithOrdinality), false, null);
            }

            case Parser.ApplyFromNode apply:
            {
                var source = RewriteDerivedTablesToCtes(
                    apply.Source,
                    cteInnerExpressions,
                    visibleOuterAliases,
                    false);
                var rightVisibleAliases = MergeAliases(visibleOuterAliases, CollectFromAliases(source.From));
                var with = RewriteDerivedTablesToCtes(
                    apply.With,
                    cteInnerExpressions,
                    rightVisibleAliases,
                    true);

                if (!with.WasDerivedTable)
                    return new DerivedTableRewriteResult(
                        new Parser.ApplyFromNode(source.From, with.From, apply.ApplyType, apply.WithOrdinality),
                        false,
                        null);

                return new DerivedTableRewriteResult(new Parser.JoinFromNode(source.From, with.From, with.JoinPredicate ?? CreateAlwaysTruePredicate(), apply.ApplyType == ApplyType.Cross ? JoinType.Inner : JoinType.OuterLeft, withOrdinality: apply.WithOrdinality), false, null);
            }

            default:
                return new DerivedTableRewriteResult(from, false, null);
        }
    }

    private DerivedTableRewriteResult RewriteDerivedTable(
        DerivedTableFromNode derived,
        List<CteInnerExpressionNode> cteInnerExpressions,
        IReadOnlySet<string> visibleOuterAliases,
        bool allowCorrelation)
    {
        var cteName = CreateUniqueDerivedTableName();
        var body = UnwrapCteSubqueryBody(derived.Query, cteInnerExpressions);
        var correlation = AnalyzeDerivedTableCorrelation(body, visibleOuterAliases);
        Node? joinPredicate = null;

        if (correlation.IsCorrelated)
        {
            if (!allowCorrelation)
                ThrowImplicitLateralDerivedTable(derived, correlation);

            var rewritten = RewriteCorrelatedDerivedBody(body, correlation, derived.Alias, derived);
            body = rewritten.Body;
            joinPredicate = rewritten.JoinPredicate;
        }

        cteInnerExpressions.Add(new CteInnerExpressionNode(body, cteName));
        return new DerivedTableRewriteResult(
            new Parser.InMemoryTableFromNode(cteName, derived.Alias),
            true,
            joinPredicate);
    }

}
