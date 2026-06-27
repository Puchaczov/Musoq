using System.Collections.Generic;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    private Token ScanParameterReference()
    {
        var start = Position;
        Position++;

        if (Position >= Input.Length || !FastCharacterClassifier.IsIdentifierStart(Input[Position]))
        {
            var span = new TextSpan(start, 1);

            if (RecoverOnError)
            {
                Diagnostics.AddError(DiagnosticCode.MQ1001_UnknownToken, span, "$");
                return AssignToken(new ErrorToken('$', span));
            }

            throw new UnknownTokenException(start, '$', Input[start..]);
        }

        var nameStart = Position;
        Position++;

        while (Position < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[Position]))
            Position++;

        return AssignToken(new ParameterReferenceToken(
            Input[nameStart..Position],
            new TextSpan(start, Position - start)));
    }

    private Token SplitNumericAccessToken(Token numericAccessToken)
    {
        if (numericAccessToken is not NumericAccessToken numericToken)
            return numericAccessToken;

        var typeName = numericToken.Name;
        var sizeValue = numericToken.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var basePosition = numericAccessToken.Span.Start;

        var typeToken = ResolveSchemaTypeToken(typeName, basePosition);

        var leftBracketPos = basePosition + typeName.Length;
        var integerPos = leftBracketPos + 1;
        var rightBracketPos = integerPos + sizeValue.Length;

        _pendingSchemaTokens.Enqueue(new LeftSquareBracketToken(new TextSpan(leftBracketPos, 1)));
        _pendingSchemaTokens.Enqueue(new IntegerToken(sizeValue, new TextSpan(integerPos, sizeValue.Length), "i"));
        _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(rightBracketPos, 1)));

        return AssignToken(typeToken);
    }

    private Token SplitKeyAccessToken(Token keyAccessToken)
    {
        if (keyAccessToken is not KeyAccessToken keyToken)
            return keyAccessToken;

        var typeName = keyToken.Name;
        var innerContent = keyToken.Key.Trim('\'');
        var basePosition = keyAccessToken.Span.Start;

        var typeToken = ResolveSchemaTypeToken(typeName, basePosition);

        var leftBracketPos = basePosition + typeName.Length;
        _pendingSchemaTokens.Enqueue(new LeftSquareBracketToken(new TextSpan(leftBracketPos, 1)));

        var innerPos = leftBracketPos + 1;
        var innerTokens = LexInnerExpression(innerContent, innerPos);
        foreach (var innerToken in innerTokens)
            _pendingSchemaTokens.Enqueue(innerToken);

        var rightBracketPos = innerPos + innerContent.Length;
        _pendingSchemaTokens.Enqueue(new RightSquareBracketToken(new TextSpan(rightBracketPos, 1)));

        return AssignToken(typeToken);
    }

    private static List<Token> LexInnerExpression(string content, int basePosition)
    {
        var tokens = new List<Token>();
        var pos = 0;

        while (pos < content.Length)
        {
            while (pos < content.Length && char.IsWhiteSpace(content[pos])) pos++;
            if (pos >= content.Length) break;

            var ch = content[pos];
            var spanStart = basePosition + pos;

            switch (ch)
            {
                case '+':
                    tokens.Add(new PlusToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '-':
                    tokens.Add(new HyphenToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '*':
                    tokens.Add(new StarToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '/':
                    tokens.Add(new FSlashToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '%':
                    tokens.Add(new ModuloToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case '(':
                    tokens.Add(new LeftParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                case ')':
                    tokens.Add(new RightParenthesisToken(new TextSpan(spanStart, 1)));
                    pos++;
                    break;
                default:
                    if (char.IsDigit(ch))
                    {
                        var start = pos;
                        while (pos < content.Length && char.IsDigit(content[pos])) pos++;
                        tokens.Add(new IntegerToken(content[start..pos], new TextSpan(spanStart, pos - start), "i"));
                    }
                    else if (char.IsLetter(ch) || ch == '_')
                    {
                        var start = pos;
                        while (pos < content.Length &&
                               (char.IsLetterOrDigit(content[pos]) || content[pos] == '_')) pos++;
                        tokens.Add(new WordToken(content[start..pos], new TextSpan(spanStart, pos - start)));
                    }
                    else
                    {
                        pos++;
                    }

                    break;
            }
        }

        return tokens;
    }

    private static SchemaToken ResolveSchemaTypeToken(string typeName, int position)
    {
        var span = new TextSpan(position, typeName.Length);
        var tokenType = KeywordLookup.GetSchemaKeywordType(typeName);
        return new SchemaToken(typeName, tokenType, span);
    }
}
