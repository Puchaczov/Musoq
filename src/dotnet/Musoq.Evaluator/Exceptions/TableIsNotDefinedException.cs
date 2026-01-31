#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a table or data source is not defined.
/// </summary>
public class TableIsNotDefinedException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with table name.
    /// </summary>
    public TableIsNotDefinedException(string table)
        : base($"Table {table} is not defined in query")
    {
        TableName = table;
        Code = DiagnosticCode.MQ3023_TableNotDefined;
    }

    /// <summary>
    ///     Initializes a new instance with table name and span.
    /// </summary>
    public TableIsNotDefinedException(string table, TextSpan span)
        : base($"Table '{table}' is not defined in query")
    {
        TableName = table;
        Code = DiagnosticCode.MQ3023_TableNotDefined;
        Span = span;
    }

    /// <summary>
    ///     Gets the undefined table name.
    /// </summary>
    public string? TableName { get; }

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
