#nullable enable
using System;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

/// <summary>
///     Evaluates constant expressions at compile time, replacing them with literal nodes.
///     Runs after type inference so all nodes have resolved types.
///     Detects division/modulo by zero (MQ3008), arithmetic overflow (MQ3032),
///     tautological conditions (MQ5010), and contradictory conditions (MQ5011).
/// </summary>
public sealed class ConstantFoldingVisitor : CloneQueryVisitor
{
    private readonly DiagnosticContext? _diagnosticContext;

    public ConstantFoldingVisitor(DiagnosticContext? diagnosticContext = null)
    {
        _diagnosticContext = diagnosticContext;
    }

    protected override string VisitorName => nameof(ConstantFoldingVisitor);

    public override void Visit(AddNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitAddNode);
        var left = nodes[0];
        var right = nodes[1];

        if (left is ConstantValueNode leftConst && right is ConstantValueNode rightConst)
        {
            var folded = FoldAdd(leftConst, rightConst, node.Span);

            if (folded != null)
            {
                Nodes.Push(folded);
                return;
            }
        }

        // Adjacent string constant folding:
        // AddNode(AddNode(X, 'a'), 'b') → AddNode(X, 'ab')
        if (IsStringConstant(right) && left is AddNode leftAdd)
        {
            var merged = TryMergeAdjacentStringConstants(leftAdd, (ConstantValueNode)right, node.Span);

            if (merged != null)
            {
                Nodes.Push(merged);
                return;
            }
        }

