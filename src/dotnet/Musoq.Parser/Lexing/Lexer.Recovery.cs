using Musoq.Parser.Diagnostics;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    private Token ScanUnknown()
    {
        var start = Position;
        var c = Input[start];
        var span = new TextSpan(start, 1);
        Position++;


        if (RecoverOnError)
        {
            Diagnostics.AddError(DiagnosticCode.MQ1001_UnknownToken, span, c.ToString());
            return AssignToken(new ErrorToken(c, span));
        }

        var remaining = Input[start..];
        throw new UnknownTokenException(start, c, remaining);
    }

    private bool ShouldSkipToken(Token token)
    {
        return (_skipWhiteSpaces && token.TokenType == TokenType.WhiteSpace) ||
               token.TokenType == TokenType.Comment ||
               (RecoverOnError && token.TokenType == TokenType.Error);
    }
}
