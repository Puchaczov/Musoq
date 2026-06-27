using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private static Node NormalizeSubqueryLeftSide(Node leftSide)
    {
        return leftSide is IdentifierNode identifierNode
            ? new AccessColumnNode(identifierNode.Name, string.Empty, default)
            : leftSide;
    }

    private static Node UnwrapCteSubqueryBody(Node subquery, List<CteInnerExpressionNode> cteInnerExpressions)
    {
        if (subquery is not CteExpressionNode cteExpression)
            return subquery;

        cteInnerExpressions.AddRange(cteExpression.InnerExpression);
        return cteExpression.OuterExpression;
    }

    private static FromNode UnwrapFromForJoin(FromNode from)
    {
        if (from is ExpressionFromNode exprFrom)
        {
            if (exprFrom.Expression is JoinNode joinNode)
                return joinNode.Join;

            return exprFrom.Expression;
        }

        return from;
    }

    private static FromNode WrapJoinedFrom(FromNode from)
    {
        return from switch
        {
            Parser.ExpressionFromNode => from,
            Parser.JoinFromNode joinFrom => new Parser.ExpressionFromNode(new Parser.JoinNode(joinFrom)),
            Parser.ApplyFromNode applyFrom => new Parser.ExpressionFromNode(new Parser.ApplyNode(applyFrom)),
            _ => new Parser.ExpressionFromNode(from)
        };
    }

    private static QueryNode AddDistinct(QueryNode subquery)
    {
        if (subquery.Select.IsDistinct)
            return subquery;

        if (subquery.GroupBy != null)
            return subquery;

        if (subquery.Select.Fields.Any(f => f.Expression is AccessMethodNode))
            return subquery;

        var groupByFields = new FieldNode[subquery.Select.Fields.Length];
        for (var i = 0; i < subquery.Select.Fields.Length; i++)
            groupByFields[i] = new FieldNode(subquery.Select.Fields[i].Expression, i, string.Empty);

        return new QueryNode(
            subquery.Select,
            subquery.From,
            subquery.Where,
            new GroupByNode(groupByFields, null),
            subquery.OrderBy,
            subquery.Skip,
            subquery.Take,
            subquery.Window,
            subquery.Qualify,
            default);
    }

    private Node ProjectExistsKey(
        Node node,
        string cteColumnName,
        List<CteInnerExpressionNode> cteInnerExpressions)
    {
        switch (node)
        {
            case QueryNode query:
            {
                var (rewrittenQuery, innerCtes) = RewriteNestedInSubqueries(query);
                cteInnerExpressions.AddRange(innerCtes);
                return ProjectExistsKey(rewrittenQuery, cteColumnName);
            }

            case SingleSetNode singleSet:
                return new SingleSetNode(ProjectExistsKey(singleSet.Query, cteColumnName));

            case SetOperatorNode setOperator:
                return RecreateSetOperator(
                    setOperator,
                    ProjectExistsKey(setOperator.Left, cteColumnName, cteInnerExpressions),
                    ProjectExistsKey(setOperator.Right, cteColumnName, cteInnerExpressions));

            default:
                return node;
        }
    }

    private static QueryNode ProjectExistsKey(QueryNode query, string cteColumnName)
    {
        return new QueryNode(
            new SelectNode(
            [
                new FieldNode(new IntegerNode(1), 0, cteColumnName)
            ]),
            query.From,
            query.Where,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);
    }

    private static SetOperatorNode RecreateSetOperator(SetOperatorNode node, Node left, Node right)
    {
        return node switch
        {
            UnionNode => new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            UnionAllNode => new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            ExceptNode => new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            IntersectNode => new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne),
            _ => throw SubqueryDiagnosticFactory.InvalidSubquery(
                "EXISTS subquery projection",
                $"Unsupported set operator {node.GetType().Name} in EXISTS subquery.",
                node)
        };
    }

    private static bool IsCardinalitySafeFallbackCorrelation(SubqueryInfo info, Node expression)
    {
        return info.Correlation?.Facts is
               {
                   HasEqualityKeys: true,
                   NullSemantics: SubqueryCorrelationNullSemantics.EqualityComparison
               } &&
               IsEqualityOnlyFallbackCorrelation(expression);
    }

    private static bool IsEqualityOnlyFallbackCorrelation(Node expression)
    {
        return expression switch
        {
            EqualityNode => true,
            AndNode and => IsEqualityOnlyFallbackCorrelation(and.Left) &&
                           IsEqualityOnlyFallbackCorrelation(and.Right),
            _ => false
        };
    }

    private static void ThrowUnsupportedPredicateFallbackCorrelation(SubqueryInfo info)
    {
        var predicateName = info.IsExists ? "EXISTS" : "IN";
        throw SubqueryDiagnosticFactory.InvalidSubquery(
            $"fallback {predicateName} subquery rewrite",
            $"Predicate {predicateName} subqueries used inside expression fallback contexts currently require equality-only correlation predicates. Non-equality correlated predicates require APPLY fallback lowering.",
            info.PredicateNode);
    }
}
