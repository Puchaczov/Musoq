using System;
using System.Diagnostics;

namespace Musoq.Parser.Tokens;

[DebuggerDisplay("{Value} of type {TokenType},nq")]
public class Token : GenericToken<TokenType>, IEquatable<Token>
{
    protected Token(string value, TokenType type, TextSpan span)
        : base(value, type, span)
    {
    }

    public bool Equals(Token other)
    {
        return other != null && other.TokenType == TokenType && other.Value == Value;
    }

    public override GenericToken<TokenType> Clone()
    {
        return new Token(Value, TokenType, Span);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Token token))
            return false;
        if (ReferenceEquals(obj, this))
            return true;
        return TokenType == token.TokenType && Value == token.Value;
    }

    public override int GetHashCode()
    {
        return 17 * TokenType.GetHashCode() + Value.GetHashCode();
    }

    public override string ToString()
    {
        return Value;
    }
}
