using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeQuantifiedSubquery(Node left, TokenType operatorType)
    {
        var quantifier = Current.TokenType;
        Consume(quantifier);
        Consume(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.Select && Current.TokenType != TokenType.From)
            throw new SyntaxException(
                $"{quantifier.ToString().ToUpperInvariant()} requires a SELECT or FROM subquery.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                Current.Span);

        var subquery = ComposeSetOperators(1);
        Consume(TokenType.RightParenthesis);

        if (quantifier is TokenType.Any or TokenType.Some && operatorType == TokenType.Equality)
            return new InQueryNode(left, subquery);

        if (subquery is not QueryNode query)
            throw new SyntaxException(
                "Quantified subqueries over set operators are not supported yet.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                subquery.HasSpan ? subquery.Span : default);

        if (query.Select.Fields.Length != 1)
            throw new SyntaxException(
                "Quantified subquery must return exactly one column.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                query.Select.Span);

        var value = query.Select.Fields[0].Expression;
        Node predicate = quantifier is TokenType.Any or TokenType.Some
            ? CreateAnyQuantifiedPredicate(left, operatorType, value)
            : CreateAllQuantifiedPredicate(left, operatorType, value);

        var where = query.Where != null
            ? new WhereNode(new AndNode(query.Where.Expression, predicate))
            : new WhereNode(predicate);
        var existsQuery = new QueryNode(
            new SelectNode([new FieldNode(new IntegerNode(1), 0, "_quantified_key")]),
            query.From,
            where,
            query.GroupBy,
            query.OrderBy,
            query.Skip,
            query.Take,
            query.Window,
            query.Qualify,
            default);

        var exists = new ExistsQueryNode(existsQuery);
        return quantifier is TokenType.All ? new NotNode(exists) : exists;
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

    private static bool IsQuantifiedSubqueryToken(TokenType tokenType)
    {
        return tokenType is TokenType.Any or TokenType.Some or TokenType.All;
    }
}
