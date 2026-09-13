using System.Globalization;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Formats diagnostics for display in various output formats.
/// </summary>
public sealed class DiagnosticFormatter
{
    private const int TerminalTabWidth = 4;

    /// <summary>
    ///     Gets or sets whether to include context snippets.
    /// </summary>
    public bool IncludeContextSnippet { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to use ANSI color codes.
    /// </summary>
    public bool UseColor { get; set; }

    /// <summary>
    ///     Gets or sets the number of context lines to show around the error.
    /// </summary>
    public int ContextLines { get; set; } = 2;

    /// <summary>
    ///     Formats a diagnostic for display.
    /// </summary>
    public string Format(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var sb = new StringBuilder();


        if (!diagnostic.Location.IsValid)
        {
            sb.Append("(unknown): ");
        }
        else if (!string.IsNullOrEmpty(diagnostic.Location.FilePath))
        {
            sb.Append(DiagnosticSafety.SanitizeForDisplay(diagnostic.Location.FilePath));
            sb.Append('(');
            sb.Append(diagnostic.Location.Line);
            sb.Append(',');
            sb.Append(diagnostic.Location.Column);
            sb.Append("): ");
        }
        else
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"({diagnostic.Location.Line},{diagnostic.Location.Column}): ");
        }


        var severityText = FormatSeverity(diagnostic.Severity);
        if (UseColor)
        {
            sb.Append(GetColorCode(diagnostic.Severity));
            sb.Append(severityText);
            sb.Append("\u001b[0m");
        }
        else
        {
            sb.Append(severityText);
        }


        sb.Append(' ');
        sb.Append(diagnostic.Code.ToString());


        sb.Append(": ");
        sb.Append(DiagnosticSafety.SanitizeMessage(diagnostic));


