using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.Helpers.Subqueries.SubqueryCorrelationUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private void ApplyNotInNullGuards(
        PredicateSubqueryJoin match,
        ref FromNode currentFrom,
        ref Node? remaining,
        List<CteInnerExpressionNode> ctes
    )
    {
        var nulls = PrepareMembershipPresence(match, ctes, nullMembersOnly: true);
        currentFrom = new Parser.JoinFromNode(currentFrom, nulls.CteRef, nulls.JoinExpression, JoinType.LeftAntiSemi);

        var rows = PrepareMembershipPresence(match, ctes, nullMembersOnly: false);
        currentFrom = new Parser.JoinFromNode(currentFrom, rows.CteRef, rows.JoinExpression, JoinType.LeftMark);
        var guard = new OrNode(new IsNullNode(match.Membership!.LeftSide, true), new NotNode(rows.Replacement));
        remaining = remaining is null ? guard : new AndNode(remaining, guard);
    }

    private Node PreparePredicateValue(
        SubqueryInfo info,
        PredicateSubqueryJoin match,
        List<CteInnerExpressionNode> ctes,
        Action<PredicateSubqueryJoin> addMark
    )
    {
        if (match.Membership is not { } membership)
            return match.Replacement;

        var rows = PrepareMembershipPresence(match, ctes, nullMembersOnly: false);
        var nulls = PrepareMembershipPresence(match, ctes, nullMembersOnly: true);
        addMark(rows);
        addMark(nulls);

        var matched = new IsNullNode(new AccessColumnNode(membership.CteColumnName, match.CteRef.Alias, default), true);
        var unknown = new OrNode(new IsNullNode(membership.LeftSide, false), nulls.Replacement);
        return new CaseNode(
            [
                (new WhenNode(matched), new ThenNode(new BooleanNode(!info.IsNegated))),
                (new WhenNode(new AndNode(rows.Replacement, unknown)), new ThenNode(new NullNode())),
            ],
            new ElseNode(new BooleanNode(info.IsNegated))
        );
    }

    // Probe the completed set, so null detection and emptiness respect DISTINCT, aggregation and paging.
    private PredicateSubqueryJoin PrepareMembershipPresence(
        PredicateSubqueryJoin match,
        List<CteInnerExpressionNode> ctes,
        bool nullMembersOnly
    )
    {
        var membership = match.Membership!;
        var source = match.CteRef.Alias;
        var name = CreateUniqueSubqueryName();
        var marker = GeneratedSubqueryContract.CreateKeyColumnName(name);
        var fields = new List<FieldNode> { new(new IntegerNode(1), 0, marker) };
        var markerRef = new AccessColumnNode(marker, name, default);
        Node probe = new EqualityNode(new IntegerNode(1), markerRef);
        if (membership.CorrelationJoinExpression is { } correlation)
        {
            var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { source };
            var projections = CollectCorrelationProjections([correlation], aliases, name);
            foreach (var column in projections)
                fields.Add(
                    new FieldNode(
                        new AccessColumnNode(column.ColumnName, source, column.Span),
                        fields.Count,
                        column.CteColumnName
                    )
                );
            probe = new AndNode(probe, RewriteCorrelatedPredicatesForJoin([correlation], projections, aliases, name));
        }

        var value = new AccessColumnNode(membership.CteColumnName, source, default);
        var query = new QueryNode(
            new SelectNode(fields.ToArray()),
            new Parser.ExpressionFromNode(new Parser.InMemoryTableFromNode(source, source)),
            nullMembersOnly ? new WhereNode(new IsNullNode(value, false)) : null,
            null,
            null,
            null,
            null
        );
        ctes.Add(new CteInnerExpressionNode(query, name));
        return new PredicateSubqueryJoin(
            new Parser.InMemoryTableFromNode(name, name),
            probe,
            new IsNullNode(markerRef, true)
        );
    }
}
