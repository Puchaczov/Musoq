using System.Globalization;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed partial class LogicalConstantExpressionFolder
{
    private bool TryFoldUnary(UnaryOp node, out IrExpression folded)
    {
        folded = node;

        if (node.Operand is not Literal literal || literal.Value is null)
            return false;

        try
        {
            return node.Kind switch
            {
                UnaryOpKind.Not when literal.Value is bool value =>
                    Succeed(CreateFoldedLiteral(!value, node), out folded),
                UnaryOpKind.Negate when ConstantOperatorEvaluator.IsNumeric(literal.Value) =>
                    Succeed(CreateFoldedLiteral(Negate(literal.Value, node.ReturnType), node), out folded),
                _ => false
            };
        }
        catch (OverflowException)
        {
            ReportArithmeticOverflow(node);
            return false;
        }
        catch (ArithmeticException)
        {
            return false;
        }
        catch (InvalidCastException)
        {
            return false;
        }
    }

    private static object Negate(object value, Type returnType)
    {
        if (returnType == typeof(decimal))
            return -Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        if (returnType == typeof(double))
            return -Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (returnType == typeof(float))
            return -Convert.ToSingle(value, CultureInfo.InvariantCulture);

        if (returnType == typeof(long))
            return checked(-Convert.ToInt64(value, CultureInfo.InvariantCulture));

        return checked(-Convert.ToInt32(value, CultureInfo.InvariantCulture));
    }
}
