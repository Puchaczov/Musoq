using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using static Musoq.Evaluator.Visitors.BinaryOperatorTypeRules;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal bool ShouldSkipCoalesceRightOperand()
    {
        var left = SafePeek(Nodes, "Visit(CoalesceNode) left");
        return IsStaticallyNonNullableValueType(left.ReturnType);
    }

    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var right = SafePop(Nodes, "Visit(CoalesceNode) right");
        var left = SafePop(Nodes, "Visit(CoalesceNode) left");

        if (IsNullLiteral(left))
        {
            Nodes.Push(FoldNullLeftOperand(right));
            return;
        }

        if (IsStaticallyNonNullableValueType(left.ReturnType))
        {
            Nodes.Push(left);
            return;
        }

        ValidateCoalesceFallback(left, right, node);

        var result = new CoalesceNode(left, RetypeNullFallback(right, left), ResolveCoalesceReturnType(left, right));
        if (node.HasSpan)
            result.WithSpan(node.Span);

        Nodes.Push(result);
    }

    private void ValidateCoalesceFallback(Node left, Node right, Node errorContextNode)
    {
        if (AreCoalesceTypesCompatible(left.ReturnType, right.ReturnType))
            return;

        var message = $"Operator ?? requires compatible fallback types, but got '{FormatTypeName(left.ReturnType)}' and '{FormatTypeName(right.ReturnType)}'.";
        if (TryReportTypeMismatch(message, errorContextNode))
            return;

        throw new TypeMismatchException(message);
    }

    private static Node FoldNullLeftOperand(Node right)
    {
        return IsNullLiteral(right)
            ? new NullNode(typeof(object), right.Span)
            : right;
    }

    private static Node RetypeNullFallback(Node right, Node left)
    {
        if (!IsNullLiteral(right))
            return right;

        return new NullNode(ResolveCoalesceReturnType(left, right), right.Span);
    }

    private static bool AreCoalesceTypesCompatible(Type? leftType, Type? rightType)
    {
        if (leftType == null || rightType == null)
            return true;

        if (IsNullNodeType(leftType) || IsNullNodeType(rightType))
            return true;

        var leftValueType = Nullable.GetUnderlyingType(leftType) ?? leftType;
        var rightValueType = Nullable.GetUnderlyingType(rightType) ?? rightType;

        if (CanSkipStaticTypeValidation(leftValueType) || CanSkipStaticTypeValidation(rightValueType))
            return true;

        if (leftValueType == rightValueType)
            return true;

        if (leftValueType.IsValueType || rightValueType.IsValueType)
            return false;

        return leftValueType.IsAssignableFrom(rightValueType) || rightValueType.IsAssignableFrom(leftValueType);
    }

    private static Type ResolveCoalesceReturnType(Node left, Node right)
    {
        var leftType = left.ReturnType;
        var rightType = right.ReturnType;

        if (leftType == null || IsNullNodeType(leftType))
            return ResolveNullLeftReturnType(rightType);

        if (IsStaticallyNonNullableValueType(leftType))
            return leftType;

        if (rightType == null || IsNullNodeType(rightType))
            return leftType;

        var leftUnderlyingType = Nullable.GetUnderlyingType(leftType);
        if (leftUnderlyingType != null)
            return Nullable.GetUnderlyingType(rightType) == null ? leftUnderlyingType : leftType;

        if (leftType == typeof(object) || rightType == typeof(object))
            return typeof(object);

        if (!leftType.IsValueType && !rightType.IsValueType)
            return ResolveReferenceCoalesceReturnType(leftType, rightType);

        return leftType;
    }

    private static Type ResolveNullLeftReturnType(Type? rightType)
    {
        if (rightType == null || IsNullNodeType(rightType))
            return typeof(object);

        return rightType;
    }

    private static Type ResolveReferenceCoalesceReturnType(Type leftType, Type rightType)
    {
        if (leftType.IsAssignableFrom(rightType))
            return leftType;

        return rightType.IsAssignableFrom(leftType) ? rightType : typeof(object);
    }

    private static bool IsStaticallyNonNullableValueType(Type? type)
    {
        return type is { IsValueType: true } && Nullable.GetUnderlyingType(type) == null && !IsNullNodeType(type);
    }

    private static bool IsNullLiteral(Node node)
    {
        return node is NullNode || IsNullNodeType(node.ReturnType);
    }

    private static bool IsNullNodeType(Type? type)
    {
        return type is NullNode.NullType;
    }
}