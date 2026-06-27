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
        ArgumentNullException.ThrowIfNull(envelope);
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

        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"{envelope.CodeString} [{severity}] [{phase}]");
        sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Message: {envelope.Message}");

        if (envelope is { Line: not null, Column: not null })
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"At: line {envelope.Line.Value}, column {envelope.Column.Value}");
        else if (envelope.Line.HasValue)
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"At: line {envelope.Line.Value}");
        else
            sb.AppendLine("At: runtime");

        if (!string.IsNullOrEmpty(envelope.Snippet))
        {
            sb.AppendLine("Snippet:");
            foreach (var line in envelope.Snippet.Split('\n'))
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  {line.TrimEnd('\r')}");
        }

        if (!string.IsNullOrEmpty(envelope.Explanation))
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Why: {envelope.Explanation}");

        if (envelope.SuggestedFixes.Count > 0)
        {
            sb.AppendLine("Try:");
            for (var i = 0; i < envelope.SuggestedFixes.Count; i++)
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"  {i + 1}) {envelope.SuggestedFixes[i]}");
        }

        if (!string.IsNullOrEmpty(envelope.DocsReference))
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Docs: {envelope.DocsReference}");

        if (!string.IsNullOrEmpty(envelope.Details))
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Details: {envelope.Details}");

        return sb.ToString().TrimEnd('\r', '\n');
    }

    /// <summary>
    ///     Formats an envelope as JSON for IDE/server integrations.
    /// </summary>
    public static string FormatJson(MusoqErrorEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        var sb = new StringBuilder();
        sb.Append('{');

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"code\":\"{envelope.CodeString}\"");

        var severity = envelope.Severity switch
        {
            DiagnosticSeverity.Error => "error",
            DiagnosticSeverity.Warning => "warning",
            DiagnosticSeverity.Info => "info",
            DiagnosticSeverity.Hint => "hint",
            _ => "error"
        };
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"severity\":\"{EscapeJson(severity)}\"");

        var phase = DiagnosticPhaseMapping.ToDisplayString(envelope.Phase);
        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"phase\":\"{EscapeJson(phase)}\"");

        sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"message\":\"{EscapeJson(envelope.Message)}\"");

        if (envelope.Line.HasValue || envelope.Column.HasValue || envelope.Length.HasValue)
        {
            sb.Append(",\"location\":{");
            var first = true;
            if (envelope.Line.HasValue)
            {
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"line\":{envelope.Line.Value}");
                first = false;
            }

            if (envelope.Column.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"column\":{envelope.Column.Value}");
                first = false;
            }

            if (envelope.Length.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"length\":{envelope.Length.Value}");
            }

            sb.Append('}');
        }

        if (!string.IsNullOrEmpty(envelope.Explanation))
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"why\":\"{EscapeJson(envelope.Explanation)}\"");

        if (envelope.SuggestedFixes.Count > 0)
        {
            sb.Append(",\"hints\":[");
            for (var i = 0; i < envelope.SuggestedFixes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"{EscapeJson(envelope.SuggestedFixes[i])}\"");
            }

            sb.Append(']');
        }

        if (!string.IsNullOrEmpty(envelope.DocsReference))
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"docs\":\"{EscapeJson(envelope.DocsReference)}\"");

        if (!string.IsNullOrEmpty(envelope.Details))
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"details\":\"{EscapeJson(envelope.Details)}\"");

        sb.Append('}');
        return sb.ToString();
    }

    private static string EscapeJson(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal);
    }
}
