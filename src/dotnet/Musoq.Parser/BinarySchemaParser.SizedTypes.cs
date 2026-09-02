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
            throw InvalidBinarySchemaField($"byte[] size must be non-negative, but got {negativeValue}.", sizeExpr.Span);

        if (Current.TokenType is TokenType.LittleEndian or TokenType.BigEndian)
            throw InvalidBinarySchemaEndianness("byte[] does not accept an endianness specifier", Current.Span);

        return new ByteArrayTypeNode(sizeExpr);
    }

    private BitsTypeNode ComposeBitsType()
    {
        Consume(TokenType.BitsType);
        Consume(TokenType.LeftSquareBracket);

        var countToken = Current;
        if (!TryParseBinaryIntegerLiteral(countToken, out var bitCount))
            throw InvalidBinarySchemaField(
                "bits[] requires a constant integer for bit count",
                GetBinaryIntegerLiteralSpan(countToken));

        Consume(countToken.TokenType);

        if (Current.TokenType != TokenType.RightSquareBracket)
            throw InvalidBinarySchemaField(
                "bits[] requires a single constant integer for bit count",
                Current.Span);

        Consume(TokenType.RightSquareBracket);

        if (bitCount is < 1 or > 64)
            throw InvalidBinarySchemaField(
                $"bits[] count must be between 1 and 64, but got {bitCount}.",
                GetBinaryIntegerLiteralSpan(countToken));

        return new BitsTypeNode(bitCount);
    }

    private AlignmentNode ComposeAlignmentType()
    {
        Consume(TokenType.Align);
        Consume(TokenType.LeftSquareBracket);

        var bitsToken = Current;
        if (!TryParseBinaryIntegerLiteral(bitsToken, out var alignmentBits))
            throw InvalidBinarySchemaField(
                "align[] requires a constant integer for alignment bits",
                GetBinaryIntegerLiteralSpan(bitsToken));

        Consume(bitsToken.TokenType);

        if (Current.TokenType != TokenType.RightSquareBracket)
            throw InvalidBinarySchemaField(
                "align[] requires a single constant integer for alignment bits",
                Current.Span);

        Consume(TokenType.RightSquareBracket);

        if (alignmentBits < 1)
            throw InvalidBinarySchemaField(
                $"align[] boundary must be at least 1 bit, but got {alignmentBits}.",
                GetBinaryIntegerLiteralSpan(bitsToken));

        return new AlignmentNode(alignmentBits);
    }

    private static bool TryParseBinaryIntegerLiteral(Token token, out int value)
    {
        value = 0;
        if (token.TokenType is not (TokenType.Integer or TokenType.HexadecimalInteger or
            TokenType.BinaryInteger or TokenType.OctalInteger or TokenType.Word))
            return false;

        var text = token.Value.Trim();
        if (text.Length == 0)
            return false;

        var isNegative = text[0] == '-';
        var digits = isNegative || text[0] == '+' ? text[1..] : text;
        if (digits.Length == 0)
            return false;

        var numberBase = 10;
        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 16;
            digits = digits[2..];
        }
        else if (digits.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 2;
            digits = digits[2..];
        }
        else if (digits.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
        {
            numberBase = 8;
            digits = digits[2..];
        }

        if (digits.Length == 0 || !TryParseUnsignedDigits(digits, numberBase, out var magnitude))
            return false;

        if (isNegative)
        {
            if (magnitude > 2_147_483_648UL)
                return false;

            value = magnitude == 2_147_483_648UL
                ? int.MinValue
                : -(int)magnitude;
            return true;
        }

        if (magnitude > int.MaxValue)
            return false;

        value = (int)magnitude;
        return true;
    }

    private static bool TryParseUnsignedDigits(string digits, int numberBase, out ulong value)
    {
        value = 0;
        foreach (var character in digits)
        {
            var digit = character switch
            {
                >= '0' and <= '9' => character - '0',
                >= 'a' and <= 'f' => character - 'a' + 10,
                >= 'A' and <= 'F' => character - 'A' + 10,
                _ => -1
            };

            if (digit < 0 || digit >= numberBase || value > (ulong.MaxValue - (uint)digit) / (uint)numberBase)
                return false;

            value = value * (uint)numberBase + (uint)digit;
        }

        return true;
    }

    private static TextSpan GetBinaryIntegerLiteralSpan(Token token)
    {
        var value = token.Value;
        var trimmed = value.Trim();
        if (trimmed.Length == 0 || token.Span.IsEmpty)
            return token.Span;

        var leadingWhitespace = value.Length - value.TrimStart().Length;
        return new TextSpan(token.Span.Start + leadingWhitespace, trimmed.Length);
    }
}
