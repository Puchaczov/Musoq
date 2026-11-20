using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static partial class PhysicalStrategyRules
{
    private static IrExpression RangeJoinResidual(JoinKind kind, IrExpression predicate) =>
        kind == JoinKind.FullOuter && predicate is BinaryOp single && IsSortMergeRangeKind(single.Kind)
            ? new Literal(true, typeof(bool))
            : predicate;

    // Index construction can evaluate keys even when the other input is empty.
    private static bool IsSafeFullOuterRangePredicate(IrExpression expression) =>
        expression switch
        {
            BinaryOp { Kind: BinaryOpKind.And } conjunction => IsSafeFullOuterRangePredicate(conjunction.Left)
                && IsSafeFullOuterRangePredicate(conjunction.Right),
            BinaryOp comparison
                when IsSortMergeRangeKind(comparison.Kind)
                    || comparison.Kind is BinaryOpKind.Equal or BinaryOpKind.NotEqual => comparison.Left
                is ColumnRef
                    or Literal
                && comparison.Right is ColumnRef or Literal
                && (!IsSortMergeRangeKind(comparison.Kind) || comparison.Left.ReturnType != typeof(string))
                && IsFullOuterComparisonType(comparison.Left.ReturnType)
                && IsFullOuterComparisonType(comparison.Right.ReturnType),
            _ => false,
        };

    private static bool IsFullOuterComparisonType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return !type.IsEnum
            && Type.GetTypeCode(type)
                is TypeCode.Byte
                    or TypeCode.SByte
                    or TypeCode.Int16
                    or TypeCode.UInt16
                    or TypeCode.Int32
                    or TypeCode.UInt32
                    or TypeCode.Int64
                    or TypeCode.UInt64
                    or TypeCode.Decimal
                    or TypeCode.DateTime
                    or TypeCode.String;
    }

    private static bool IsSortMergeRangeKind(BinaryOpKind kind)
    {
        return kind
            is BinaryOpKind.GreaterThan
                or BinaryOpKind.GreaterOrEqual
                or BinaryOpKind.LessThan
                or BinaryOpKind.LessOrEqual;
    }
}
