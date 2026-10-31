using Musoq.Parser.Tokens;

namespace Musoq.Parser.Syntax;

internal static class SyntaxTokenRoles
{
    internal static SyntaxTokenRole Of(Token token)
    {
        var type = token.TokenType;
        if (type is TokenType.Comment)
            return SyntaxTokenRole.Comment;
        if (type is TokenType.WhiteSpace)
            return SyntaxTokenRole.Whitespace;
        if (type is TokenType.StringLiteral or TokenType.Word or TokenType.Decimal or TokenType.Integer
            or TokenType.HexadecimalInteger or TokenType.BinaryInteger or TokenType.OctalInteger
            or TokenType.True or TokenType.False or TokenType.Null)
            return SyntaxTokenRole.Literal;
        if (type is TokenType.Identifier or TokenType.Function or TokenType.Property or TokenType.ParameterReference
            or TokenType.KeyAccess or TokenType.NumericAccess or TokenType.MethodAccess or TokenType.AliasedStar
            or TokenType.NumericColumn or TokenType.Underscore)
            return SyntaxTokenRole.Identifier;
        if (type is >= TokenType.LittleEndian and <= TokenType.Ebcdic)
            return SyntaxTokenRole.TypeName;
        if (type is TokenType.Comma or TokenType.Semicolon or TokenType.Dot or TokenType.Colon
            or TokenType.LeftParenthesis or TokenType.RightParenthesis or TokenType.LeftSquareBracket
            or TokenType.RightSquareBracket or TokenType.LBracket or TokenType.RBracket)
            return SyntaxTokenRole.Punctuation;
        if (type is TokenType.Plus or TokenType.Hyphen or TokenType.Star or TokenType.FSlash or TokenType.Mod
            or TokenType.Equality or TokenType.Diff or TokenType.Greater or TokenType.GreaterEqual
            or TokenType.Less or TokenType.LessEqual or TokenType.Ampersand or TokenType.Pipe or TokenType.Caret
            or TokenType.LeftShift or TokenType.RightShift or TokenType.FatArrow or TokenType.NullCoalescing
            or TokenType.QuestionMark or TokenType.DoubleColon or TokenType.StringConcatenation)
            return SyntaxTokenRole.Operator;
        return SyntaxTokenRole.Keyword;
    }
}
