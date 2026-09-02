using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private Node ComposeExpression()
    {
        return ComposeComparisonExpression();
    }

    private Node ComposeSizeExpression()
    {
        return ComposeAdditiveExpression();
    }

    private Node ComposeAdditiveExpression()
    {
        var left = ComposeMultiplicativeExpression();

        while (Current.TokenType is TokenType.Plus or TokenType.Hyphen)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposeMultiplicativeExpression();

            left = op == TokenType.Plus
                ? new AddNode(left, right)
                : new HyphenNode(left, right);
        }

        return left;
    }

    private Node ComposeMultiplicativeExpression()
    {
        var left = ComposePrimaryExpression();

        while (Current.TokenType is TokenType.Star or TokenType.FSlash or TokenType.Mod)
        {
            var op = Current.TokenType;
            Consume(op);
            var right = ComposePrimaryExpression();

            left = op switch
            {
                TokenType.Star => new StarNode(left, right),
                TokenType.FSlash => new FSlashNode(left, right),
                TokenType.Mod => new ModuloNode(left, right),
                _ => throw new InvalidOperationException($"Unexpected operator: {op}")
            };
        }

        return left;
    }

    private Node ComposePrimaryExpression()
    {
        switch (Current.TokenType)
        {
            case TokenType.Integer:
                var intToken = ConsumeAndGetToken(TokenType.Integer);
                return new IntegerNode(intToken.Value, "i", intToken.Span);

            case TokenType.HexadecimalInteger:
                var hexToken = ConsumeAndGetToken(TokenType.HexadecimalInteger);
                return new HexIntegerNode(hexToken.Value, hexToken.Span);

            case TokenType.BinaryInteger:
                var binToken = ConsumeAndGetToken(TokenType.BinaryInteger);
                return new BinaryIntegerNode(binToken.Value, binToken.Span);

            case TokenType.OctalInteger:
                var octToken = ConsumeAndGetToken(TokenType.OctalInteger);
                return new OctalIntegerNode(octToken.Value, octToken.Span);

            case TokenType.Decimal:
                var decimalToken = ConsumeAndGetToken(TokenType.Decimal);
                return new DecimalNode(decimalToken.Value, decimalToken.Span);

            case TokenType.True:
                var trueToken = ConsumeAndGetToken(TokenType.True);
                return new BooleanNode(true, trueToken.Span);

            case TokenType.False:
                var falseToken = ConsumeAndGetToken(TokenType.False);
                return new BooleanNode(false, falseToken.Span);

            case TokenType.Identifier:
            case TokenType.Word:
            case TokenType.Function:
                return ComposeIdentifierOrFunctionCall();

            case TokenType.StringLiteral:

                var stringToken = ConsumeAndGetToken(TokenType.StringLiteral);
                return new WordNode(stringToken.Value, stringToken.Span);

            case TokenType.LeftParenthesis:
                Consume(TokenType.LeftParenthesis);
                var expr = ComposeExpression();
                Consume(TokenType.RightParenthesis);
                return expr;

            case TokenType.Hyphen:
                var minusToken = ConsumeAndGetToken(TokenType.Hyphen);
                var operand = ComposePrimaryExpression();
                return new HyphenNode(new IntegerNode("0", "i"), operand).WithSpan(minusToken.Span.Through(operand.Span));

            default:
                if (AllowedKeywordTokenTypes.Contains(Current.TokenType))
                    return ComposeIdentifierOrFunctionCall();

                throw new SyntaxException(
                    $"Expected integer, identifier, or expression but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
        }
    }
}
