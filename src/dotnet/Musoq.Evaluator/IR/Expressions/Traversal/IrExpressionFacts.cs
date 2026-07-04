using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

internal static class IrExpressionFacts
{
    public static bool ContainsMethodCall(IrExpression? expression)
    {
        return IrExpressionTraversal
            .SelfAndDescendants(expression)
            .Any(static current => current is MethodCall);
    }
}
