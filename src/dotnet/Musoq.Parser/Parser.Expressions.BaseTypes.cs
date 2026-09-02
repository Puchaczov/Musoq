using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using KeyAccessToken = Musoq.Parser.Tokens.KeyAccessToken;
using NumericAccessToken = Musoq.Parser.Tokens.NumericAccessToken;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeBaseTypes(int minPrecedence = 0)
    {
        if (SqlKeywordTokenFacts.IsContextualExpressionIdentifier(Current.TokenType))
        {
            if (Current.TokenType == TokenType.Exists)
                return ComposeExistsPredicateOrIdentifier();

            return ComposeContextualKeywordIdentifier();
        }

        switch (Current.TokenType)
        {
            case TokenType.Decimal:
                var token = ConsumeAndGetToken(TokenType.Decimal);
                try
                {
                    return new DecimalNode(token.Value, token.Span);
                }
                catch (Exception ex) when (IsNumericConstructionFailure(ex))
                {
                    throw NumericLiteralOutOfRange(token, ex);
                }
            case TokenType.Integer:
                return ComposeInteger();
            case TokenType.HexadecimalInteger:
                return ComposeHexInteger();
            case TokenType.BinaryInteger:
                return ComposeBinaryInteger();
            case TokenType.OctalInteger:
                return ComposeOctalInteger();
            case TokenType.Word:
            case TokenType.StringLiteral:
                return ComposeWord();
            case TokenType.Skip:
            case TokenType.Take:
                ReplaceCurrentToken(new FunctionToken(Current.Value, Current.Span));
                return ComposeAccessMethod(string.Empty);
            case TokenType.Function when IsExistsFunction(Current):
                return ComposeExistsPredicateOrIdentifier();
            case TokenType.Function:
                return TryComposeWindowFunction(ComposeAccessMethod(string.Empty));
            case TokenType.Identifier:

                if (Current is not ColumnToken column)
                    throw new ArgumentNullException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}");

                Consume(TokenType.Identifier);

                return new IdentifierNode(column.Value, null, column.Span);
            case TokenType.KeyAccess:
                var keyAccess = (KeyAccessToken)Current;
                Consume(TokenType.KeyAccess);
                return new AccessObjectKeyNode(keyAccess);
            case TokenType.NumericAccess:
                var numericAccess = (NumericAccessToken)Current;
                Consume(TokenType.NumericAccess);
                return new AccessObjectArrayNode(numericAccess);
            case TokenType.MethodAccess:
                var methodAccess = (MethodAccessToken)Current;
                Consume(TokenType.MethodAccess);
                Consume(TokenType.Dot);
                return TryComposeWindowFunction(ComposeAccessMethod(methodAccess.Alias));
            case TokenType.Property:
                token = ConsumeAndGetToken(TokenType.Property);
                return new PropertyValueNode(token.Value).WithSpan(token.Span);
            case TokenType.AliasedStar:
                token = ConsumeAndGetToken(TokenType.AliasedStar);
                return TryComposeStarModifiers(
                    new AllColumnsNode(token.Value.Replace(".*", string.Empty, StringComparison.Ordinal)).WithSpan(token.Span));
            case TokenType.Star:
                token = ConsumeAndGetToken(TokenType.Star);
                return TryComposeStarModifiers(
                    new AllColumnsNode().WithSpan(token.Span));
            case TokenType.True:
                token = ConsumeAndGetToken(TokenType.True);
                return new BooleanNode(true, token.Span);
            case TokenType.False:
                token = ConsumeAndGetToken(TokenType.False);
                return new BooleanNode(false, token.Span);
            case TokenType.LeftParenthesis:
                return ComposeParenthesizedExpressionOrScalarSubquery();
            case TokenType.Not:
                Consume(TokenType.Not);
                return new NotNode(ComposeEqualityOperators());
            case TokenType.Hyphen:
                Consume(TokenType.Hyphen);
                return new StarNode(new IntegerNode("-1", "s"),
                    Compose(f => f.ComposeArithmeticExpression(minPrecedence)));
            case TokenType.Case:
                var (whenThenNodes, elseNode) = ComposeCase();
                return new CaseNode(whenThenNodes, elseNode);
            case TokenType.Null:
                token = ConsumeAndGetToken(TokenType.Null);
                return new NullNode(token.Span);
            case TokenType.ParameterReference:
                token = ConsumeAndGetToken(TokenType.ParameterReference);
                return new ParameterReferenceNode(token.Value, null, token.Span);
            default:

                if (IsSchemaKeywordToken(Current.TokenType)) return ComposeSchemaTokenAsWord();
                if (GetArithmeticPrecedence(Current.TokenType) >= 0)
                    throw new SyntaxException(
                        "A binary operator is missing its left operand.",
                        _lexer.AlreadyResolvedQueryPart,
                        DiagnosticCode.MQ2020_MissingOperand,
                        Current.Span);
                break;
        }

        throw new SyntaxException(
            $"Token {Current.Value}({Current.TokenType}) at position {Current.Span.Start} cannot be used here.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ2001_UnexpectedToken,
            Current.Span);
    }

    private IntegerNode ComposeInteger()
    {
        var token = (IntegerToken)ConsumeAndGetToken(TokenType.Integer);
        try
        {
            return new IntegerNode(token.Value, token.Abbreviation, token.Span);
        }
        catch (Exception ex) when (IsNumericConstructionFailure(ex))
        {
            throw NumericLiteralOutOfRange(token, ex);
        }
    }

    private HexIntegerNode ComposeHexInteger()
    {
        var token = (HexIntegerToken)ConsumeAndGetToken(TokenType.HexadecimalInteger);
        try
        {
            return new HexIntegerNode(token.Value, token.Span);
        }
        catch (Exception ex) when (IsNumericConstructionFailure(ex))
        {
            throw NumericLiteralOutOfRange(token, ex);
        }
    }

    private BinaryIntegerNode ComposeBinaryInteger()
    {
        var token = (BinaryIntegerToken)ConsumeAndGetToken(TokenType.BinaryInteger);
        try
        {
            return new BinaryIntegerNode(token.Value, token.Span);
        }
        catch (Exception ex) when (IsNumericConstructionFailure(ex))
        {
            throw NumericLiteralOutOfRange(token, ex);
        }
    }

    private OctalIntegerNode ComposeOctalInteger()
    {
        var token = (OctalIntegerToken)ConsumeAndGetToken(TokenType.OctalInteger);
        try
        {
            return new OctalIntegerNode(token.Value, token.Span);
        }
        catch (Exception ex) when (IsNumericConstructionFailure(ex))
        {
            throw NumericLiteralOutOfRange(token, ex);
        }
    }

    private WordNode ComposeWord()
    {
        var tokenType = Current.TokenType;

        var token = tokenType switch
        {
            TokenType.Word => ConsumeAndGetToken(TokenType.Word),
            TokenType.StringLiteral => ConsumeAndGetToken(TokenType.StringLiteral),
            _ => throw new SyntaxException(
                $"Expected Word or StringLiteral but got {tokenType}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ2001_UnexpectedToken,
                Current.Span)
        };
        return new WordNode(token.Value, token.Span);
    }

    private SyntaxException NumericLiteralOutOfRange(Token token, Exception innerException)
    {
        var literal = token.Span.Start >= 0 && token.Span.End <= _lexer.Input.Length ? _lexer.Input[token.Span.Start..token.Span.End] : token.Value;
        return new SyntaxException(
            $"Numeric literal '{literal}' is outside the supported range.",
            _lexer.AlreadyResolvedQueryPart,
            DiagnosticCode.MQ1009_NumericLiteralOutOfRange,
            token.Span,
            innerException);
    }

    private static bool IsNumericConstructionFailure(Exception exception)
    {
        return exception is OverflowException or FormatException or ArgumentException or NotSupportedException;
    }

    private WordNode ComposeSchemaTokenAsWord()
    {
        var token = ConsumeAndGetToken();
        return new WordNode(token.Value, token.Span);
    }

    private IdentifierNode ComposeContextualKeywordIdentifier()
    {
        return CreateContextualKeywordIdentifier(ConsumeAndGetToken());
    }

    private IdentifierNode CreateContextualKeywordIdentifier(Token token)
    {
        var identifier = _lexer.Input.Substring(token.Span.Start, token.Span.Length);
        return new IdentifierNode(identifier, null, token.Span);
    }

    private static bool IsExistsFunction(Token token)
    {
        return token is FunctionToken function &&
               function.Value.Equals(ExistsToken.TokenText, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSchemaKeywordToken(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Binary or TokenType.Text or
                TokenType.LittleEndian or TokenType.BigEndian or
                TokenType.ByteType or TokenType.SByteType or
                TokenType.ShortType or TokenType.UShortType or
                TokenType.IntType or TokenType.UIntType or
                TokenType.LongType or TokenType.ULongType or
                TokenType.FloatType or TokenType.DoubleType or
                TokenType.BitsType or TokenType.Align or
                TokenType.StringType or TokenType.Utf8 or
                TokenType.Utf16Le or TokenType.Utf16Be or
                TokenType.Ascii or TokenType.Latin1 or TokenType.Ebcdic or
                TokenType.Trim or TokenType.RTrim or TokenType.LTrim or
                TokenType.NullTerm or TokenType.Check or
                TokenType.At or TokenType.Colon or
                TokenType.Pattern or TokenType.Literal or
                TokenType.Until or
                TokenType.Chars or TokenType.Token or
                TokenType.Rest or TokenType.Whitespace or
                TokenType.Optional or TokenType.Repeat or
                TokenType.Switch or TokenType.Nested or
                TokenType.Escaped or TokenType.Greedy or TokenType.Lazy or
                TokenType.Lower or TokenType.Upper or
                TokenType.Capture or TokenType.Extends => true,
            _ => false
        };
    }

}
