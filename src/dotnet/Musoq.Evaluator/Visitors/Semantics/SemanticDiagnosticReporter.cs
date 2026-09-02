using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticDiagnosticReporter(
    DiagnosticContext? diagnosticContext,
    IReadOnlyCollection<string>? generatedAliases = null)
{
    private readonly IReadOnlyCollection<string> _generatedAliases = generatedAliases ?? [];

    public bool TryReportTypeMismatch(string message, Node node)
    {
        if (diagnosticContext == null)
            return false;

        diagnosticContext.ReportError(DiagnosticCode.MQ3005_TypeMismatch, message, node);
        return true;
    }

    public bool TryReportException(Exception exception, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        diagnosticContext.ReportException(exception, node?.Span);
        return true;
    }

    public bool TryReportInvalidExpressionType(FieldNode field, Type? invalidType, string context, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        if (diagnosticContext.HasErrors)
            return true;

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Query output column '{field.FieldName}' has invalid type '{invalidType?.Name ?? "null"}' in {context}. Only primitive types are allowed in query outputs.",
            node);
        return true;
    }

    public bool TryReportInvalidExpressionType(string expressionDescription, Type? invalidType, string context, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        if (diagnosticContext.HasErrors)
            return true;

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Expression '{expressionDescription}' has invalid type '{invalidType?.Name ?? "null"}' in {context}. Only primitive types are allowed in query expressions.",
            node);
        return true;
    }

    public bool TryReportNonAggregatedColumnInSelect(string columnName, IEnumerable<string> groupByColumns, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        // An invalid aggregate in GROUP BY owns the structural failure. The
        // non-aggregate projection error is only a dependent consequence of
        // the same malformed grouping shape.
        if (diagnosticContext.Diagnostics.Any(diagnostic =>
                diagnostic.Code == DiagnosticCode.MQ3092_AggregateInGroupBy))
            return true;

        var groupByColumnList = groupByColumns
            .Select(FormatGroupByColumn)
            .ToArray();
        var groupByList = groupByColumnList.Length > 0
            ? string.Join(", ", groupByColumnList)
            : "(none)";
        diagnosticContext.ReportError(
            DiagnosticCode.MQ3012_NonAggregateInSelect,
            $"Column '{columnName}' must appear in the GROUP BY clause or be used in an aggregate function. " +
            $"Current GROUP BY columns: {groupByList}.",
            node);
        return true;
    }

    private string FormatGroupByColumn(string value)
    {
        foreach (var alias in _generatedAliases)
        {
            if (value.StartsWith(alias + ".", StringComparison.OrdinalIgnoreCase))
                return value[(alias.Length + 1)..];
        }

        return value;
    }
}
