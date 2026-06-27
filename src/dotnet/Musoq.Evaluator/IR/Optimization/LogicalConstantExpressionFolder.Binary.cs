using System.Globalization;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.Visitors;

namespace Musoq.Evaluator.IR.Optimization;

internal sealed partial class LogicalConstantExpressionFolder
{
    private bool TryFoldBinary(BinaryOp node, out IrExpression folded)
    {
        folded = node;

        if (node.Left is not Literal left || node.Right is not Literal right)
            return false;

        if (TryFoldNullBinary(node, left.Value, right.Value, out folded))
            return true;

        if (left.Value is null || right.Value is null)
            return false;

        try
        {
            return node.Kind switch
            {
                BinaryOpKind.And when left.Value is bool leftBool && right.Value is bool rightBool =>
                    Succeed(CreateFoldedLiteral(leftBool && rightBool, node), out folded),
                BinaryOpKind.Or when left.Value is bool leftBool && right.Value is bool rightBool =>
                    Succeed(CreateFoldedLiteral(leftBool || rightBool, node), out folded),
                BinaryOpKind.Equal or BinaryOpKind.IsNotDistinctFrom =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) == 0, node), out folded),
                BinaryOpKind.NotEqual or BinaryOpKind.IsDistinctFrom =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) != 0, node), out folded),
                BinaryOpKind.GreaterThan =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) > 0, node), out folded),
                BinaryOpKind.LessThan =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) < 0, node), out folded),
                BinaryOpKind.GreaterOrEqual =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) >= 0, node), out folded),
                BinaryOpKind.LessOrEqual =>
                    Succeed(CreateFoldedLiteral(CompareLiteralValues(left.Value, right.Value) <= 0, node), out folded),
                BinaryOpKind.StringConcatenate =>
                    Succeed(CreateFoldedLiteral(string.Concat(left.Value, right.Value), node), out folded),
                BinaryOpKind.Add when node.ReturnType == typeof(string) =>
                    Succeed(CreateFoldedLiteral(string.Concat(left.Value, right.Value), node), out folded),
                _ => TryFoldNumericBinary(node, left.Value, right.Value, out folded)
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
        catch (FormatException)
        {
            return false;
        }
    }

    private bool TryFoldNullBinary(
        BinaryOp node,
        object? left,
        object? right,
        out IrExpression folded)
    {
        folded = node;

        if (left is not null && right is not null)
            return false;

        return node.Kind switch
        {
            BinaryOpKind.Equal or BinaryOpKind.IsNotDistinctFrom => Succeed(CreateFoldedLiteral(left is null && right is null, node), out folded),
            BinaryOpKind.NotEqual or BinaryOpKind.IsDistinctFrom => Succeed(CreateFoldedLiteral(left is not null || right is not null, node), out folded),
            BinaryOpKind.Add or
                BinaryOpKind.Subtract or
                BinaryOpKind.Multiply or
                BinaryOpKind.Divide or
                BinaryOpKind.Modulo or
                BinaryOpKind.StringConcatenate => Succeed(CreateFoldedLiteral(null, node), out folded),
            _ => false
        };
    }

    private bool TryFoldNumericBinary(BinaryOp node, object left, object right, out IrExpression folded)
    {
        folded = node;

        if (!ConstantOperatorEvaluator.IsNumeric(left) || !ConstantOperatorEvaluator.IsNumeric(right))
            return false;

        var operation = node.Kind switch
        {
            BinaryOpKind.Add => ConstantOperatorKind.Add,
            BinaryOpKind.Subtract => ConstantOperatorKind.Subtract,
            BinaryOpKind.Multiply => ConstantOperatorKind.Multiply,
            BinaryOpKind.Divide => ConstantOperatorKind.Divide,
            BinaryOpKind.Modulo => ConstantOperatorKind.Modulo,
            BinaryOpKind.BitwiseAnd => ConstantOperatorKind.BitwiseAnd,
            BinaryOpKind.BitwiseOr => ConstantOperatorKind.BitwiseOr,
            BinaryOpKind.BitwiseXor => ConstantOperatorKind.BitwiseXor,
            BinaryOpKind.LeftShift => ConstantOperatorKind.LeftShift,
            BinaryOpKind.RightShift => ConstantOperatorKind.RightShift,
            _ => (ConstantOperatorKind?)null
        };

        if (operation is null)
            return false;

        if ((operation == ConstantOperatorKind.Divide || operation == ConstantOperatorKind.Modulo) &&
            ConstantOperatorEvaluator.IsZero(right))
        {
            ReportDivisionByZero(node);
            return false;
        }

        var normalizedLeft = NormalizeNumericLiteral(left, node.ReturnType);
        var normalizedRight = NormalizeNumericLiteral(right, operation.Value is ConstantOperatorKind.LeftShift or ConstantOperatorKind.RightShift
            ? typeof(long)
            : node.ReturnType);
        var value = ConstantOperatorEvaluator.EvaluatePreservingNumericType(operation.Value, normalizedLeft, normalizedRight);

        return Succeed(CreateFoldedLiteral(value, node), out folded);
    }

    private static object NormalizeNumericLiteral(object value, Type targetType)
    {
        if (targetType == typeof(byte) ||
            targetType == typeof(sbyte) ||
            targetType == typeof(short) ||
            targetType == typeof(ushort))
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(uint))
            return Convert.ToUInt32(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(ulong))
            return Convert.ToUInt64(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(long))
            return Convert.ToInt64(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(float))
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(double))
            return Convert.ToDouble(value, CultureInfo.InvariantCulture);

        if (targetType == typeof(decimal))
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static int CompareLiteralValues(object left, object right)
    {
        return ConstantOperatorEvaluator.CompareValues(left, right);
    }
}
