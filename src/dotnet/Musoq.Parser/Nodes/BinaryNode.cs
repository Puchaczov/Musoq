using System;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class BinaryNode : Node
{
    private readonly Node[] _nodes;
    private Type _returnType;

    protected BinaryNode(Node left, Node right)
        : this(left, right, default)
    {
    }

    protected BinaryNode(Node left, Node right, TextSpan span)
    {
        _nodes = [left, right];

        // If no explicit span provided, compute from children
        if (span.IsEmpty && left?.HasSpan == true && right?.HasSpan == true)
        {
            Span = ComputeSpan(left, right);
            FullSpan = Span;
        }
        else
        {
            Span = span;
            FullSpan = span;
        }
    }

    public Node Left => _nodes[0];

    public Node Right => _nodes[1];

    public override Type ReturnType => _returnType ??= ComputeReturnType();

    protected static string CalculateId<T>(T node)
        where T : BinaryNode
    {
        return $"{typeof(T).Name}{node.Left.Id}{node.Right.Id}{node.ReturnType?.Name}";
    }

    private Type ComputeReturnType()
    {
        var leftType = Left.ReturnType;
        var rightType = Right.ReturnType;
        var leftIsNullOrVoid = IsNullOrVoid(leftType);
        var rightIsNullOrVoid = IsNullOrVoid(rightType);

        if (leftIsNullOrVoid && rightIsNullOrVoid)
        {
            if (leftType is NullNode.NullType && rightType is NullNode.NullType)
                return typeof(object);

            return typeof(void);
        }

        if (leftIsNullOrVoid)
        {
            if (leftType is NullNode.NullType)
                return MakeNullable(rightType);

            return typeof(void);
        }

        if (rightIsNullOrVoid)
        {
            if (rightType is NullNode.NullType)
                return MakeNullable(leftType);

            return typeof(void);
        }


        if (Left is NullNode || Right is NullNode)
        {
            var baseType = NodeHelpers.GetReturnTypeMap(leftType, rightType);
            return MakeNullable(baseType);
        }

        return NodeHelpers.GetReturnTypeMap(leftType, rightType);
    }

    private static bool IsNullOrVoid(Type type)
    {
        return type == null || type == typeof(void) || type is NullNode.NullType;
    }

    private static Type MakeNullable(Type type)
    {
        if (type == null || type == typeof(void) || type is NullNode.NullType)
            return typeof(object);

        if (type.IsValueType && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            return typeof(Nullable<>).MakeGenericType(type);

        return type;
    }
}
