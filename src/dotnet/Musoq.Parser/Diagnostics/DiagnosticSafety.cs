using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;

namespace Musoq.Parser.Diagnostics;

/// <summary>
///     Applies the public diagnostic redaction policy at presentation
///     boundaries. Diagnostic producers should still avoid placing secret
///     values in structured facts; this helper protects the remaining source
///     and payload sinks when a value crosses that boundary.
/// </summary>
public static class DiagnosticSafety
{
    public const string RedactedValue = "[redacted]";
    public const int MaxDisplayLength = 512;
    public const int MaxSuggestedIdentifierLength = 128;

    /// <summary>
    ///     Escapes terminal control and bidirectional-format characters and
    ///     bounds untrusted text before it reaches a public renderer.
    /// </summary>
    public static string SanitizeForDisplay(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var result = new System.Text.StringBuilder(Math.Min(value.Length, MaxDisplayLength));
        foreach (var character in value)
        {
            var escaped = IsUnsafeDisplayCharacter(character)
                ? EscapeUnsafeCharacter(character)
                : character.ToString();
            var remaining = MaxDisplayLength - result.Length;
            if (escaped.Length > remaining)
            {
                if (remaining > 1)
                    result.Append('…');
                break;
            }

            result.Append(escaped);
        }

        return result.ToString();
    }

    /// <summary>
    ///     Quotes an untrusted identifier for diagnostic prose. This is a
    ///     display value only and must not be used as executable SQL.
    /// </summary>
    public static string QuoteForDisplay(string? value)
    {
        return $"'{SanitizeForDisplay(value).Replace("'", "''", StringComparison.Ordinal)}'";
    }

