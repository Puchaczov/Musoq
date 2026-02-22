using System;
using System.Text;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Formats diagnostics for display in various output formats.
/// </summary>
public sealed class DiagnosticFormatter
{
    /// <summary>
    ///     Gets or sets whether to include context snippets.
    /// </summary>
    public bool IncludeContextSnippet { get; set; } = true;

    /// <summary>
    ///     Gets or sets whether to use ANSI color codes.
    /// </summary>
    public bool UseColor { get; set; } = false;

    /// <summary>
    ///     Gets or sets the number of context lines to show around the error.
    /// </summary>
    public int ContextLines { get; set; } = 2;

    /// <summary>
    ///     Formats a diagnostic for display.
    /// </summary>
    public string Format(Diagnostic diagnostic)
    {
        var sb = new StringBuilder();


        if (!string.IsNullOrEmpty(diagnostic.Location.FilePath))
        {
            sb.Append(diagnostic.Location.FilePath);
            sb.Append('(');
            sb.Append(diagnostic.Location.Line);
            sb.Append(',');
            sb.Append(diagnostic.Location.Column);
            sb.Append("): ");
        }
        else
        {
            sb.Append($"({diagnostic.Location.Line},{diagnostic.Location.Column}): ");
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
        sb.Append(diagnostic.Message);


        if (IncludeContextSnippet && !string.IsNullOrEmpty(diagnostic.ContextSnippet))
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(FormatContextSnippet(diagnostic));
        }


        if (diagnostic.SuggestedFixes.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Suggested fixes:");
            foreach (var fix in diagnostic.SuggestedFixes)
            {
                sb.Append("  - ");
                sb.AppendLine(fix.Title);
            }
        }

        return sb.ToString();
    }

    private string FormatContextSnippet(Diagnostic diagnostic)
    {
        var sb = new StringBuilder();
        var lines = diagnostic.ContextSnippet?.Split('\n') ?? Array.Empty<string>();


        var errorLine = diagnostic.Location.Line;
        var startLine = Math.Max(1, errorLine - ContextLines);

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNum = startLine + i;
            var isErrorLine = lineNum == errorLine;


            var lineNumStr = lineNum.ToString().PadLeft(5);
            if (UseColor && isErrorLine) sb.Append(GetColorCode(diagnostic.Severity));

            sb.Append(isErrorLine ? " --> " : "     ");
            sb.Append(lineNumStr);
            sb.Append(" | ");
            sb.Append(lines[i].TrimEnd('\r'));

            if (UseColor && isErrorLine) sb.Append("\u001b[0m");

            sb.AppendLine();


            if (isErrorLine)
            {
                var column = diagnostic.Location.Column;
                var endColumn = diagnostic.EndLocation.Column > 0 ? diagnostic.EndLocation.Column : column + 1;
                var length = Math.Max(1, endColumn - column);

                sb.Append("           | ");
                sb.Append(new string(' ', Math.Max(0, column - 1)));

                if (UseColor) sb.Append(GetColorCode(diagnostic.Severity));

                sb.Append(new string('^', length));

                if (UseColor) sb.Append("\u001b[0m");

                sb.AppendLine();
            }
        }

        return sb.ToString();
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
        var sb = new StringBuilder();
        sb.Append('{');
        sb.Append(
            $"\"range\":{{\"start\":{{\"line\":{diagnostic.Location.Line0},\"character\":{diagnostic.Location.Column0}}}");


        sb.Append(
            $",\"end\":{{\"line\":{diagnostic.EndLocation.Line0},\"character\":{diagnostic.EndLocation.Column0}}}");

        sb.Append("},");
        sb.Append($"\"severity\":{(int)diagnostic.Severity},");
        sb.Append($"\"code\":\"{diagnostic.Code}\",");
        sb.Append($"\"message\":\"{EscapeJson(diagnostic.Message)}\"");
        sb.Append('}');

        return sb.ToString();
    }

    private static string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }
}
