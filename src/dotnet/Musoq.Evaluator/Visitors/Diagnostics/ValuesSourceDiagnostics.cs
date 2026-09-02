using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

internal static class ValuesSourceDiagnostics
{
    public static string RowNumber(int rowIndex)
    {
        return InvariantNumber(rowIndex + 1);
    }

    public static string InvariantNumber(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static TextSpan ExpressionSpan(ValuesFieldNode field, ValuesFromNode node)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(node);

        if (field.Expression is AccessMethodNode method)
        {
            var span = method.FunctionToken.Span.Through(method.Arguments.Span);
            if (method.FilterExpression?.HasSpan == true)
                span = span.Through(method.FilterExpression.Span);
            return span;
        }

        return field.Expression.HasSpan
            ? field.Expression.Span
            : !field.NameSpan.IsEmpty
                ? field.NameSpan
                : node.SpanOrEmpty();
    }

    public static ValuesSourceException Error(
        string message,
        TextSpan span,
        params (string Name, string Value)[] facts)
    {
        var arguments = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sourceKind"] = "values"
        };
        foreach (var (name, value) in facts)
            arguments[name] = value;

        return new ValuesSourceException(message, span, arguments);
    }
}
