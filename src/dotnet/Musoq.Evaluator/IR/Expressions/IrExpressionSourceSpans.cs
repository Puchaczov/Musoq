using System.Runtime.CompilerServices;
using Musoq.Parser;

namespace Musoq.Evaluator.IR.Expressions;

internal static class IrExpressionSourceSpans
{
    private static readonly ConditionalWeakTable<IrExpression, SourceSpanHolder> Spans = new();

    public static T Set<T>(T expression, TextSpan span)
        where T : IrExpression
    {
        ArgumentNullException.ThrowIfNull(expression);

        Spans.Remove(expression);
        Spans.Add(expression, new SourceSpanHolder(span));
        return expression;
    }

    public static T CopyFrom<T>(T expression, IrExpression source)
        where T : IrExpression
    {
        return Set(expression, Get(source));
    }

    public static TextSpan Get(IrExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        return Spans.TryGetValue(expression, out var holder)
            ? holder.Span
            : TextSpan.Empty;
    }

    private sealed class SourceSpanHolder(TextSpan span)
    {
        public TextSpan Span { get; } = span;
    }
}
