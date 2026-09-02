using System.Collections.Generic;
using Musoq.Evaluator;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a column reference is ambiguous between multiple aliases.
/// </summary>
public class AmbiguousColumnException : Exception, IDiagnosticException
{
    public AmbiguousColumnException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public AmbiguousColumnException(string message)
        : base(message)
    {
    }

    public AmbiguousColumnException()
    {
    }
    /// <summary>
    ///     Initializes a new instance of AmbiguousColumnException.
    /// </summary>
    public AmbiguousColumnException(string column, string alias1, string alias2)
        : base($"Ambiguous column name '{column}' between '{alias1}' and '{alias2}' aliases.")
    {
        ColumnName = column;
        Alias1 = alias1;
        Alias2 = alias2;
        Code = DiagnosticCode.MQ3002_AmbiguousColumn;
    }

    /// <summary>
    ///     Initializes a new instance of AmbiguousColumnException with location information.
    /// </summary>
    public AmbiguousColumnException(string column, string alias1, string alias2, TextSpan span)
        : base($"Ambiguous column name '{column}' between '{alias1}' and '{alias2}' aliases.")
    {
        ColumnName = column;
        Alias1 = alias1;
        Alias2 = alias2;
        Code = DiagnosticCode.MQ3002_AmbiguousColumn;
        Span = span;
    }

    /// <summary>
    ///     Gets the ambiguous column name.
    /// </summary>
    public string ColumnName { get; } = string.Empty;

    /// <summary>
    ///     Gets the first conflicting alias.
    /// </summary>
    public string Alias1 { get; } = string.Empty;

    /// <summary>
    ///     Gets the second conflicting alias.
    /// </summary>
    public string Alias2 { get; } = string.Empty;

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
        return SemanticDiagnosticFactory.Create(
            Code,
            Message,
            Span,
            sourceText,
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["column"] = ColumnName,
                ["aliases"] = $"{Alias1}, {Alias2}"
            });
    }
}
