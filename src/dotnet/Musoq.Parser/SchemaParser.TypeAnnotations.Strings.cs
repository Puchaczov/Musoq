using Musoq.Parser.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

public partial class SchemaParser
{
    private TypeAnnotationNode ComposeStringType()
    {
        Consume(TokenType.StringType);
        Consume(TokenType.LeftSquareBracket);
        var sizeExpr = ComposeSizeExpression();
        Consume(TokenType.RightSquareBracket);

        var encoding = ComposeStringEncoding();
        var modifiers = ComposeStringModifiers();

        string? asTextSchemaName = null;
        if (Current.TokenType == TokenType.As)
        {
            Consume(TokenType.As);
            if (Current.TokenType != TokenType.Identifier)
                throw new SyntaxException(
                    $"Expected text schema name after 'as' but found '{Current.TokenType}'",
                    _lexer.AlreadyResolvedQueryPart);
            asTextSchemaName = Current.Value;
            Consume(TokenType.Identifier);
        }

        var stringType = new StringTypeNode(sizeExpr, encoding, modifiers, asTextSchemaName);

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
            _ => throw new SyntaxException(
                $"Expected string encoding (utf8, utf16le, utf16be, ascii, latin1, ebcdic) but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart)
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
