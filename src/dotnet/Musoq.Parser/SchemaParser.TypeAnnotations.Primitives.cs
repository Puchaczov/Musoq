using Musoq.Parser.Diagnostics;
using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private static bool IsTypeKeyword(TokenType tokenType)
    {
        return tokenType is TokenType.ByteType or TokenType.SByteType or
            TokenType.ShortType or TokenType.UShortType or
            TokenType.IntType or TokenType.UIntType or
            TokenType.LongType or TokenType.ULongType or
            TokenType.FloatType or TokenType.DoubleType or
            TokenType.StringType or TokenType.BitsType or TokenType.Align;
    }

    private TypeAnnotationNode ComposeTypeAnnotation(string? fieldName = null)
    {
        return Current.TokenType switch
        {
            TokenType.ByteType when PeekNextTokenType() == TokenType.LeftSquareBracket =>
                ComposeByteArrayType(),
            TokenType.ByteType => ComposePrimitiveType(PrimitiveTypeName.Byte, false),
            TokenType.SByteType => ComposePrimitiveType(PrimitiveTypeName.SByte, false),
            TokenType.ShortType => ComposePrimitiveType(PrimitiveTypeName.Short, true),
            TokenType.UShortType => ComposePrimitiveType(PrimitiveTypeName.UShort, true),
            TokenType.IntType => ComposePrimitiveType(PrimitiveTypeName.Int, true),
            TokenType.UIntType => ComposePrimitiveType(PrimitiveTypeName.UInt, true),
            TokenType.LongType => ComposePrimitiveType(PrimitiveTypeName.Long, true),
            TokenType.ULongType => ComposePrimitiveType(PrimitiveTypeName.ULong, true),
            TokenType.FloatType => ComposePrimitiveType(PrimitiveTypeName.Float, true),
            TokenType.DoubleType => ComposePrimitiveType(PrimitiveTypeName.Double, true),

            TokenType.Identifier when Current.Value.Equals("byte", StringComparison.OrdinalIgnoreCase) =>
                ComposeByteArrayType(),

            TokenType.StringType => ComposeStringType(),

            TokenType.BitsType => ComposeBitsType(),

            TokenType.Align => ComposeAlignmentType(),

            TokenType.LBracket => ComposeInlineSchemaOrArray(),

            TokenType.Switch => ComposeBinarySwitchType(),

            TokenType.Substream => ComposeSubstreamType(fieldName),

            TokenType.Identifier or TokenType.Word => ComposeSchemaReferenceOrArray(),

            _ => throw new SyntaxException(
                $"Expected type annotation but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart)
        };
    }

    private TypeAnnotationNode ComposePrimitiveType(PrimitiveTypeName typeName, bool canHaveEndianness)
    {
        Consume(Current.TokenType);

        Node? arraySizeExpr = null;
        if (Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            arraySizeExpr = ComposeSizeExpression();
            Consume(TokenType.RightSquareBracket);
        }

        Endianness endianness;
        if (canHaveEndianness)
        {
            endianness = Current.TokenType switch
            {
                TokenType.LittleEndian => Endianness.LittleEndian,
                TokenType.BigEndian => Endianness.BigEndian,
                _ => throw new SyntaxException(
                    $"Multi-byte type '{typeName}' requires endianness specifier (le or be)",
                    _lexer.AlreadyResolvedQueryPart)
            };
            Consume(Current.TokenType);
        }
        else
        {
            endianness = Endianness.NotApplicable;
        }

        if (arraySizeExpr == null && Current.TokenType == TokenType.LeftSquareBracket)
        {
            Consume(TokenType.LeftSquareBracket);
            arraySizeExpr = ComposeSizeExpression();
            Consume(TokenType.RightSquareBracket);
        }

        var primitiveType = new PrimitiveTypeNode(typeName, endianness);

        if (arraySizeExpr == null)
            return primitiveType;

        if (IsNegativeConstantSizeExpression(arraySizeExpr, out var negativeArrayValue))
            throw new SyntaxException(
                $"{typeName}[] size must be non-negative, but got {negativeArrayValue}.",
                _lexer.AlreadyResolvedQueryPart,
                DiagnosticCode.MQ4001_InvalidBinarySchemaField,
                arraySizeExpr.Span);

        return new ArrayTypeNode(primitiveType, arraySizeExpr);
    }
}
