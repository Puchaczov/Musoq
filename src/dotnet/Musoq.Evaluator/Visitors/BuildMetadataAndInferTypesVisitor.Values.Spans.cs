using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private static ValuesSourceException CreateValuesSourceException(string message, ValuesFromNode node)
    {
        return new ValuesSourceException(message, node.SpanOrEmpty());
    }

    private static TextSpan GetRowSpan(ValuesRowNode row, ValuesFromNode node)
    {
        if (!row.Span.IsEmpty)
            return row.Span;

        var fieldSpan = row.Fields
            .Select(field => GetFieldNameSpan(field, node))
            .FirstOrDefault(span => !span.IsEmpty);

        return fieldSpan.IsEmpty ? node.SpanOrEmpty() : fieldSpan;
    }

    private static TextSpan GetFieldNameSpan(ValuesFieldNode field, ValuesFromNode node)
    {
        if (!field.NameSpan.IsEmpty)
            return field.NameSpan;

        return field.Expression.HasSpan ? field.Expression.Span : node.SpanOrEmpty();
    }

    private static TextSpan GetExpressionSpan(ValuesFieldNode field, ValuesFromNode node)
    {
        return field.Expression.HasSpan ? field.Expression.Span : GetFieldNameSpan(field, node);
    }

    private static TextSpan GetColumnExpressionSpan(IReadOnlyList<Node> expressions, ValuesFromNode node)
    {
        var expression = expressions.FirstOrDefault(expression => expression.HasSpan);
        return expression?.Span ?? node.SpanOrEmpty();
    }
}