        var safeSnippet = DiagnosticSafety.GetSafeSnippet(diagnostic);
        if (IncludeContextSnippet && !string.IsNullOrEmpty(safeSnippet))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(FormatContextSnippet(diagnostic, safeSnippet));
        }


        if (diagnostic.SuggestedFixes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            foreach (var fix in diagnostic.SuggestedFixes)
            {
                sb.Append("  - ");
                sb.AppendLine(DiagnosticSafety.SanitizeForDisplay(
                    DiagnosticSafety.SanitizeText(fix.Title, diagnostic)));
            }
        }

        return sb.ToString();
    }

    private string FormatContextSnippet(Diagnostic diagnostic, string safeSnippet)
    {
        var sb = new StringBuilder();
        var lines = safeSnippet.Split('\n');


        var errorLine = diagnostic.Location.Line;
        var startLine = Math.Max(1, errorLine - ContextLines);
        var renderedLineIndex = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            if (IsContextPointerLine(lines[i]))
                continue;

            var lineNum = startLine + renderedLineIndex++;
            var isErrorLine = lineNum == errorLine;
            var sourceLine = ExtractSourceLine(lines[i]);
            var displayLine = DiagnosticSafety.SanitizeForDisplay(ToDisplaySafeLine(sourceLine));


            var lineNumStr = lineNum.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(5);
            if (UseColor && isErrorLine) sb.Append(GetColorCode(diagnostic.Severity));

            sb.Append(isErrorLine ? " --> " : "     ");
            sb.Append(lineNumStr);
            sb.Append(" | ");
            sb.Append(displayLine);

            if (UseColor && isErrorLine) sb.Append("\u001b[0m");

            sb.AppendLine();


            if (isErrorLine)
            {
                var column = diagnostic.Location.Column;
                var endColumn = diagnostic.EndLocation.Column > 0 ? diagnostic.EndLocation.Column : column + 1;
                var startIndex = Math.Max(0, Math.Min(sourceLine.Length, column - 1));
                var endIndex = Math.Max(startIndex, Math.Min(sourceLine.Length, endColumn - 1));
                var displayColumn = GetDisplayWidth(sourceLine, startIndex) + 1;
                var displayEndColumn = GetDisplayWidth(sourceLine, endIndex) + 1;
                var length = Math.Max(1, displayEndColumn - displayColumn);

                sb.Append("           | ");
                sb.Append(new string(' ', displayColumn - 1));

                if (UseColor) sb.Append(GetColorCode(diagnostic.Severity));

                sb.Append(new string('^', length));

                if (UseColor) sb.Append("\u001b[0m");

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static bool IsContextPointerLine(string line)
    {
        if (line.Contains(" | ", StringComparison.Ordinal))
            return false;

        var trimmed = line.Trim();
        return trimmed.Length > 0 && trimmed.All(static character => character is ' ' or '^');
    }

    private static string ExtractSourceLine(string line)
    {
        var separator = line.IndexOf(" | ", StringComparison.Ordinal);
        if (separator > 0 && int.TryParse(line[..separator].Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out _))
            return line[(separator + 3)..].TrimEnd('\r');

        return line.TrimEnd('\r');
    }

    private static string ToDisplaySafeLine(string sourceLine)
    {
        var builder = new StringBuilder(sourceLine.Length);
        var index = 0;
        var displayWidth = 0;

        while (index < sourceLine.Length)
        {
            var character = sourceLine[index];
            if (character == '\t')
            {
                var spaces = TerminalTabWidth - displayWidth % TerminalTabWidth;
                builder.Append(' ', spaces);
                displayWidth += spaces;
                index++;
                continue;
            }

            if (DiagnosticSafety.IsUnsafeDisplayCharacter(character))
            {
                var escape = DiagnosticSafety.EscapeUnsafeCharacter(character);
                builder.Append(escape);
                displayWidth += escape.Length;
                index++;
                continue;
            }

            var consumed = GetScalarLength(sourceLine, index);
            builder.Append(sourceLine, index, consumed);
            displayWidth += GetScalarDisplayWidth(sourceLine, index);
            index += consumed;
        }

        return builder.ToString();
    }

    private static int GetDisplayWidth(string sourceLine, int sourceLength)
    {
        var index = 0;
        var displayWidth = 0;

        while (index < sourceLength)
        {
            var character = sourceLine[index];
            if (character == '\t')
            {
                displayWidth += TerminalTabWidth - displayWidth % TerminalTabWidth;
                index++;
                continue;
            }

            if (DiagnosticSafety.IsUnsafeDisplayCharacter(character))
            {
                displayWidth += DiagnosticSafety.EscapeUnsafeCharacter(character).Length;
                index++;
                continue;
            }

            displayWidth += GetScalarDisplayWidth(sourceLine, index);
            index += GetScalarLength(sourceLine, index);
        }

        return displayWidth;
    }

    private static int GetScalarLength(string text, int index)
    {
        return index + 1 < text.Length && char.IsHighSurrogate(text[index]) &&
               char.IsLowSurrogate(text[index + 1])
            ? 2
            : 1;
    }

    private static int GetScalarDisplayWidth(string text, int index)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(text, index);
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or
            UnicodeCategory.EnclosingMark)
            return 0;

        var scalar = (int)text[index];
        if (index + 1 < text.Length && char.IsHighSurrogate(text[index]) &&
            char.IsLowSurrogate(text[index + 1]))
            scalar = char.ConvertToUtf32(text[index], text[index + 1]);

        return IsWideScalar(scalar) ? 2 : 1;
    }

    private static bool IsWideScalar(int scalar)
    {
        return scalar is >= 0x1100 and <= 0x115F or
            >= 0x2329 and <= 0x232A or
            >= 0x2E80 and <= 0x303E or
            >= 0x3040 and <= 0xA4CF or
            >= 0xAC00 and <= 0xD7A3 or
            >= 0xF900 and <= 0xFAFF or
            >= 0xFE10 and <= 0xFE19 or
            >= 0xFE30 and <= 0xFE6F or
            >= 0xFF00 and <= 0xFF60 or
            >= 0xFFE0 and <= 0xFFE6 or
            >= 0x1F300 and <= 0x1FAFF or
            >= 0x20000 and <= 0x3FFFD;
    }

    private static string FormatSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => "unknown"
        };
    }

    private static string GetColorCode(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Error => "\u001b[31m",
            DiagnosticSeverity.Warning => "\u001b[33m",
            DiagnosticSeverity.Info => "\u001b[36m",
            DiagnosticSeverity.Hint => "\u001b[90m",
            _ => ""
        };
    }

    /// <summary>
    ///     Formats diagnostics for JSON output (LSP-compatible).
    /// </summary>
    public string FormatAsJson(Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        var sb = new StringBuilder();
        sb.Append('{');
        if (diagnostic.Location.IsValid && diagnostic.EndLocation.IsValid)
        {
            sb.Append(System.Globalization.CultureInfo.InvariantCulture,
                $"\"range\":{{\"start\":{{\"line\":{diagnostic.Location.Line0},\"character\":{diagnostic.Location.Column0}}}");
            sb.Append(System.Globalization.CultureInfo.InvariantCulture,
                $",\"end\":{{\"line\":{diagnostic.EndLocation.Line0},\"character\":{diagnostic.EndLocation.Column0}}}");
            sb.Append("},");
        }
        else
        {
            sb.Append("\"range\":null,");
        }

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"severity\":{(int)diagnostic.Severity},");
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"code\":\"{diagnostic.Code}\",");
        sb.Append(System.Globalization.CultureInfo.InvariantCulture,
            $"\"message\":\"{EscapeJson(DiagnosticSafety.SanitizeMessageForJson(diagnostic))}\"");
        sb.Append('}');

        return sb.ToString();
    }

    private static string EscapeJson(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            switch (character)
            {
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\"':
                    builder.Append("\\\"");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (DiagnosticSafety.IsUnsafeDisplayCharacter(character))
                    {
                        builder.Append(DiagnosticSafety.EscapeUnsafeCharacter(character));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }
}
