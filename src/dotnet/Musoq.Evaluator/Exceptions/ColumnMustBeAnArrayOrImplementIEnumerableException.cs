using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a column must be an array or implement IEnumerable.
/// </summary>
public class ColumnMustBeAnArrayOrImplementIEnumerableException : Exception, IDiagnosticException
{

    public ColumnMustBeAnArrayOrImplementIEnumerableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ColumnMustBeAnArrayOrImplementIEnumerableException(string message)
        : base(message)
    {
    }
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public ColumnMustBeAnArrayOrImplementIEnumerableException()
        : base("Column must be an array or implement IEnumerable<T> interface")
    {
        Code = DiagnosticCode.MQ3025_ColumnMustBeArray;
    }

    /// <summary>
    ///     Initializes a new instance with column name and span.
    /// </summary>
    public ColumnMustBeAnArrayOrImplementIEnumerableException(string columnName, TextSpan span)
        : base($"Column '{columnName}' must be an array or implement IEnumerable<T> interface")
    {
        ColumnName = columnName;
        Code = DiagnosticCode.MQ3025_ColumnMustBeArray;
        Span = span;
    }

    /// <summary>
    ///     Gets the column name.
    /// </summary>
    public string? ColumnName { get; }

    /// <summary>
    ///     Gets the diagnostic code for this exception.
    /// </summary>
    public DiagnosticCode Code { get; }

    /// <summary>
    ///     Gets the source location span where this error occurred.
    /// </summary>
    public TextSpan? Span { get; }

    /// <summary>
    ///     Converts this exception to a Diagnostic instance.
    /// </summary>
    public Diagnostic ToDiagnostic(SourceText? sourceText = null)
    {
        var span = Span ?? TextSpan.Empty;
        var metadata = ErrorMetadataCatalog.Get(Code);
        var (location, endLocation) = span.IsEmpty
            ? (SourceLocation.None, SourceLocation.None)
            : sourceText is null
                ? (new SourceLocation(span.Start, 1, span.Start + 1),
                    new SourceLocation(span.End, 1, span.End + 1))
                : sourceText.GetLocations(span);
        var diagnostic = new Diagnostic(
            Code,
            ErrorCatalog.GetDefaultSeverity(Code),
            Message,
            location,
            endLocation,
            span.IsEmpty ? null : sourceText?.GetContextSnippet(span),
            suggestedFixes: metadata?.SuggestedFixes.Select(DiagnosticAction.Suggestion),
            explanation: metadata?.Explanation,
            docsReference: metadata?.DocsReference,
            phase: metadata?.Phase);

        if (!string.IsNullOrWhiteSpace(ColumnName))
            diagnostic = diagnostic.WithArgument("column", ColumnName);

        return diagnostic;
    }
}
