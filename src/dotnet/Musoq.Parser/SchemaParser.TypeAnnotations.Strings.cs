using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TypeAnnotationNode ComposeStringType()
    {
        var stringToken = ConsumeAndGetToken(TokenType.StringType);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        if (IsNegativeConstantSizeExpression(sizeExpr, out var negativeValue))
            throw InvalidBinarySchemaField($"string[] size must be non-negative, but got {negativeValue}.", sizeExpr.Span);

        var encoding = ComposeStringEncoding();
        var modifiers = ComposeStringModifiers();

        string? asTextSchemaName = null;
        var stringTypeEndSpan = stringToken.Span;
        if (Current.TokenType == TokenType.As)
        {
            Consume(TokenType.As);
            if (Current.TokenType != TokenType.Identifier)
                throw new SyntaxException(
                    $"Expected text schema name after 'as' but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
            var schemaNameToken = Current;
            asTextSchemaName = Current.Value;
            Consume(TokenType.Identifier);
            stringTypeEndSpan = schemaNameToken.Span;
        }

        var stringType = (StringTypeNode)new StringTypeNode(sizeExpr, encoding, modifiers, asTextSchemaName)
            .WithSpan(stringToken.Span.Through(stringTypeEndSpan));

        if (Current.TokenType == TokenType.LeftSquareBracket) return ComposeArrayOfType(stringType);

        return stringType;
    }

    private StringEncoding ComposeStringEncoding()
    {
        var encoding = Current.TokenType switch
        {
            TokenType.Utf8 => StringEncoding.Utf8,
            TokenType.Utf16Le => StringEncoding.Utf16Le,
            TokenType.Utf16Be => StringEncoding.Utf16Be,
            TokenType.Ascii => StringEncoding.Ascii,
            TokenType.Latin1 => StringEncoding.Latin1,
            TokenType.Ebcdic => StringEncoding.Ebcdic,
            _ => throw InvalidBinarySchemaField(
                $"Expected string encoding (utf8, utf16le, utf16be, ascii, latin1, ebcdic) but found '{Current.TokenType}'",
                Current.Span)
        };
        Consume(Current.TokenType);
        return encoding;
    }

    private StringModifier ComposeStringModifiers()
    {
        var modifiers = StringModifier.None;

        while (true)
            switch (Current.TokenType)
            {
                case TokenType.Trim:
                    modifiers |= StringModifier.Trim;
                    Consume(TokenType.Trim);
                    break;
                case TokenType.RTrim:
                    modifiers |= StringModifier.RTrim;
                    Consume(TokenType.RTrim);
                    break;
                case TokenType.LTrim:
                    modifiers |= StringModifier.LTrim;
                    Consume(TokenType.LTrim);
                    break;
                case TokenType.NullTerm:
                    modifiers |= StringModifier.NullTerm;
                    Consume(TokenType.NullTerm);
                    break;
                default:
                    return modifiers;
            }
    }
}
