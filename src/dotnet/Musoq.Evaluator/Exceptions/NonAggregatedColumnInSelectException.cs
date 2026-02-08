#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Exceptions;

/// <summary>
///     Exception thrown when a non-aggregated column is used in SELECT with GROUP BY
///     but the column is not part of the GROUP BY clause.
/// </summary>
public class NonAggregatedColumnInSelectException : Exception, IDiagnosticException
{
    /// <summary>
    ///     Initializes a new instance with column name and available GROUP BY columns.
    /// </summary>
    public NonAggregatedColumnInSelectException(string columnName, string[] groupByColumns)
        : base(BuildMessage(columnName, groupByColumns))
    {
        ColumnName = columnName;
        GroupByColumns = groupByColumns;
        Code = DiagnosticCode.MQ3012_NonAggregateInSelect;
    }

    /// <summary>
    ///     Initializes a new instance with column name, GROUP BY columns, and span.
    /// </summary>
    public NonAggregatedColumnInSelectException(string columnName, string[] groupByColumns, TextSpan span)
        : base(BuildMessage(columnName, groupByColumns))
    {
        ColumnName = columnName;
        GroupByColumns = groupByColumns;
        Code = DiagnosticCode.MQ3012_NonAggregateInSelect;
        Span = span;
    }

    /// <summary>
    ///     Gets the name of the non-aggregated column.
    /// </summary>
    public string ColumnName { get; }

    /// <summary>
    ///     Gets the columns that are in the GROUP BY clause.
    /// </summary>
    public string[] GroupByColumns { get; }

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

    private static string BuildMessage(string columnName, string[] groupByColumns)
    {
        var groupByList = groupByColumns.Length > 0
            ? string.Join(", ", groupByColumns)
            : "(none)";
        return $"Column '{columnName}' must appear in the GROUP BY clause or be used in an aggregate function. " +
               $"Current GROUP BY columns: {groupByList}.";
    }
}