        Nodes.Push(new AddNode(left, right).WithSpan(node.Span));
    }

    public override void Visit(HyphenNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitHyphenNode);
        var left = nodes[0];
        var right = nodes[1];

        var folded = TryFoldArithmetic(left, right, Subtract, node.Span);
        Nodes.Push(folded ?? new HyphenNode(left, right).WithSpan(node.Span));
    }

    public override void Visit(StarNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitStarNode);
        var left = nodes[0];
        var right = nodes[1];

        var folded = TryFoldArithmetic(left, right, Multiply, node.Span);
        Nodes.Push(folded ?? new StarNode(left, right).WithSpan(node.Span));
    }

    public override void Visit(FSlashNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitFSlashNode);
        var folded = TryFoldDivisionLike(nodes[0], nodes[1], Divide, node.Span);
        Nodes.Push(folded ?? new FSlashNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(ModuloNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitModuloNode);
        var folded = TryFoldDivisionLike(nodes[0], nodes[1], Modulo, node.Span);
        Nodes.Push(folded ?? new ModuloNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(BitwiseAndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseAndNode);
        var folded = TryFoldBitwise(nodes[0], nodes[1], BitwiseAnd, node.Span);
        Nodes.Push(folded ?? new BitwiseAndNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(BitwiseOrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseOrNode);
        var folded = TryFoldBitwise(nodes[0], nodes[1], BitwiseOr, node.Span);
        Nodes.Push(folded ?? new BitwiseOrNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(BitwiseXorNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitBitwiseXorNode);
        var folded = TryFoldBitwise(nodes[0], nodes[1], BitwiseXor, node.Span);
        Nodes.Push(folded ?? new BitwiseXorNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(LeftShiftNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitLeftShiftNode);
        var folded = TryFoldBitwise(nodes[0], nodes[1], LeftShift, node.Span);
        Nodes.Push(folded ?? new LeftShiftNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(RightShiftNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitRightShiftNode);
        var folded = TryFoldBitwise(nodes[0], nodes[1], RightShift, node.Span);
        Nodes.Push(folded ?? new RightShiftNode(nodes[0], nodes[1]).WithSpan(node.Span));
    }

    public override void Visit(AndNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitAndNode);
        var left = nodes[0];
        var right = nodes[1];

        if (left is BooleanNode leftBool && right is BooleanNode rightBool)
        {
            Nodes.Push(new BooleanNode(leftBool.Value && rightBool.Value, node.Span));
            return;
        }

        Nodes.Push(new AndNode(left, right).WithSpan(node.Span));
    }

    public override void Visit(OrNode node)
    {
        var nodes = SafePopMultiple(Nodes, 2, VisitorOperationNames.VisitOrNode);
        var left = nodes[0];
        var right = nodes[1];

        if (left is BooleanNode leftBool && right is BooleanNode rightBool)
        {
            Nodes.Push(new BooleanNode(leftBool.Value || rightBool.Value, node.Span));
            return;
        }

        Nodes.Push(new OrNode(left, right).WithSpan(node.Span));
    }

    public override void Visit(NotNode node)
    {
        var operand = SafePop(Nodes, "VisitNotNode");

        if (operand is BooleanNode boolNode)
        {
            Nodes.Push(new BooleanNode(!boolNode.Value, node.Span));
            return;
        }

        Nodes.Push(new NotNode(operand).WithSpan(node.Span));
    }

    public override void Visit(WhereNode node)
    {
        var expression = Nodes.Pop();
        ReportConstantCondition(expression, "WHERE");
        Nodes.Push(new WhereNode(expression).WithSpan(node.Span));
    }

    public override void Visit(HavingNode node)
    {
        var expression = Nodes.Pop();
        ReportConstantCondition(expression, "HAVING");
        Nodes.Push(new HavingNode(expression).WithSpan(node.Span));
    }

    #region Arithmetic helpers

    private Node? TryFoldArithmetic(
        Node left, Node right,
        Func<object, object, object> operation,
        TextSpan span)
    {
        if (left is not ConstantValueNode leftConst || right is not ConstantValueNode rightConst)
            return null;

        if (IsNullConstant(leftConst) || IsNullConstant(rightConst))
            return CreateNullNode(left, right, span);

        return FoldCheckedArithmetic(leftConst, rightConst, operation, span);
    }

    private Node? TryFoldDivisionLike(
        Node left, Node right,
        Func<object, object, object> operation,
        TextSpan span)
    {
        if (left is not ConstantValueNode leftConst || right is not ConstantValueNode rightConst)
            return null;

        if (IsNullConstant(leftConst) || IsNullConstant(rightConst))
            return CreateNullNode(left, right, span);

        if (IsZero(rightConst))
        {
            ReportDivisionByZero(span);
            return null;
        }

        return FoldCheckedArithmetic(leftConst, rightConst, operation, span);
    }

    private Node? FoldCheckedArithmetic(
        ConstantValueNode left, ConstantValueNode right,
        Func<object, object, object> operation,
        TextSpan span)
    {
        try
        {
            var (leftVal, rightVal) = PromoteToCommonType(left.ObjValue, right.ObjValue);
            var resultValue = operation(leftVal, rightVal);
            return CreateConstantNode(resultValue, span);
        }
        catch (OverflowException)
        {
            ReportArithmeticOverflow(span);
            return null;
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    private Node? FoldAdd(ConstantValueNode left, ConstantValueNode right, TextSpan span)
    {
        if (IsNullConstant(left) || IsNullConstant(right))
            return CreateNullNode(left, right, span);

        if (IsStringValue(left) || IsStringValue(right))
        {
            var concatenated = string.Concat(left.ObjValue?.ToString(), right.ObjValue?.ToString());
            return new WordNode(concatenated, span);
        }

        return FoldCheckedArithmetic(left, right, Add, span);
    }

    private Node? TryFoldBitwise(
        Node left, Node right,
        Func<object, object, object> operation,
        TextSpan span)
    {
        if (left is not ConstantValueNode leftConst || right is not ConstantValueNode rightConst)
            return null;

        if (IsNullConstant(leftConst) || IsNullConstant(rightConst))
            return CreateNullNode(left, right, span);

        try
        {
            var leftLong = ConvertToLong(leftConst.ObjValue);
            var rightLong = ConvertToLong(rightConst.ObjValue);
            var resultValue = operation(leftLong, rightLong);
            return CreateConstantNode(resultValue, span);
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }

    #endregion

    #region Adjacent string constant merging

    private Node? TryMergeAdjacentStringConstants(AddNode leftAdd, ConstantValueNode rightConst, TextSpan span)
    {
        // Pattern: AddNode(X, 'a') + 'b' → AddNode(X, 'ab')
        // where leftAdd.Right is a string constant
        if (!IsStringConstant(leftAdd.Right))
            return null;

        var leftRight = (ConstantValueNode)leftAdd.Right;
        var merged = string.Concat(leftRight.ObjValue?.ToString(), rightConst.ObjValue?.ToString());
        return new AddNode(leftAdd.Left, new WordNode(merged, span));
    }

    #endregion

    #region Type promotion and arithmetic operations

    private static (object Left, object Right) PromoteToCommonType(object left, object right)
    {
        var leftType = left.GetType();
        var rightType = right.GetType();

        if (leftType == rightType)
            return (left, right);

        var targetType = NodeHelpers.GetReturnTypeMap(leftType, rightType);

        return (ConvertTo(left, targetType), ConvertTo(right, targetType));
    }

    private static object ConvertTo(object value, Type targetType)
    {
        if (value.GetType() == targetType)
            return value;

        return Type.GetTypeCode(targetType) switch
        {
            TypeCode.Byte => Convert.ToByte(value),
            TypeCode.SByte => Convert.ToSByte(value),
            TypeCode.Int16 => Convert.ToInt16(value),
            TypeCode.UInt16 => Convert.ToUInt16(value),
            TypeCode.Int32 => Convert.ToInt32(value),
            TypeCode.UInt32 => Convert.ToUInt32(value),
            TypeCode.Int64 => Convert.ToInt64(value),
            TypeCode.UInt64 => Convert.ToUInt64(value),
            TypeCode.Single => Convert.ToSingle(value),
            TypeCode.Double => Convert.ToDouble(value),
            TypeCode.Decimal => Convert.ToDecimal(value),
            _ => throw new InvalidCastException(
                $"Cannot convert {value.GetType().Name} to {targetType.Name}")
        };
    }

    private static object Add(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l + (decimal)right),
            double l => l + (double)right,
            float l => l + (float)right,
            ulong l => checked(l + (ulong)right),
            long l => checked(l + (long)right),
            uint l => checked(l + (uint)right),
            int l => checked(l + (int)right),
            ushort l => checked((int)l + Convert.ToInt32(right)),
            short l => checked((int)l + Convert.ToInt32(right)),
            byte l => checked((int)l + Convert.ToInt32(right)),
            sbyte l => checked((int)l + Convert.ToInt32(right)),
            _ => throw new InvalidCastException($"Cannot add {left.GetType().Name}")
        };
    }

    private static object Subtract(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l - (decimal)right),
            double l => l - (double)right,
            float l => l - (float)right,
            ulong l => checked(l - (ulong)right),
            long l => checked(l - (long)right),
            uint l => checked(l - (uint)right),
            int l => checked(l - (int)right),
            ushort l => checked((int)l - Convert.ToInt32(right)),
            short l => checked((int)l - Convert.ToInt32(right)),
            byte l => checked((int)l - Convert.ToInt32(right)),
            sbyte l => checked((int)l - Convert.ToInt32(right)),
            _ => throw new InvalidCastException($"Cannot subtract {left.GetType().Name}")
        };
    }

    private static object Multiply(object left, object right)
    {
        return left switch
        {
            decimal l => checked(l * (decimal)right),
            double l => l * (double)right,
            float l => l * (float)right,
            ulong l => checked(l * (ulong)right),
            long l => checked(l * (long)right),
            uint l => checked(l * (uint)right),
            int l => checked(l * (int)right),
            ushort l => checked((int)l * Convert.ToInt32(right)),
            short l => checked((int)l * Convert.ToInt32(right)),
            byte l => checked((int)l * Convert.ToInt32(right)),
            sbyte l => checked((int)l * Convert.ToInt32(right)),
            _ => throw new InvalidCastException($"Cannot multiply {left.GetType().Name}")
        };
    }

    private static object Divide(object left, object right)
    {
        return left switch
        {
            decimal l => l / (decimal)right,
            double l => l / (double)right,
            float l => l / (float)right,
            ulong l => l / (ulong)right,
            long l => l / (long)right,
            uint l => l / (uint)right,
            int l => l / (int)right,
            ushort l => (int)l / Convert.ToInt32(right),
            short l => (int)l / Convert.ToInt32(right),
            byte l => (int)l / Convert.ToInt32(right),
            sbyte l => (int)l / Convert.ToInt32(right),
            _ => throw new InvalidCastException($"Cannot divide {left.GetType().Name}")
        };
    }

    private static object Modulo(object left, object right)
    {
        return left switch
        {
            decimal l => l % (decimal)right,
            double l => l % (double)right,
            float l => l % (float)right,
            ulong l => l % (ulong)right,
            long l => l % (long)right,
            uint l => l % (uint)right,
            int l => l % (int)right,
            ushort l => (int)l % Convert.ToInt32(right),
            short l => (int)l % Convert.ToInt32(right),
            byte l => (int)l % Convert.ToInt32(right),
            sbyte l => (int)l % Convert.ToInt32(right),
            _ => throw new InvalidCastException($"Cannot modulo {left.GetType().Name}")
        };
    }

    private static object BitwiseAnd(object left, object right) => (long)left & (long)right;

    private static object BitwiseOr(object left, object right) => (long)left | (long)right;

    private static object BitwiseXor(object left, object right) => (long)left ^ (long)right;

    private static object LeftShift(object left, object right) => (long)left << (int)(long)right;

    private static object RightShift(object left, object right) => (long)left >> (int)(long)right;

    #endregion

    #region Node creation and type helpers

    private static Node CreateConstantNode(object value, TextSpan span)
    {
        return value switch
        {
            int i => new IntegerNode(i, span),
            long l => new IntegerNode(l, span),
            uint ui => new IntegerNode(ui, span),
            ulong ul => new IntegerNode(ul, span),
            short s => new IntegerNode(s, span),
            ushort us => new IntegerNode(us, span),
            sbyte sb => new IntegerNode(sb, span),
            byte b => new IntegerNode(b, span),
            decimal d => new DecimalNode(d, span),
            double d => new DecimalNode((decimal)d, span),
            float f => new DecimalNode((decimal)f, span),
            string s => new WordNode(s, span),
            bool b => new BooleanNode(b, span),
            _ => throw new InvalidCastException($"Cannot create constant node for {value.GetType().Name}")
        };
    }

    private static Node CreateNullNode(Node left, Node right, TextSpan span)
    {
        var nonNullType = left is NullNode ? right.ReturnType : left.ReturnType;
        var baseType = nonNullType is NullNode.NullType ? typeof(object) : nonNullType;
        return new NullNode(baseType, span);
    }

    private static bool IsZero(ConstantValueNode node)
    {
        return node.ObjValue switch
        {
            int i => i == 0,
            long l => l == 0,
            uint ui => ui == 0,
            ulong ul => ul == 0,
            short s => s == 0,
            ushort us => us == 0,
            sbyte sb => sb == 0,
            byte b => b == 0,
            decimal d => d == 0m,
            double d => d == 0.0,
            float f => f == 0f,
            _ => false
        };
    }

    private static bool IsNullConstant(Node node) => node is NullNode;

    private static bool IsStringValue(ConstantValueNode node) =>
        node is WordNode or StringNode;

    private static bool IsStringConstant(Node node) =>
        node is WordNode or StringNode;

    private static long ConvertToLong(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            uint ui => ui,
            short s => s,
            ushort us => us,
            sbyte sb => sb,
            byte b => b,
            _ => throw new InvalidCastException($"Cannot convert {value.GetType().Name} to long for bitwise operation")
        };
    }

    private void ReportDivisionByZero(TextSpan span)
    {
        _diagnosticContext?.ReportError(
            DiagnosticCode.MQ3008_DivisionByZero,
            "Division by zero in constant expression.",
            span);
    }

    private void ReportArithmeticOverflow(TextSpan span)
    {
        _diagnosticContext?.ReportError(
            DiagnosticCode.MQ3032_ArithmeticOverflow,
            "Arithmetic overflow in constant expression.",
            span);
    }

    private void ReportConstantCondition(Node expression, string clauseName)
    {
        if (expression is not BooleanNode boolNode)
            return;

        if (boolNode.Value)
        {
            _diagnosticContext?.ReportWarning(
                DiagnosticCode.MQ5010_TautologicalCondition,
                $"{clauseName} clause always evaluates to true and has no effect.",
                boolNode.Span);
        }
        else
        {
            _diagnosticContext?.ReportWarning(
                DiagnosticCode.MQ5011_ContradictoryCondition,
                $"{clauseName} clause always evaluates to false; no rows will be returned.",
                boolNode.Span);
        }
    }

    #endregion
}
