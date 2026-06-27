using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    private Token ScanIdentifierOrKeyword()
    {
        var start = Position;

        var multiWordToken = TryMatchMultiWordKeyword();
        if (multiWordToken != null)
            return AssignToken(multiWordToken);

        var identifierEnd = start + 1;
        while (identifierEnd < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[identifierEnd]))
            identifierEnd++;

        if (TryScanGenericFunction(start, identifierEnd, out var genericFuncToken))
            return AssignToken(genericFuncToken);

        if (identifierEnd < Input.Length && Input[identifierEnd] == '(')
        {
            Position = identifierEnd;
            return AssignToken(new FunctionToken(Input[start..identifierEnd], new TextSpan(start, identifierEnd - start)));
        }

        if (TryScanMethodAccess(start, identifierEnd, out var methodToken))
            return AssignToken(methodToken);

        if (identifierEnd + 1 < Input.Length && Input[identifierEnd] == '.' && Input[identifierEnd + 1] == '*')
        {
            Position = identifierEnd + 2;
            return AssignToken(new AliasedStarToken(Input[start..Position], new TextSpan(start, Position - start)));
        }

        if (identifierEnd < Input.Length && Input[identifierEnd] == '[' &&
            TryScanAccessToken(start, identifierEnd, out var accessToken))
        {
            return AssignToken(accessToken);
        }

        Position = identifierEnd;
        var span = new TextSpan(start, identifierEnd - start);
        var textSpan = Input.AsSpan(start, identifierEnd - start);

        if (IsSchemaContext && KeywordLookup.TryGetSchemaKeyword(textSpan, out var schemaKeywordType))
        {
            if (_currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(textSpan.ToString(), span));

            return AssignToken(new SchemaToken(textSpan.ToString(), schemaKeywordType, span));
        }

        if (KeywordLookup.TryGetKeyword(textSpan, out var keywordType))
        {
            if (_currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(textSpan.ToString(), span));

            return AssignToken(CreateKeywordToken(keywordType, span));
        }

        if (KeywordLookup.TryGetSchemaKeyword(textSpan, out var nonContextSchemaType))
        {
            var text = textSpan.ToString();
            if (_currentToken?.TokenType == TokenType.Dot)
                return AssignToken(new PropertyToken(text, span));

            if (!IsSchemaContext)
                return AssignToken(new ColumnToken(text, span));

            return AssignToken(new SchemaToken(text, nonContextSchemaType, span));
        }

        if (_currentToken?.TokenType == TokenType.Dot)
            return AssignToken(new PropertyToken(textSpan.ToString(), span));

        return AssignToken(new ColumnToken(textSpan.ToString(), span));
    }

}
