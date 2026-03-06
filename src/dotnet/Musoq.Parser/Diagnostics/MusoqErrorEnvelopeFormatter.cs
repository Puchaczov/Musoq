using System;
using System.Text;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Formats <see cref="MusoqErrorEnvelope" /> instances for display in CLI text or JSON form.
/// </summary>
public static class MusoqErrorEnvelopeFormatter
{
    /// <summary>
    ///     Formats an envelope in the spec-defined text format:
    ///     <code>
    ///     MQ3022 [error] [bind]
    ///     Message: ...
    ///     At: line L, column C
    ///     Snippet:
    ///       ...
    ///     Why: ...
    ///     Try:
    ///       1) ...
    ///     Docs: ...
    ///     Details: ...
    ///     </code>
    /// </summary>
    public static string FormatText(MusoqErrorEnvelope envelope)
    {
        var sb = new StringBuilder();

        var severity = envelope.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => "error"
        };

        var phase = DiagnosticPhaseMapping.ToDisplayString(envelope.Phase);

        sb.AppendLine($"{envelope.CodeString} [{severity}] [{phase}]");
        sb.AppendLine($"Message: {envelope.Message}");

        if (envelope.Line.HasValue && envelope.Column.HasValue)
            sb.AppendLine($"At: line {envelope.Line.Value}, column {envelope.Column.Value}");
        else if (envelope.Line.HasValue)
            sb.AppendLine($"At: line {envelope.Line.Value}");
        else
            sb.AppendLine("At: runtime");

        if (!string.IsNullOrEmpty(envelope.Snippet))
        {
            sb.AppendLine("Snippet:");
            foreach (var line in envelope.Snippet.Split('\n'))
                sb.AppendLine($"  {line.TrimEnd('\r')}");
        }

        if (!string.IsNullOrEmpty(envelope.Explanation))
            sb.AppendLine($"Why: {envelope.Explanation}");

        if (envelope.SuggestedFixes.Count > 0)
        {
            sb.AppendLine("Try:");
            for (var i = 0; i < envelope.SuggestedFixes.Count; i++)
                sb.AppendLine($"  {i + 1}) {envelope.SuggestedFixes[i]}");
        }

        if (!string.IsNullOrEmpty(envelope.DocsReference))
            sb.AppendLine($"Docs: {envelope.DocsReference}");

        if (!string.IsNullOrEmpty(envelope.Details))
            sb.AppendLine($"Details: {envelope.Details}");

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    ///     Formats an envelope as JSON for IDE/server integrations.
    /// </summary>
    public static string FormatJson(MusoqErrorEnvelope envelope)
    {
        var sb = new StringBuilder();
        sb.Append('{');

        sb.Append($"\"code\":\"{envelope.CodeString}\"");

        var severity = envelope.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => "error"
        };
        sb.Append($",\"severity\":\"{EscapeJson(severity)}\"");

        var phase = DiagnosticPhaseMapping.ToDisplayString(envelope.Phase);
        sb.Append($",\"phase\":\"{EscapeJson(phase)}\"");

        sb.Append($",\"message\":\"{EscapeJson(envelope.Message)}\"");

        if (envelope.Line.HasValue || envelope.Column.HasValue || envelope.Length.HasValue)
        {
            sb.Append(",\"location\":{");
            var first = true;
            if (envelope.Line.HasValue)
            {
                sb.Append($"\"line\":{envelope.Line.Value}");
                first = false;
            }

            if (envelope.Column.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append($"\"column\":{envelope.Column.Value}");
                first = false;
            }

            if (envelope.Length.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append($"\"length\":{envelope.Length.Value}");
            }

            sb.Append('}');
        }

        if (!string.IsNullOrEmpty(envelope.Explanation))
            sb.Append($",\"why\":\"{EscapeJson(envelope.Explanation)}\"");

        if (envelope.SuggestedFixes.Count > 0)
        {
            sb.Append(",\"hints\":[");
            for (var i = 0; i < envelope.SuggestedFixes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append($"\"{EscapeJson(envelope.SuggestedFixes[i])}\"");
            }

            sb.Append(']');
        }

        if (!string.IsNullOrEmpty(envelope.DocsReference))
            sb.Append($",\"docs\":\"{EscapeJson(envelope.DocsReference)}\"");

        if (!string.IsNullOrEmpty(envelope.Details))
            sb.Append($",\"details\":\"{EscapeJson(envelope.Details)}\"");

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
