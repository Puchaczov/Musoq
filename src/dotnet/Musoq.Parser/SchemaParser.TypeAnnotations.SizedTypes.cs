using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private ByteArrayTypeNode ComposeByteArrayType()
    {
        Consume(Current.TokenType);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        if (IsNegativeConstantSizeExpression(sizeExpr, out var negativeValue))
            throw new SyntaxException(
                $"byte[] size must be non-negative, but got {negativeValue}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4001_InvalidBinarySchemaField,
                sizeExpr.Span);

        return new ByteArrayTypeNode(sizeExpr);
    }

    private BitsTypeNode ComposeBitsType()
    {
        Consume(TokenType.BitsType);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "bits[] requires a constant integer for bit count",
                _lexer.AlreadyResolvedQueryPart);

        var countToken = ConsumeAndGetToken(TokenType.Integer);
        var bitCount = int.Parse(countToken.Value, System.Globalization.CultureInfo.InvariantCulture);

        Consume(TokenType.RightSquareBracket);

        if (bitCount is < 1 or > 64)
            throw new SyntaxException(
                $"bits[] count must be between 1 and 64, but got {bitCount}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4001_InvalidBinarySchemaField,
                countToken.Span);

        return new BitsTypeNode(bitCount);
    }

    private AlignmentNode ComposeAlignmentType()
    {
        Consume(TokenType.Align);
        Consume(TokenType.LeftSquareBracket);

        if (Current.TokenType != TokenType.Integer)
            throw new SyntaxException(
                "align[] requires a constant integer for alignment bits",
                _lexer.AlreadyResolvedQueryPart);

        var bitsToken = ConsumeAndGetToken(TokenType.Integer);
        var alignmentBits = int.Parse(bitsToken.Value, System.Globalization.CultureInfo.InvariantCulture);

        Consume(TokenType.RightSquareBracket);

        if (alignmentBits < 1)
            throw new SyntaxException(
                $"align[] boundary must be at least 1 bit, but got {alignmentBits}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4001_InvalidBinarySchemaField,
                bitsToken.Span);

        return new AlignmentNode(alignmentBits);
    }
}
