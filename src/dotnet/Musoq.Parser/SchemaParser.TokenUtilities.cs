using Musoq.Parser.Exceptions;
using Musoq.Parser.Tokens;

namespace Musoq.Parser;

/// <summary>
///     Parser for binary and text schema definitions.
///     Handles the interpretation schema syntax for defining data formats.
/// </summary>
public partial class SchemaParser
{

    private string ComposeIdentifierOrWord()
    {
        if (Current.TokenType == TokenType.Identifier)
            return ConsumeAndGetToken(TokenType.Identifier).Value;

        if (Current.TokenType == TokenType.Word)
            return ConsumeAndGetToken(TokenType.Word).Value;

        if (Current is { TokenType: TokenType.Property, Value: "_" })
            return ConsumeAndGetToken(TokenType.Property).Value;

        if (AllowedKeywordTokenTypes.Contains(Current.TokenType))
            return ConsumeAndGetToken(Current.TokenType).Value;

        throw new SyntaxException(
            $"Expected identifier but found '{Current.TokenType}' ({Current.Value})",
            _lexer.AlreadyResolvedQueryPart);
    }

    private string ComposeStringLiteral()
    {
        if (Current.TokenType != TokenType.Word && Current.TokenType != TokenType.StringLiteral)
            throw new SyntaxException(
                $"Expected string literal but found '{Current.TokenType}'",
                _lexer.AlreadyResolvedQueryPart);

        var token = ConsumeAndGetToken(Current.TokenType);

        var value = token.Value;

        if (token.TokenType == TokenType.Word &&
            ((value.StartsWith('\'') && value.EndsWith('\'')) ||
             (value.StartsWith('"') && value.EndsWith('"'))) && value.Length >= 2)
            value = value[1..^1];

        return token.TokenType == TokenType.StringLiteral ? value : UnescapeString(value);
    }

    private static string UnescapeString(string value)
    {
        return value
            .Replace("\\'", "'", StringComparison.Ordinal)
            .Replace("\\\"", "\"", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal)
            .Replace("\\r", "\r", StringComparison.Ordinal)
            .Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace("\\t", "\t", StringComparison.Ordinal);
    }

    private TokenType PeekNextTokenType()
    {
        _savedTokenBeforePeek = _lexer.Current();

        _lexer.Next();
        var nextToken = _lexer.Current();

        _peekedToken = nextToken;

        return nextToken.TokenType;
    }

    private void Consume(TokenType tokenType)
    {
        if (!Current.TokenType.Equals(tokenType))
            throw new SyntaxException(
                $"Expected token '{tokenType}' but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart);

        if (_pendingGenericGreaterTokens > 0)
        {
            _pendingGenericGreaterTokens--;
            return;
        }

        _hasReplacedToken = false;
        _savedTokenBeforePeek = null;

        if (_peekedToken != null)
        {
            _replacedToken = _peekedToken;
            _hasReplacedToken = true;
            _peekedToken = null;
        }
        else
        {
            _lexer.Next();
        }
    }

    private void ConsumeGenericGreater()
    {
        if (Current.TokenType == TokenType.Greater)
        {
            Consume(TokenType.Greater);
            return;
        }

        if (Current.TokenType != TokenType.RightShift)
            throw new SyntaxException(
                $"Expected token '{TokenType.Greater}' but found '{Current.TokenType}' ({Current.Value})",
                _lexer.AlreadyResolvedQueryPart);

        _hasReplacedToken = false;
        _savedTokenBeforePeek = null;
        _pendingGenericGreaterTokens++;

        if (_peekedToken != null)
        {
            _replacedToken = _peekedToken;
            _hasReplacedToken = true;
            _peekedToken = null;
            return;
        }

        _lexer.Next();
    }

    private Token ConsumeAndGetToken(TokenType expected)
    {
        var token = Current;
        Consume(expected);
        return token;
    }
}
