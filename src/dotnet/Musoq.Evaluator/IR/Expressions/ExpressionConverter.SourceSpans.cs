using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private static T WithSourceSpan<T>(T expression, Node node)
        where T : IrExpression
    {
        return IrExpressionSourceSpans.Set(
            expression,
            node.HasSpan ? node.Span : TextSpan.Empty);
    }
}
