using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a property cannot be found on a type.
/// </summary>
public class UnknownPropertyException : Exception, IDiagnosticException
{

    public UnknownPropertyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public UnknownPropertyException()
    {
    }
    /// <summary>
    ///     Initializes a new instance of UnknownPropertyException.
    /// </summary>
    public UnknownPropertyException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3028_UnknownProperty;
    }

    /// <summary>
    ///     Initializes a new instance of UnknownPropertyException with property information.
    /// </summary>
    public UnknownPropertyException(string propertyName, string typeName, TextSpan span)
        : base($"Property '{propertyName}' not found on type '{typeName}'")
    {
        PropertyName = propertyName;
        TypeName = typeName;
        Code = DiagnosticCode.MQ3028_UnknownProperty;
        Span = span;
    }

    /// <summary>
    ///     Initializes an unknown property error with bounded candidate facts.
    /// </summary>
    public UnknownPropertyException(
        string propertyName,
        string typeName,
        TextSpan span,
        IEnumerable<string> candidates)
        : this(propertyName, typeName, span)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        AvailableProperties = NormalizeCandidates(candidates);
        var closeCandidates = ErrorCatalog.GetDidYouMeanCandidates(propertyName, AvailableProperties);
        Suggestion = closeCandidates.Count == 1 ? closeCandidates[0] : null;
    }

    /// <summary>
    ///     Gets the name of the property that was not found.
    /// </summary>
    public string? PropertyName { get; }

    /// <summary>
    ///     Gets the name of the type on which the property was searched.
    /// </summary>
    public string? TypeName { get; }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Gets the candidate properties captured while binding the reference.
    /// </summary>
    public IReadOnlyList<string> AvailableProperties { get; } = [];

    /// <summary>
    ///     Gets the unique close spelling candidate, when one exists.
    /// </summary>
    public string? Suggestion { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!string.IsNullOrWhiteSpace(PropertyName))
            arguments["property"] = PropertyName;
        if (!string.IsNullOrWhiteSpace(TypeName))
            arguments["objectType"] = TypeName;
        if (AvailableProperties.Count > 0)
            arguments["availableProperties"] = string.Join(", ", AvailableProperties);
        if (!string.IsNullOrWhiteSpace(Suggestion))
            arguments["suggestion"] = Suggestion;

        var message = string.IsNullOrWhiteSpace(Suggestion)
            ? Message
            : $"{Message}. Did you mean '{Suggestion}'?";
        var replacementSpan = GetIdentifierSpan(sourceText, span, PropertyName);
        var suggestedFixes = string.IsNullOrWhiteSpace(Suggestion) ||
                             replacementSpan.IsEmpty ||
                             PropertyName == null ||
                             replacementSpan.Length != PropertyName.Length
            ? null
            : new[]
            {
                DiagnosticAction.QuickFix(
                    $"Replace '{PropertyName}' with '{Suggestion}'",
                    replacementSpan,
                    Suggestion)
            };

        return SemanticDiagnosticFactory.Create(Code, message, Span, sourceText, arguments, suggestedFixes);
    }

    private static string[] NormalizeCandidates(IEnumerable<string> candidates)
    {
        var canonicalCandidates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (!canonicalCandidates.TryGetValue(candidate, out var existing) ||
                string.CompareOrdinal(candidate, existing) < 0)
                canonicalCandidates[candidate] = candidate;
        }

        return canonicalCandidates.Values
            .OrderBy(static candidate => candidate, StringComparer.Ordinal)
            .ToArray();
    }

    private static TextSpan GetIdentifierSpan(SourceText? sourceText, TextSpan nodeSpan, string? identifier)
    {
        if (sourceText == null || string.IsNullOrWhiteSpace(identifier) || nodeSpan.IsEmpty)
            return nodeSpan;

        var start = Math.Max(0, nodeSpan.Start);
        var end = Math.Min(sourceText.Length, nodeSpan.End);
        if (start >= end)
            return nodeSpan;

        var index = sourceText.Text.IndexOf(
            identifier,
            start,
            end - start,
            StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? new TextSpan(index, identifier.Length) : nodeSpan;
    }
}
