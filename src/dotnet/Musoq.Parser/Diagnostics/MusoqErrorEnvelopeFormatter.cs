using System.Text;
using System.Linq;

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

        if (envelope.SourceKind != DiagnosticSourceKind.Query)
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"Source: {FormatSourceKind(envelope.SourceKind)}");

        if (envelope.Offset.HasValue)
        {
            sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                $"Span: offset {envelope.Offset.Value}, length {envelope.Length ?? 0}");
        }

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

        if (envelope.Arguments.Count > 0)
        {
            sb.AppendLine("Facts:");
            foreach (var argument in envelope.Arguments.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                    $"  {argument.Key}: {argument.Value}");
        }

        if (envelope.RelatedLocations.Count > 0)
        {
            sb.AppendLine("Related:");
            foreach (var related in envelope.RelatedLocations)
            {
                var message = string.IsNullOrEmpty(related.Message) ? string.Empty : $": {related.Message}";
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                    $"  {related.Location}{message}");
            }
        }

        if (envelope.Actions.Count > 0)
        {
            sb.AppendLine("Actions:");
            foreach (var action in envelope.Actions)
                sb.AppendLine(System.Globalization.CultureInfo.InvariantCulture,
                    $"  {FormatActionKind(action.Kind)}: {action.Title}");
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

        sb.Append(System.Globalization.CultureInfo.InvariantCulture,
            $",\"source\":\"{EscapeJson(FormatSourceKind(envelope.SourceKind))}\"");

        if (envelope.Line.HasValue || envelope.Column.HasValue || envelope.Length.HasValue ||
            envelope.Offset.HasValue || envelope.EndOffset.HasValue || envelope.EndLine.HasValue ||
            envelope.EndColumn.HasValue)
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
                first = false;
            }

            if (envelope.Offset.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"offset\":{envelope.Offset.Value}");
                first = false;
            }

            if (envelope.EndOffset.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"endOffset\":{envelope.EndOffset.Value}");
                first = false;
            }

            if (envelope.EndLine.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"endLine\":{envelope.EndLine.Value}");
                first = false;
            }

            if (envelope.EndColumn.HasValue)
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture, $"\"endColumn\":{envelope.EndColumn.Value}");
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

        if (envelope.Arguments.Count > 0)
        {
            sb.Append(",\"arguments\":{");
            var first = true;
            foreach (var argument in envelope.Arguments.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
            {
                if (!first) sb.Append(',');
                sb.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"\"{EscapeJson(argument.Key)}\":\"{EscapeJson(argument.Value)}\"");
                first = false;
            }

            sb.Append('}');
        }

        if (envelope.RelatedLocations.Count > 0)
        {
            sb.Append(",\"related\":[");
            for (var i = 0; i < envelope.RelatedLocations.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var related = envelope.RelatedLocations[i];
                sb.Append("{\"source\":\"");
                sb.Append(EscapeJson(FormatSourceKind(related.SourceKind)));
                sb.Append("\",\"offset\":");
                sb.Append(related.Location.IsValid ? related.Location.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null");
                sb.Append(",\"endOffset\":");
                sb.Append(related.EndLocation.IsValid ? related.EndLocation.Offset.ToString(System.Globalization.CultureInfo.InvariantCulture) : "null");
                if (!string.IsNullOrEmpty(related.Message))
                    sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"message\":\"{EscapeJson(related.Message)}\"");
                sb.Append('}');
            }

            sb.Append(']');
        }

        if (envelope.Actions.Count > 0)
        {
            sb.Append(",\"actions\":[");
            for (var i = 0; i < envelope.Actions.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var action = envelope.Actions[i];
                sb.Append(System.Globalization.CultureInfo.InvariantCulture,
                    $"{{\"title\":\"{EscapeJson(action.Title)}\",\"kind\":\"{EscapeJson(FormatActionKind(action.Kind))}\"");
                if (action.TextEdit != null)
                {
                    sb.Append(System.Globalization.CultureInfo.InvariantCulture,
                        $",\"edit\":{{\"start\":{action.TextEdit.Span.Start},\"length\":{action.TextEdit.Span.Length},\"newText\":\"{EscapeJson(action.TextEdit.NewText)}\"}}");
                }

                sb.Append('}');
            }

            sb.Append(']');
        }

        if (!string.IsNullOrEmpty(envelope.CorrelationId))
            sb.Append(System.Globalization.CultureInfo.InvariantCulture, $",\"correlationId\":\"{EscapeJson(envelope.CorrelationId)}\"");

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

    private static string FormatSourceKind(DiagnosticSourceKind sourceKind)
    {
        return sourceKind switch
        {
            DiagnosticSourceKind.Query => "query",
            DiagnosticSourceKind.GeneratedSource => "generated-source",
            DiagnosticSourceKind.Schema => "schema",
            DiagnosticSourceKind.DataSource => "datasource",
            DiagnosticSourceKind.Runtime => "runtime",
            DiagnosticSourceKind.Internal => "internal",
            _ => "unknown"
        };
    }

    private static string FormatActionKind(DiagnosticActionKind kind)
    {
        return kind switch
        {
            DiagnosticActionKind.QuickFix => "quick-fix",
            DiagnosticActionKind.Refactor => "refactor",
            DiagnosticActionKind.Suggestion => "suggestion",
            _ => "unknown"
        };
    }
}
