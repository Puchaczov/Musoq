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
                var end = Position + 2;
                while (end < Input.Length && IsHexDigit(Input[end]))
                    end++;

                if (end > Position + 2)
                {
                    Position = end;
                    return AssignToken(new HexIntegerToken(Input[start..end], new TextSpan(start, end - start)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1006_InvalidHexNumber, "hexadecimal", "0x");
            }

            if (next == 'b')
            {
                var end = Position + 2;
                while (end < Input.Length && (Input[end] == '0' || Input[end] == '1'))
                    end++;

                if (end > Position + 2)
                {
                    Position = end;
                    return AssignToken(new BinaryIntegerToken(Input[start..end], new TextSpan(start, end - start)));
                }

                return HandleInvalidBaseNumber(start, DiagnosticCode.MQ1007_InvalidBinaryNumber, "binary", "0b");
            }

            if (next == 'o')
            {
                var end = Position + 2;
                while (end < Input.Length && Input[end] is >= '0' and <= '7')
                    end++;

                if (end > Position + 2)
                {
                    Position = end;
                    return AssignToken(new OctalIntegerToken(Input[start..end], new TextSpan(start, end - start)));
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
        var end = start + 1;
        while (end < Input.Length)
        {
            var current = Input[end];
            if (current == '\\')
            {
                end += Math.Min(2, Input.Length - end);
                continue;
            }

            if (current == '\'')
            {
                var innerText = Input.AsSpan(start + 1, end - start - 1);
                var length = end - start + 1;

                if (TryFindInvalidEscapeSequence(innerText, out var invalidEscape, out var invalidEscapeSpan))
                {
                    var absoluteSpan = new TextSpan(start + 1 + invalidEscapeSpan.Start, invalidEscapeSpan.Length);
                    var message = $"Invalid escape sequence '{invalidEscape}'.";

                    if (RecoverOnError)
                        Diagnostics.AddError(DiagnosticCode.MQ1004_InvalidEscapeSequence, message, absoluteSpan);
                    else
                        throw new LexerException(message, absoluteSpan.Start, DiagnosticCode.MQ1004_InvalidEscapeSequence);
                }

                Position = end + 1;
                var unescaped = innerText.ToString().Unescape();
                return AssignToken(new StringLiteralToken(unescaped, new TextSpan(start, length)));
            }

            end++;
        }

        if (Position + 1 < Input.Length && Input[Position + 1] == '\'')
        {
            Position += 2;
            return AssignToken(new StringLiteralToken(string.Empty, new TextSpan(start, 2)));
        }


        if (RecoverOnError)
        {
            var recoveryEnd = Position + 1;
            while (recoveryEnd < Input.Length && Input[recoveryEnd] != '\n' && Input[recoveryEnd] != '\r')
                recoveryEnd++;

            var span = new TextSpan(start, recoveryEnd - start);
            Diagnostics.AddError(DiagnosticCode.MQ1002_UnterminatedString,
                "Unterminated string literal: missing closing '", span);

            Position = recoveryEnd;
            return AssignToken(new ErrorToken(Input[start..recoveryEnd], span));
        }

        throw new LexerException("Unterminated string literal: missing closing '", start, DiagnosticCode.MQ1002_UnterminatedString);
    }

}
