#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a column must be marked as BindablePropertyAsTable.
/// </summary>
public class ColumnMustBeMarkedAsBindablePropertyAsTableException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    public ColumnMustBeMarkedAsBindablePropertyAsTableException()
        : base("Column must be marked as BindablePropertyAsTable")
    {
        Code = DiagnosticCode.MQ3026_ColumnNotBindable;
    }

    /// <summary>
    ///     Initializes a new instance with column name and span.
    /// </summary>
    public ColumnMustBeMarkedAsBindablePropertyAsTableException(string columnName, TextSpan span)
        : base($"Column '{columnName}' must be marked as BindablePropertyAsTable")
    {
        ColumnName = columnName;
        Code = DiagnosticCode.MQ3026_ColumnNotBindable;
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
        return Diagnostic.Error(Code, Message, span);
    }
}
