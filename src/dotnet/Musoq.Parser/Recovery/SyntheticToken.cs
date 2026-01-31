using Musoq.Parser.Tokens;

namespace Musoq.Parser.Recovery;

/// <summary>
///     A synthetic token created during error recovery.
/// </summary>
internal sealed class SyntheticToken : Token
{
    public SyntheticToken(string value, TokenType type, TextSpan span)
        : base(value, type, span)
    {
        IsSynthetic = true;
    }

    /// <summary>
    ///     Gets a value indicating whether this token was synthesized during error recovery.
    /// </summary>
    public bool IsSynthetic { get; }

    /// <inheritdoc />
    public override GenericToken<TokenType> Clone()
    {
        return new SyntheticToken(Value, TokenType, Span);
    }
}
