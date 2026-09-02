using System.Collections.Generic;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when an inline VALUES source cannot be bound as a strongly typed table.
/// </summary>
public sealed class ValuesSourceException : Exception, IDiagnosticException
{
    public ValuesSourceException(
        string message,
        TextSpan span,
        IReadOnlyDictionary<string, string>? arguments = null,
        IReadOnlyList<DiagnosticAction>? suggestedFixes = null)
        : base(message)
    {
        Span = span;
        Arguments = arguments is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(arguments, StringComparer.Ordinal);
        SuggestedFixes = suggestedFixes is null ? [] : [..suggestedFixes];
    }

    public ValuesSourceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ValuesSourceException(string message)
        : base(message)
    {
    }

    public ValuesSourceException()
    {
    }

    public DiagnosticCode Code { get; } = DiagnosticCode.MQ3055_InvalidValuesSource;

    public TextSpan? Span { get; }

    public IReadOnlyDictionary<string, string> Arguments { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public IReadOnlyList<DiagnosticAction> SuggestedFixes { get; } = [];

    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var (location, endLocation) = sourceText is null
            ? (new SourceLocation(span.Start, 1, span.Start + 1), new SourceLocation(span.End, 1, span.End + 1))
            : sourceText.GetLocations(span);
        var diagnostic = new Diagnostic(
            Code,
            DiagnosticSeverity.Error,
            Message,
            location,
            endLocation,
            sourceText?.GetContextSnippet(span));
        foreach (var argument in Arguments)
            diagnostic = diagnostic.WithArgument(argument.Key, argument.Value);
        foreach (var suggestedFix in SuggestedFixes)
            diagnostic = diagnostic.WithSuggestedFix(suggestedFix);
        return diagnostic;
    }
}
