using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeQuantifiedSubquery(Node left, TokenType operatorType)
    {
        using var sourceRegion = _sourceCapture?.Begin(Syntax.SyntaxKind.Subquery, Current);
        var outerAlias = GetSingleQuantifiedOuterAlias();
        var quantifierToken = Current;
        var quantifier = quantifierToken.TokenType;
        Consume(quantifier);
        var openingParenthesis = ConsumeAndGetToken(TokenType.LeftParenthesis);

        if (Current.TokenType != TokenType.Select && Current.TokenType != TokenType.From)
            throw new SyntaxException(
                $"{quantifier.ToString().ToUpperInvariant()} requires a SELECT or FROM subquery.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2024_InvalidSubquery,
                Current.Span);

        var subquery = ComposeSetOperators(1);
        var closingParenthesis = ConsumeAndGetToken(TokenType.RightParenthesis);
        var quantifiedSpan = left.Span
            .Through(quantifierToken.Span)
            .Through(openingParenthesis.Span)
            .Through(subquery.Span)
            .Through(closingParenthesis.Span);

        var firstQuery = subquery;
        while (firstQuery is SetOperatorNode set)
            firstQuery = set.Left;
        if (firstQuery is QueryNode { Select.Fields.Length: not 1 } wide)
            throw new SyntaxException("Quantified subquery must return exactly one column.",
                _lexer.AlreadyResolvedQueryPart, DiagnosticCode.MQ2024_InvalidSubquery, wide.Select.Span);

        return new QuantifiedSubqueryNode(QualifyQuantifiedColumn(left, outerAlias), operatorType,
            quantifier, subquery).WithSpan(quantifiedSpan);
    }

    private static Node QualifyQuantifiedColumn(Node node, string? alias)
    {
        if (node is not IdentifierNode identifier || string.IsNullOrEmpty(alias))
            return node;

        return new AccessColumnNode(identifier.Name, alias, identifier.ReturnType ?? typeof(void), identifier.Span)
            .WithFullSpan(identifier.FullSpan);
    }

    private string? GetSingleQuantifiedOuterAlias()
    {
        if (_fromAliasesStack.Count == 0 || _fromAliasesStack.Peek().Count != 1)
            return null;

        return _fromAliasesStack.Peek().Single();
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

    private static bool IsQuantifiedSubqueryToken(TokenType tokenType) =>
        tokenType is TokenType.Any or TokenType.Some or TokenType.All;
}
