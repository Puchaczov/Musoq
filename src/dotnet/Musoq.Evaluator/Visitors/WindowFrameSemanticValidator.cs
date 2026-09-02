using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class WindowFrameSemanticValidator
{
    internal static void Validate(
        WindowSpecificationNode node,
        FieldOrderedNode[] orderByFields,
        Action<DiagnosticCode, string, TextSpan> report)
    {
        var frame = node.Frame;
        if (frame == null)
            return;

        if (GetBoundRank(frame.Start.BoundType) > GetBoundRank(frame.End.BoundType))
        {
            report(
                DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
                $"Invalid window frame: start bound '{DescribeBound(frame.Start)}' is logically after end bound '{DescribeBound(frame.End)}'.",
                GetSpan(node));
            return;
        }

        if (frame.FrameType != WindowFrameType.Range ||
            !HasOffsetBound(frame) ||
            orderByFields.Length == 0)
            return;

        if (orderByFields.Length == 1 && IsNumeric(orderByFields[0]))
            return;

        report(
            DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey,
            "A RANGE frame with a PRECEDING or FOLLOWING offset requires exactly one numeric ORDER BY key.",
            GetSpan(node));
    }

    private static bool IsNumeric(FieldOrderedNode field)
    {
        return BinaryOperatorTypeRules.IsNumericType(
            BinaryOperatorTypeRules.NormalizeOperandType(field.Expression.ReturnType));
    }

    private static bool HasOffsetBound(WindowFrameNode frame)
    {
        return frame.Start.BoundType is WindowFrameBoundType.OffsetPreceding or WindowFrameBoundType.OffsetFollowing ||
               frame.End.BoundType is WindowFrameBoundType.OffsetPreceding or WindowFrameBoundType.OffsetFollowing;
    }

    private static int GetBoundRank(WindowFrameBoundType boundType)
    {
        return boundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => 0,
            WindowFrameBoundType.OffsetPreceding => 1,
            WindowFrameBoundType.CurrentRow => 2,
            WindowFrameBoundType.OffsetFollowing => 3,
            WindowFrameBoundType.UnboundedFollowing => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(boundType), boundType, null)
        };
    }

    private static string DescribeBound(WindowFrameBoundNode bound)
    {
        return bound.BoundType switch
        {
            WindowFrameBoundType.UnboundedPreceding => "UNBOUNDED PRECEDING",
            WindowFrameBoundType.OffsetPreceding => $"{bound.Offset} PRECEDING",
            WindowFrameBoundType.CurrentRow => "CURRENT ROW",
            WindowFrameBoundType.OffsetFollowing => $"{bound.Offset} FOLLOWING",
            WindowFrameBoundType.UnboundedFollowing => "UNBOUNDED FOLLOWING",
            _ => bound.BoundType.ToString().ToUpperInvariant()
        };
    }

    private static TextSpan GetSpan(WindowSpecificationNode node)
    {
        return node.HasSpan ? node.Span : TextSpan.Empty;
    }
}
