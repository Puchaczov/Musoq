using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class QuantifiedFilterPredicate
{
    internal static Node? Create(QuantifiedSubqueryNode node, Node left)
    {
        if (node.Subquery is not QueryNode { GroupBy: null, OrderBy: null, Skip: null, Take: null,
                Window: null, Qualify: null, Select.IsDistinct: false } query ||
            query.Select.Fields.Length != 1 || !IsSimple(left) || !IsSimple(query.Select.Fields[0].Expression))
            return null;
        if (node.Quantifier is TokenType.Any or TokenType.Some && node.Comparison == TokenType.Equality)
            return new InQueryNode(left, query).CopySpansFrom(node);
        var right = QualifyQuantifiedColumn(query.Select.Fields[0].Expression, GetSingleQuantifiedSourceAlias(query.From));
        Node predicate;
        if (node.Quantifier == TokenType.All)
            predicate = CreateAllQuantifiedPredicate(left, node.Comparison, right);
        else
            predicate = CreateAnyQuantifiedPredicate(left, node.Comparison, right);
        var where = query.Where is null ? predicate : new AndNode(query.Where.Expression, predicate);
        var body = new QueryNode(new SelectNode([new FieldNode(new IntegerNode(1), 0, "_quantified_key")]),
            query.From, new WhereNode(where), null, null, null, null);
        var exists = new ExistsQueryNode(body);
        if (node.Quantifier == TokenType.All)
            return new NotNode(exists).CopySpansFrom(node);
        return exists.CopySpansFrom(node);
    }

    private static bool IsSimple(Node node) => node is AccessColumnNode or IdentifierNode or ConstantValueNode ||
        node is DotNode { Root: IdentifierNode, Expression: IdentifierNode };

    private static Node QualifyQuantifiedColumn(Node node, string? alias)
    {
        if (node is not IdentifierNode identifier || string.IsNullOrEmpty(alias))
            return node;

        return new AccessColumnNode(identifier.Name, alias, identifier.ReturnType ?? typeof(void), identifier.Span)
            .WithFullSpan(identifier.FullSpan);
    }

    private static string? GetSingleQuantifiedSourceAlias(FromNode source)
    {
        return source switch
        {
            ExpressionFromNode expression => GetSingleQuantifiedSourceAlias(expression.Expression),
            JoinNode or JoinFromNode or ApplyNode or ApplyFromNode => null,
            _ => source.Alias
        };
    }

    private static AndNode CreateAnyQuantifiedPredicate(Node left, TokenType operatorType, Node right)
    {
        return new AndNode(
            new AndNode(
                new IsNullNode(CloneQuantifiedOperand(left), true),
                new IsNullNode(CloneQuantifiedOperand(right), true)),
            CreateComparisonPredicate(CloneQuantifiedOperand(left), operatorType, CloneQuantifiedOperand(right)));
    }

    private static OrNode CreateAllQuantifiedPredicate(Node left, TokenType operatorType, Node right)
    {
        return new OrNode(
            new OrNode(
                new IsNullNode(CloneQuantifiedOperand(left), false),
                new IsNullNode(CloneQuantifiedOperand(right), false)),
            CreateComparisonPredicate(
                CloneQuantifiedOperand(left),
                InvertComparisonOperator(operatorType),
                CloneQuantifiedOperand(right)));
    }

    private static Node CloneQuantifiedOperand(Node node)
    {
        return node switch
        {
            AccessColumnNode access => new AccessColumnNode(
                access.Name,
                access.Alias,
                access.ReturnType,
                access.Span,
                access.IntendedTypeName),
            DotNode dot => new DotNode(
                CloneQuantifiedOperand(dot.Root),
                CloneQuantifiedOperand(dot.Expression),
                dot.IsTheMostInner,
                dot.Name,
                dot.ReturnType ?? typeof(void),
                dot.IntendedTypeName),
            IdentifierNode identifier => new IdentifierNode(
                identifier.Name,
                identifier.ReturnType ?? typeof(void),
                identifier.Span),
            _ => node
        };
    }

    private static TokenType InvertComparisonOperator(TokenType operatorType)
    {
        return operatorType switch
        {
            TokenType.GreaterEqual => TokenType.Less,
            TokenType.Greater => TokenType.LessEqual,
            TokenType.LessEqual => TokenType.Greater,
            TokenType.Less => TokenType.GreaterEqual,
            TokenType.Equality => TokenType.Diff,
            TokenType.Diff => TokenType.Equality,
            _ => throw new InvalidOperationException($"{operatorType} is not a quantified comparison operator.")
        };
    }

    private static Node CreateComparisonPredicate(Node left, TokenType operatorType, Node right)
    {
        return operatorType switch
        {
            TokenType.GreaterEqual => new GreaterOrEqualNode(left, right),
            TokenType.Greater => new GreaterNode(left, right),
            TokenType.LessEqual => new LessOrEqualNode(left, right),
            TokenType.Less => new LessNode(left, right),
            TokenType.Equality => new EqualityNode(left, right),
            TokenType.Diff => new DiffNode(left, right),
            _ => throw new InvalidOperationException($"{operatorType} is not a comparison operator.")
        };
    }

}
