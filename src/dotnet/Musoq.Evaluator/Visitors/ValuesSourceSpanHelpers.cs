using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class ValuesSourceSpanHelpers
{
    public static ValuesSourceException CreateException(
        string message,
        ValuesFromNode node,
        string constraint)
    {
        return ValuesSourceDiagnostics.Error(
            message,
            GetValuesInsertionSpan(node),
            ("constraint", constraint));
    }

    public static TextSpan GetValuesInsertionSpan(ValuesFromNode node)
    {
        var span = node.SpanOrEmpty();
        return span.Length >= 2 ? new TextSpan(span.End - 1, 0) : span;
    }

    public static TextSpan GetRowSpan(ValuesRowNode row, ValuesFromNode node)
    {
        if (!row.Span.IsEmpty)
            return row.Span;

        var fieldSpan = row.Fields
            .Select(field => GetFieldNameSpan(field, node))
            .FirstOrDefault(span => !span.IsEmpty);

        return fieldSpan.IsEmpty ? node.SpanOrEmpty() : fieldSpan;
    }

    public static TextSpan GetMissingFieldInsertionSpan(ValuesRowNode row, ValuesFromNode node)
    {
        return row.Span.Length >= 2
            ? new TextSpan(row.Span.End - 1, 0)
            : GetRowSpan(row, node);
    }

    public static TextSpan GetFieldNameSpan(ValuesFieldNode field, ValuesFromNode node)
    {
        if (!field.NameSpan.IsEmpty)
            return field.NameSpan;

        return field.Expression.HasSpan ? field.Expression.Span : node.SpanOrEmpty();
    }

    public static TextSpan GetColumnExpressionSpan(IReadOnlyList<Node> expressions, ValuesFromNode node)
    {
        var expression = expressions.FirstOrDefault(expression => expression.HasSpan);
        return expression?.Span ?? node.SpanOrEmpty();
    }
}
