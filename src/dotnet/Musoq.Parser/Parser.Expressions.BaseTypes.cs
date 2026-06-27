using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using KeyAccessToken = Musoq.Parser.Tokens.KeyAccessToken;
using NumericAccessToken = Musoq.Parser.Tokens.NumericAccessToken;

namespace Musoq.Parser;

public partial class Parser
{
    private Node ComposeBaseTypes(int minPrecedence = 0)
    {
        switch (Current.TokenType)
        {
            case TokenType.Decimal:
                var token = ConsumeAndGetToken(TokenType.Decimal);
                return new DecimalNode(token.Value, token.Span);
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
            case TokenType.Function:
                return TryComposeWindowFunction(ComposeAccessMethod(string.Empty));
            case TokenType.Identifier:

                if (Current is not ColumnToken column)
                    throw new ArgumentNullException(
                        $"Expected token is {TokenType.Identifier} but received {Current.TokenType}");

                Consume(TokenType.Identifier);

                return new IdentifierNode(column.Value, null, column.Span);
            case TokenType.Any:
            case TokenType.Some:
            case TokenType.All:
                token = ConsumeAndGetToken();
                return new IdentifierNode(token.Value, null, token.Span);
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
            case TokenType.Exists:
                return ComposeExistsExpression();
            case TokenType.Not:
                Consume(TokenType.Not);
                if (Current.TokenType == TokenType.Exists)
                    return new NotNode(ComposeExistsExpression());

                var previous = Previous ?? Current;
                throw new NotSupportedException(
                    $"Token {previous.Value}({previous.TokenType}) at position {previous.Span.Start} cannot be used here.");
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
                break;
        }

        throw new NotSupportedException(
            $"Token {Current.Value}({Current.TokenType}) at position {Current.Span.Start} cannot be used here.");
    }


    private IntegerNode ComposeInteger()
    {
        var token = (IntegerToken)ConsumeAndGetToken(TokenType.Integer);
        return new IntegerNode(token.Value, token.Abbreviation, token.Span);
    }


    private HexIntegerNode ComposeHexInteger()
    {
        var token = (HexIntegerToken)ConsumeAndGetToken(TokenType.HexadecimalInteger);
        return new HexIntegerNode(token.Value, token.Span);
    }


    private BinaryIntegerNode ComposeBinaryInteger()
    {
        var token = (BinaryIntegerToken)ConsumeAndGetToken(TokenType.BinaryInteger);
        return new BinaryIntegerNode(token.Value, token.Span);
    }


    private OctalIntegerNode ComposeOctalInteger()
    {
        var token = (OctalIntegerToken)ConsumeAndGetToken(TokenType.OctalInteger);
        return new OctalIntegerNode(token.Value, token.Span);
    }


    private WordNode ComposeWord()
    {
        var tokenType = Current.TokenType;

        var token = tokenType switch
        {
            TokenType.Word => ConsumeAndGetToken(TokenType.Word),
            TokenType.StringLiteral => ConsumeAndGetToken(TokenType.StringLiteral),
            _ => throw new NotSupportedException($"Expected Word or StringLiteral but got {tokenType}")
        };
        return new WordNode(token.Value, token.Span);
    }


    private WordNode ComposeSchemaTokenAsWord()
    {
        var token = ConsumeAndGetToken();
        return new WordNode(token.Value, token.Span);
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
                TokenType.Until or TokenType.Between or
                TokenType.Chars or TokenType.Token or
                TokenType.Rest or TokenType.Whitespace or
                TokenType.Optional or TokenType.Repeat or
                TokenType.Switch or TokenType.Nested or
                TokenType.Escaped or TokenType.Greedy or TokenType.Lazy or
                TokenType.Lower or TokenType.Upper or
                TokenType.Capture or TokenType.Extends or TokenType.End => true,
            _ => false
        };
    }


    private static bool IsSqlKeywordToken(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.And or TokenType.Or or TokenType.Not or
                TokenType.Where or TokenType.Select or TokenType.From or
                TokenType.Pivot or TokenType.Unpivot or
                TokenType.Like or TokenType.NotLike or TokenType.RLike or TokenType.NotRLike or
                TokenType.As or TokenType.Is or TokenType.Null or
                TokenType.Union or TokenType.UnionAll or TokenType.Except or TokenType.Intersect or
                TokenType.GroupBy or TokenType.Having or TokenType.Contains or
                TokenType.Skip or TokenType.Take or TokenType.With or
                TokenType.InnerJoin or TokenType.OuterJoin or TokenType.CrossApply or TokenType.OuterApply or
                TokenType.On or TokenType.OrderBy or TokenType.Asc or TokenType.Desc or
                TokenType.Functions or TokenType.True or TokenType.False or
                TokenType.In or TokenType.Exists or TokenType.Any or TokenType.Some or TokenType.All or
                TokenType.NotIn or TokenType.Table or TokenType.Couple or
                TokenType.Case or TokenType.When or TokenType.Then or TokenType.Else or
                TokenType.Distinct or TokenType.ColumnKeyword or TokenType.Between => true,
            _ => false
        };
    }

}
