using Musoq.Parser.Diagnostics;
using Musoq.Parser.Helpers;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Lexing;

/// <summary>
///     High-performance lexer that uses direct character scanning instead of regex matching.
///     Provides 17-42x speedup over previous regex-based lexer for most queries.
/// </summary>
public sealed partial class Lexer
{

    private Token ScanNumber()
    {
        var start = Position;


        if (Input[Position] == '0' && Position + 1 < Input.Length)
        {
            var next = char.ToLowerInvariant(Input[Position + 1]);
            if (next == 'x')
            {
                var hexMatch = HexIntegerRegex.Match(Input, Position);
                if (hexMatch.Success && hexMatch.Index == Position)
                {
                    Position += hexMatch.Length;
                    return AssignToken(new HexIntegerToken(hexMatch.Value, new TextSpan(start, hexMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1006_InvalidHexNumber, "hexadecimal", "0x");
            }

            if (next == 'b')
            {
                var binMatch = BinaryIntegerRegex.Match(Input, Position);
                if (binMatch.Success && binMatch.Index == Position)
                {
                    Position += binMatch.Length;
                    return AssignToken(new BinaryIntegerToken(binMatch.Value, new TextSpan(start, binMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1007_InvalidBinaryNumber, "binary", "0b");
            }

            if (next == 'o')
            {
                var octMatch = OctalIntegerRegex.Match(Input, Position);
                if (octMatch.Success && octMatch.Index == Position)
                {
                    Position += octMatch.Length;
                    return AssignToken(new OctalIntegerToken(octMatch.Value, new TextSpan(start, octMatch.Length)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1008_InvalidOctalNumber, "octal", "0o");
            }
        }


        while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
            Position++;


        if (Position < Input.Length && Input[Position] == '.')
            if (Position + 1 < Input.Length && FastCharacterClassifier.IsDigit(Input[Position + 1]))
            {
                Position++;
                while (Position < Input.Length && FastCharacterClassifier.IsDigit(Input[Position]))
                    Position++;

                var decimalTextEnd = Position;


                if (Position < Input.Length && (Input[Position] == 'd' || Input[Position] == 'D'))
                    Position++;

                var text = Input[start..decimalTextEnd];
                return AssignToken(new DecimalToken(text, new TextSpan(start, Position - start)));
            }


        var numericEnd = Position;
        var suffix = string.Empty;
        if (Position < Input.Length)
        {
            var ch = char.ToLowerInvariant(Input[Position]);


            if (ch == 'd')
            {
                Position++;
                var intText = Input[start..numericEnd];
                return AssignToken(new DecimalToken(intText, new TextSpan(start, Position - start)));
            }


            if (ch == 'u' && Position + 1 < Input.Length)
            {
                var nextCh = char.ToLowerInvariant(Input[Position + 1]);
                if (nextCh is 'i' or 'l' or 's' or 'b')
                {
                    suffix = Input.Substring(Position, 2).ToUpperInvariant();
                    Position += 2;
                }
            }
            else if (ch is 'i' or 'l' or 's' or 'b')
            {
                suffix = FastCharacterClassifier.CharToString(ch);
                Position++;
            }
        }

        var numText = Input[start..numericEnd];
        return AssignToken(new IntegerToken(numText, new TextSpan(start, Position - start), suffix));
    }

    private Token HandleInvalidBaseNumber(int start, DiagnosticCode code, string baseName, string prefix)
    {
        var scanEnd = Position + 2;
        while (scanEnd < Input.Length && FastCharacterClassifier.IsIdentifierContinue(Input[scanEnd]))
            scanEnd++;

        var invalidLiteral = Input[start..scanEnd];
        var span = new TextSpan(start, scanEnd - start);

        if (RecoverOnError)
        {
            Diagnostics.AddError(code, span, invalidLiteral);
            Position = scanEnd;
            return AssignToken(new ErrorToken(Input[start], span));
        }

        Position = scanEnd;
        throw new LexerException(
            $"Invalid {baseName} number literal '{invalidLiteral}'. Expected valid {baseName} digits after '{prefix}' prefix.",
            start,
            code);
    }

    private Token ScanStringLiteral()
    {
        var start = Position;
        var match = StringLiteralRegex.Match(Input, Position);

        if (match.Success && match.Index == Position)
        {
            Position += match.Length;
            var fullText = match.Value;
            var innerText = fullText[1..^1];

            if (TryFindInvalidEscapeSequence(innerText.AsSpan(), out var invalidEscape, out var invalidEscapeSpan))
            {
                var absoluteSpan = new TextSpan(start + 1 + invalidEscapeSpan.Start, invalidEscapeSpan.Length);
                var message = $"Invalid escape sequence '{invalidEscape}'.";

                if (RecoverOnError)
                    Diagnostics.AddError(DiagnosticCode.MQ1004_InvalidEscapeSequence, message, absoluteSpan);
                else
                    throw new LexerException(message, absoluteSpan.Start, DiagnosticCode.MQ1004_InvalidEscapeSequence);
            }

            var unescaped = innerText.Unescape();
            return AssignToken(new StringLiteralToken(unescaped, new TextSpan(start, match.Length)));
        }


        if (Position + 1 < Input.Length && Input[Position + 1] == '\'')
        {
            Position += 2;
            return AssignToken(new StringLiteralToken(string.Empty, new TextSpan(start, 2)));
        }


        if (RecoverOnError)
        {
            var end = Position + 1;
            while (end < Input.Length && Input[end] != '\n' && Input[end] != '\r')
                end++;

            var span = new TextSpan(start, end - start);
            Diagnostics.AddError(DiagnosticCode.MQ1002_UnterminatedString,
                "Unterminated string literal: missing closing '", span);

            Position = end;
            return AssignToken(new ErrorToken(Input[start..end], span));
        }

        throw new LexerException("Unterminated string literal: missing closing '", start, DiagnosticCode.MQ1002_UnterminatedString);
    }

    private static bool TryFindInvalidEscapeSequence(ReadOnlySpan<char> value, out string invalidEscape,
        out TextSpan span)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != '\\')
                continue;

            if (i + 1 >= value.Length)
            {
                invalidEscape = "\\";
                span = new TextSpan(i, 1);
                return true;
            }

            var next = value[i + 1];

            if (IsSimpleEscape(next))
            {
                i += 1;
                continue;
            }

            if (next == 'u')
                return TryValidateFixedLengthEscape(value, i, 4, out invalidEscape, out span);

            if (next == 'x')
                return TryValidateFixedLengthEscape(value, i, 2, out invalidEscape, out span);

            i += 1;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool TryValidateFixedLengthEscape(ReadOnlySpan<char> value, int start, int digitsLength,
        out string invalidEscape, out TextSpan span)
    {
        var availableDigits = Math.Min(digitsLength, value.Length - (start + 2));

        if (availableDigits == 0)
        {
            invalidEscape = string.Empty;
            span = TextSpan.Empty;
            return false;
        }

        if (availableDigits < digitsLength)
        {
            var invalidLength = Math.Min(2 + availableDigits, value.Length - start);
            invalidEscape = value.Slice(start, invalidLength).ToString();
            span = new TextSpan(start, invalidLength);
            return true;
        }

        for (var i = 0; i < digitsLength; i++)
        {
            if (Uri.IsHexDigit(value[start + 2 + i]))
                continue;

            invalidEscape = value.Slice(start, 2 + digitsLength).ToString();
            span = new TextSpan(start, 2 + digitsLength);
            return true;
        }

        invalidEscape = string.Empty;
        span = TextSpan.Empty;
        return false;
    }

    private static bool IsSimpleEscape(char value)
    {
        return value is '\\' or '\'' or '"' or 'n' or 'r' or 't' or 'b' or 'f' or 'e' or '0';
    }
}
