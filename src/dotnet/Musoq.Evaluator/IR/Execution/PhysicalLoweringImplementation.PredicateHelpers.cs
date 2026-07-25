using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static IrExpression? AppendPredicate(IrExpression? left, IrExpression? right)
    {
        if (left == null)
            return right;

        if (right == null)
            return left;

        return new BinaryOp(BinaryOpKind.And, left, right, typeof(bool));
    }
}
