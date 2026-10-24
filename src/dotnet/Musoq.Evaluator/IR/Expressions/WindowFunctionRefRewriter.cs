using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class WindowFunctionRefRewriter(IReadOnlyDictionary<int, int> windowIndexMap) : IrExpressionRewriter
{
    private readonly IReadOnlyDictionary<int, int> _windowIndexMap = windowIndexMap ?? throw new ArgumentNullException(nameof(windowIndexMap));

    public static IrExpression Rewrite(IrExpression expression, IReadOnlyDictionary<int, int> windowIndexMap)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var rewriter = new WindowFunctionRefRewriter(windowIndexMap);
        return rewriter.Visit(expression);
    }

    protected override IrExpression VisitWindowFunctionRef(WindowFunctionRef node)
    {
        if (!_windowIndexMap.TryGetValue(node.WindowIndex, out var windowIndex))
            return node;

        return windowIndex == node.WindowIndex
            ? node
            : new WindowFunctionRef(windowIndex, node.ReturnType);
    }

}
