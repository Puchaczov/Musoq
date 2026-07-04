using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticDiagnosticReporter(DiagnosticContext? diagnosticContext)
{
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

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Query output column '{field.FieldName}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query outputs.",
            node);
        return true;
    }

    public bool TryReportInvalidExpressionType(string expressionDescription, Type? invalidType, string context, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        diagnosticContext.ReportError(
            DiagnosticCode.MQ3027_InvalidExpressionType,
            $"Expression '{expressionDescription}' has invalid type '{invalidType?.FullName ?? "null"}' in {context}. Only primitive types are allowed in query expressions.",
            node);
        return true;
    }

    public bool TryReportNonAggregatedColumnInSelect(string columnName, IEnumerable<string> groupByColumns, Node? node)
    {
        if (diagnosticContext == null)
            return false;

        var groupByColumnList = groupByColumns.ToArray();
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
}
