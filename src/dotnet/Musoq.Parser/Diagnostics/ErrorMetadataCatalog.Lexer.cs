using System.Collections.Generic;
using static Musoq.Parser.Diagnostics.ErrorMetadataCatalog;

namespace Musoq.Parser.Diagnostics;

internal static class LexerErrorMetadataCatalog
{
    public static IEnumerable<ErrorMetadata> Build()
    {
        yield return Entry(
            DiagnosticCode.MQ1001_UnknownToken,
            "The lexer encountered a character that is not part of Musoq SQL syntax.",
            [
                "Remove or replace the unrecognized character.",
                "If this is a string literal, wrap it in single quotes: 'value'."
            ],
            "Core Spec - Lexical Structure");

        yield return Entry(
            DiagnosticCode.MQ1002_UnterminatedString,
            "A string literal was opened with a single quote but never closed.",
            ["Add a closing single quote to the string literal."],
            "Core Spec - String Literals");

        yield return Entry(
            DiagnosticCode.MQ1003_InvalidNumericLiteral,
            "The numeric literal format is not valid.",
            [
                "Check for misplaced decimal points or invalid digit characters.",
                "For hex use 0x prefix, for binary use 0b, for octal use 0o."
            ],
            "Core Spec - Numeric Literals");

        yield return Entry(
            DiagnosticCode.MQ1004_InvalidEscapeSequence,
            "The string literal contains a malformed fixed-length escape sequence. Supported escapes are decoded; unknown escapes such as \\q remain literal.",
            [
                "Use a supported escape such as \\n, \\r, \\t, \\', \\\\, \\uFFFF, or \\xFF.",
                "For Windows paths, use a raw literal such as r'C:\\Path\\To\\File' or double each backslash as 'C:\\\\Path\\\\To\\\\File'."
            ],
            "Core Spec - String Literals");

        yield return Entry(
            DiagnosticCode.MQ1005_UnterminatedBlockComment,
            "A block comment was opened but the lexer reached the end of the script before finding a closing marker.",
            [
                "Close the block comment with */.",
                "Remove the opening /* if the text should be query syntax."
            ],
            "Core Spec - Comments");

        yield return Entry(
            DiagnosticCode.MQ1006_InvalidHexNumber,
            "A hexadecimal numeric literal contains characters that are not valid hexadecimal digits.",
            [
                "Use only digits 0-9 and letters A-F after the 0x prefix.",
                "Remove separators or suffixes that are not part of the literal format."
            ],
            "Core Spec - Numeric Literals");

        yield return Entry(
            DiagnosticCode.MQ1007_InvalidBinaryNumber,
            "A binary numeric literal contains characters other than 0 or 1.",
            [
                "Use only 0 and 1 after the 0b prefix.",
                "Convert the value to decimal if the source format is not binary."
            ],
            "Core Spec - Numeric Literals");

        yield return Entry(
            DiagnosticCode.MQ1008_InvalidOctalNumber,
            "An octal numeric literal contains digits outside the range 0 through 7.",
            [
                "Use only digits 0-7 after the 0o prefix.",
                "Convert the value to decimal if it needs digits 8 or 9."
            ],
            "Core Spec - Numeric Literals");

        yield return Entry(
            DiagnosticCode.MQ1009_NumericLiteralOutOfRange,
            "A numeric literal is syntactically valid but cannot be represented by a supported Musoq numeric type.",
            [
                "Use a smaller value or a supported numeric representation.",
                "If the value is intended as text, enclose it in a string literal."
            ],
            "Core Spec - Numeric Literals");
    }
}