    /// <summary>
    ///     Returns whether a provider-supplied identifier is safe for a native
    ///     automatic replacement edit. Unsafe values may still be displayed,
    ///     but they do not become executable editor actions.
    /// </summary>
    public static bool IsSafeSuggestedIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxSuggestedIdentifierLength)
            return false;

        foreach (var character in value)
        {
            if (IsUnsafeDisplayCharacter(character) || char.IsWhiteSpace(character) ||
                character is '\'' or '"' or '`' or ';' or '\\' or '<' or '>' or '&' or '|')
                return false;

            if (character is '.' or '_' or '$' or '#')
                continue;

            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (!char.IsLetterOrDigit(character) && category is not
                    (UnicodeCategory.NonSpacingMark or UnicodeCategory.SpacingCombiningMark or
                    UnicodeCategory.ConnectorPunctuation) && !char.IsSurrogate(character))
                return false;
        }

        return true;
    }

    internal static bool IsUnsafeDisplayCharacter(char character)
    {
        return char.IsControl(character) || character is '\u007f' or '\u2028' or '\u2029' or
            >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u2069';
    }

    internal static string EscapeUnsafeCharacter(char character)
    {
        return character switch
        {
            '\b' => "\\b",
            '\f' => "\\f",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\v' => "\\v",
            _ => $"\\u{((int)character).ToString("x4", CultureInfo.InvariantCulture)}"
        };
    }

    public static string SanitizeMessage(Diagnostic diagnostic, string? queryText = null)
    {
        return SanitizeForDisplay(SanitizeText(diagnostic.Message, diagnostic, queryText));
    }

    /// <summary>
    ///     Returns a structured value suitable for JSON encoding. Trusted
    ///     diagnostic values retain their control characters so the JSON
    ///     encoder can round-trip them; provider-owned values are bounded first.
    /// </summary>
    public static string SanitizeForJson(
        string? value,
        DiagnosticSourceKind sourceKind)
    {
        var result = value ?? string.Empty;
        return sourceKind == DiagnosticSourceKind.DataSource
            ? BoundRaw(result)
            : result;
    }

    /// <summary>
    ///     Redacts a diagnostic message for JSON without converting control
    ///     characters into display escape text.
    /// </summary>
    public static string SanitizeMessageForJson(
        Diagnostic diagnostic,
        string? queryText = null)
    {
        return SanitizeForJson(
            SanitizeText(diagnostic.Message, diagnostic, queryText),
            diagnostic.SourceKind);
    }

    public static string? SanitizeText(
        string? value,
        Diagnostic diagnostic,
        string? queryText = null)
    {
        if (value == null)
            return null;

        var result = value;
        foreach (var sensitiveValue in GetSensitiveValues(diagnostic, queryText))
        {
            if (sensitiveValue.Length == 0)
                continue;

            result = result.Replace(sensitiveValue, RedactedValue, StringComparison.Ordinal);
        }

        return result;
    }

    public static string? GetSafeSnippet(Diagnostic diagnostic, string? queryText = null)
    {
        if (queryText != null && diagnostic.SourceKind == DiagnosticSourceKind.Query &&
            TryGetSourceSpan(diagnostic, queryText, out var source, out var span) &&
            IsPotentialLiteral(source.GetText(span)))
        {
            var masked = MaskSpan(source.Text, span);
            return new SourceText(masked).GetContextSnippet(span);
        }

        return diagnostic.ContextSnippet == null
            ? null
            : MaskContextSnippet(diagnostic, diagnostic.ContextSnippet);
    }

    public static IReadOnlyDictionary<string, string> SanitizeArguments(
        Diagnostic diagnostic,
        string? queryText = null)
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var argument in diagnostic.Arguments)
        {
            arguments[argument.Key] = IsSensitiveKey(argument.Key)
                ? RedactedValue
                : SanitizeText(argument.Value, diagnostic, queryText) ?? string.Empty;
        }

        return arguments;
    }

    public static IReadOnlyList<DiagnosticRelatedLocation> SanitizeRelatedLocations(
        Diagnostic diagnostic,
        string? queryText = null)
    {
        return diagnostic.RelatedLocations
            .Select(location => new DiagnosticRelatedLocation(
                location.Location,
                location.EndLocation,
                SanitizeText(location.Message, diagnostic, queryText),
                location.SourceKind))
            .ToArray();
    }

    public static IReadOnlyList<DiagnosticAction> SanitizeActions(
        Diagnostic diagnostic,
        string? queryText = null)
    {
        return diagnostic.SuggestedFixes
            .Select(action => new DiagnosticAction(
                SanitizeText(action.Title, diagnostic, queryText) ?? string.Empty,
                action.Kind,
                action.TextEdit == null
                    ? null
                    : new TextEdit(
                        action.TextEdit.Span,
                        SanitizeText(action.TextEdit.NewText, diagnostic, queryText) ?? string.Empty)))
            .ToArray();
    }

    private static IEnumerable<string> GetSensitiveValues(
        Diagnostic diagnostic,
        string? queryText)
    {
        if (queryText != null && diagnostic.SourceKind == DiagnosticSourceKind.Query &&
            TryGetSourceSpan(diagnostic, queryText, out var source, out var span))
        {
            var sourceValue = source.GetText(span);
            if (IsPotentialLiteral(sourceValue))
            {
                yield return sourceValue;
                var innerValue = GetLiteralContent(sourceValue);
                if (!string.IsNullOrEmpty(innerValue))
                    yield return innerValue;
            }

            yield break;
        }

        if (diagnostic.ContextSnippet == null)
            yield break;

        var selected = GetSelectedContextText(diagnostic, diagnostic.ContextSnippet);
        if (!IsPotentialLiteral(selected))
            yield break;

        yield return selected!;
        var selectedContent = GetLiteralContent(selected!);
        if (!string.IsNullOrEmpty(selectedContent))
            yield return selectedContent;
    }

    private static bool IsSensitiveKey(string key)
    {
        var normalized = key.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();

        return normalized.Contains("secret", StringComparison.Ordinal) ||
               normalized.Contains("password", StringComparison.Ordinal) ||
               normalized.Contains("token", StringComparison.Ordinal) ||
               normalized.Contains("credential", StringComparison.Ordinal) ||
               normalized.Contains("apikey", StringComparison.Ordinal) ||
               normalized.Contains("authorization", StringComparison.Ordinal) ||
               normalized.Contains("connectionstring", StringComparison.Ordinal) ||
               normalized.Contains("privatekey", StringComparison.Ordinal);
    }

    private static bool TryGetSourceSpan(
        Diagnostic diagnostic,
        string queryText,
        out SourceText source,
        out TextSpan span)
    {
        source = new SourceText(queryText);
        if (!diagnostic.Location.IsValid || diagnostic.Location.Offset < 0 ||
            diagnostic.Location.Offset > source.Length)
        {
            span = TextSpan.Empty;
            return false;
        }

        var endOffset = diagnostic.EndLocation.IsValid
            ? diagnostic.EndLocation.Offset
            : diagnostic.Location.Offset + 1;
        endOffset = Math.Clamp(endOffset, diagnostic.Location.Offset, source.Length);
        span = new TextSpan(diagnostic.Location.Offset, endOffset - diagnostic.Location.Offset);
        return span.Length > 0;
    }

    private static bool IsPotentialLiteral(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        var first = value[0];
        return first is '\'' or '"' ||
               ((first is 'r' or 'R') && value.Length > 1 && (value[1] is '\'' or '"'));
    }

    private static string GetLiteralContent(string value)
    {
        var start = value.Length > 1 && (value[0] is 'r' or 'R') && (value[1] is '\'' or '"') ? 2 : 1;
        if (value.Length <= start)
            return string.Empty;

        var end = value.Length > start && value[^1] == value[start - 1]
            ? value.Length - 1
            : value.Length;
        return value[start..end];
    }

    private static string MaskSpan(string text, TextSpan span)
    {
        var characters = text.ToCharArray();
        var start = Math.Clamp(span.Start, 0, characters.Length);
        var end = Math.Clamp(span.End, start, characters.Length);
        for (var index = start; index < end; index++)
            characters[index] = '*';

        return new string(characters);
    }

    private static string MaskContextSnippet(Diagnostic diagnostic, string snippet)
    {
        var values = GetSensitiveValues(diagnostic, queryText: null).ToArray();
        if (values.Length == 0)
            return snippet;

        var result = snippet;
        foreach (var value in values.Where(static value => value.Length > 0).OrderByDescending(static value => value.Length))
            result = result.Replace(value, new string('*', value.Length), StringComparison.Ordinal);

        return result;
    }

    private static string BoundRaw(string value)
    {
        if (value.Length <= MaxDisplayLength)
            return value;

        return value[..(MaxDisplayLength - 1)] + '…';
    }

    private static string? GetSelectedContextText(Diagnostic diagnostic, string snippet)
    {
        if (!diagnostic.Location.IsValid)
            return null;

        foreach (var line in snippet.Split('\n'))
        {
            var separator = line.IndexOf(" | ", StringComparison.Ordinal);
            if (separator < 0 || !int.TryParse(line[..separator].Trim(), out var lineNumber) ||
                lineNumber != diagnostic.Location.Line)
                continue;

            var sourceLine = line[(separator + 3)..].TrimEnd('\r');
            var start = Math.Clamp(diagnostic.Location.Column - 1, 0, sourceLine.Length);
            var endColumn = diagnostic.EndLocation.IsValid &&
                            diagnostic.EndLocation.Line == diagnostic.Location.Line
                ? diagnostic.EndLocation.Column
                : diagnostic.Location.Column + 1;
            var end = Math.Clamp(endColumn - 1, start, sourceLine.Length);
            return sourceLine[start..end];
        }

        return null;
    }
}
