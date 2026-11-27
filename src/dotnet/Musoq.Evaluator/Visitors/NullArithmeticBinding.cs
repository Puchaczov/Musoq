using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class NullArithmeticBinding
{
    internal static Node AsNullable(Node operand, Type type)
    {
        var target = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(type);
        return operand is NullNode
            ? new NullNode(target, operand.Span)
            : new CastNode(operand, (Nullable.GetUnderlyingType(target) ?? target).Name, target).CopySpansFrom(operand);
    }

    internal static Node PreserveNullOperands(Node left, Node right, Node source)
    {
        if (left is NullNode)
            return right;
        if (right is NullNode)
            return left;
        // The left operand always yields SQL NULL; coalescing evaluates both in their original order.
        return new CoalesceNode(left, right, typeof(DBNull)).CopySpansFrom(source);
    }

    internal static Node? Bind<T>(Node left, Node right, Func<Node, Node, T> create, Node source)
        where T : Node
    {
        var leftIsNull = left.ReturnType is NullNode.NullType;
        var rightIsNull = right.ReturnType is NullNode.NullType;
        if (!leftIsNull && !rightIsNull)
            return null;
        if (leftIsNull && rightIsNull)
            return PreserveNullOperands(left, right, source);

        var type = BinaryOperatorTypeRules.NormalizeOperandType(leftIsNull ? right.ReturnType : left.ReturnType);
        return create(leftIsNull ? AsNullable(left, type) : left, rightIsNull ? AsNullable(right, type) : right)
            .CopySpansFrom(source);
    }
}
