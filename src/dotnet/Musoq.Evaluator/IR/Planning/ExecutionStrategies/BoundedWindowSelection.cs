using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.IR.Planning;

internal static class BoundedWindowSelection
{
    internal static bool CanSelectWinners(WindowRegistration registration, IrExpression? qualify)
    {
        if (
            registration.Function?.DeclaringType != typeof(LibraryBase)
            || registration.Function.Name != nameof(LibraryBase.RowNumber)
            || registration.OrderKeys.Length == 0
            || !registration.OrderKeys.All(key => ExpressionStabilityAnalyzer.IsStable(key.Expression))
            || !registration.PartitionKeys.All(ExpressionStabilityAnalyzer.IsStable)
        )
            return false;
        return qualify switch
        {
            BinaryOp
            {
                Kind: BinaryOpKind.LessThan or BinaryOpKind.LessOrEqual,
                Left: WindowFunctionRef reference,
                Right: Literal bound
            } => reference.WindowIndex == registration.WindowIndex && IsIntegralBound(bound.Value),
            BinaryOp
            {
                Kind: BinaryOpKind.GreaterThan or BinaryOpKind.GreaterOrEqual,
                Right: WindowFunctionRef reference,
                Left: Literal bound
            } => reference.WindowIndex == registration.WindowIndex && IsIntegralBound(bound.Value),
            _ => false,
        };
    }

    private static bool IsIntegralBound(object? value) =>
        value is byte or sbyte or short or ushort or int or uint or long;

    internal static bool CanReusePeers(WindowRegistration registration)
    {
        if (
            registration.Function?.DeclaringType != typeof(LibraryBase)
            || registration.Function.Name is not ("Sum" or "Count")
            || registration.FilterPredicate != null
            || registration.Frame is not { FrameType: WindowFrameType.Range } frame
            || registration.OrderKeys is not [var order]
            || !ExpressionStabilityAnalyzer.IsStable(order.Expression)
            || !registration.PartitionKeys.All(ExpressionStabilityAnalyzer.IsStable)
            || !registration.ValueArguments.All(ExpressionStabilityAnalyzer.IsStable)
            || !(IsOffset(frame.Start.BoundType) || IsOffset(frame.End.BoundType))
        )
            return false;
        var type = Nullable.GetUnderlyingType(order.Expression.ReturnType) ?? order.Expression.ReturnType;
        return Type.GetTypeCode(type)
            is TypeCode.Byte
                or TypeCode.SByte
                or TypeCode.Int16
                or TypeCode.UInt16
                or TypeCode.Int32
                or TypeCode.UInt32
                or TypeCode.Int64
                or TypeCode.UInt64
                or TypeCode.Decimal;
    }

    private static bool IsOffset(WindowFrameBoundType bound) =>
        bound is WindowFrameBoundType.OffsetPreceding or WindowFrameBoundType.OffsetFollowing;

    internal static bool CanUseIntegerOrderIndex(WindowRegistration registration, WindowFunctionAttribute? declaration)
    {
        if (registration.OrderKeys is not [var key] || key.Expression.ReturnType != typeof(int))
            return false;
        if (declaration?.RankingKind is not null and not WindowRankingKind.None)
            return true;
        return declaration?.ContextType is null && registration.Frame?.FrameType == WindowFrameType.Rows;
    }
}
