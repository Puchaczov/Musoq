using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class WindowFrameSemanticValidator
{
    internal static void Validate(
        WindowSpecificationNode node,
        FieldOrderedNode[] orderByFields,
        Func<Exception, Node?, bool> reportException)
    {
        var frame = node.Frame;
        if (frame == null)
            return;

        if (GetBoundRank(frame.Start.BoundType) > GetBoundRank(frame.End.BoundType))
        {
            reportException(CreateInvalidBoundsException(node, frame), node);
            return;
        }

        if (frame.FrameType != WindowFrameType.Range ||
            !HasOffsetBound(frame) ||
            orderByFields.Length == 0)
            return;

        if (orderByFields.Length == 1 && IsNumeric(orderByFields[0]))
            return;

        reportException(CreateInvalidRangeOrderKeyException(node), node);
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

    private static VisitorException CreateInvalidBoundsException(
        WindowSpecificationNode node,
        WindowFrameNode frame)
    {
        return new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "Visit(WindowSpecificationNode)",
            $"Invalid window frame: start bound '{frame.Start}' is logically after end bound '{frame.End}'.",
            DiagnosticCode.MQ3053_InvalidWindowFrameBounds,
            GetSpan(node));
    }

    private static VisitorException CreateInvalidRangeOrderKeyException(WindowSpecificationNode node)
    {
        return new VisitorException(
            nameof(BuildMetadataAndInferTypesVisitor),
            "Visit(WindowSpecificationNode)",
            "A RANGE frame with a PRECEDING or FOLLOWING offset requires exactly one numeric ORDER BY key.",
            DiagnosticCode.MQ3098_InvalidRangeFrameOrderKey,
            GetSpan(node));
    }

    private static TextSpan GetSpan(WindowSpecificationNode node)
    {
        return node.HasSpan ? node.Span : TextSpan.Empty;
    }
}
