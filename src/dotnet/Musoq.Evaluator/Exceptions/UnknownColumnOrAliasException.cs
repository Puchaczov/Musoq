#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a column or alias cannot be resolved.
/// </summary>
public class UnknownColumnOrAliasException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance of UnknownColumnOrAliasException.
    /// </summary>
    public UnknownColumnOrAliasException(string message)
        : base(message)
    {
        Code = DiagnosticCode.MQ3001_UnknownColumn;
    }

    /// <summary>
    ///     Initializes a new instance of UnknownColumnOrAliasException with diagnostic information.
    /// </summary>
    public UnknownColumnOrAliasException(string columnName, string context, TextSpan span)
        : base($"Unknown column or alias '{columnName}'{(string.IsNullOrEmpty(context) ? "" : $" {context}")}.")
    {
        ColumnName = columnName;
        Code = DiagnosticCode.MQ3001_UnknownColumn;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the unknown column or alias.
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
