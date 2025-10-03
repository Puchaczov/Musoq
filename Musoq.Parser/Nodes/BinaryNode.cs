using System;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class BinaryNode : Node
{
    private readonly Node[] _nodes;
    private readonly Type _returnType;

    protected BinaryNode(Node left, Node right)
    {
        _nodes = [left, right];
        // Cache the return type calculation to avoid exponential complexity
        // when accessing ReturnType recursively during Id calculation
        _returnType = IsNullOrVoid(left.ReturnType) || IsNullOrVoid(right.ReturnType)
            ? typeof(void)
            : NodeHelpers.GetReturnTypeMap(left.ReturnType, right.ReturnType);
    }

    public Node Left => _nodes[0];

    public Node Right => _nodes[1];

    public override Type ReturnType => _returnType;

    protected static string CalculateId<T>(T node)
        where T : BinaryNode
    {
        return $"{typeof(T).Name}{node.Left.Id}{node.Right.Id}{node.ReturnType?.Name}";
    }

    private static bool IsNullOrVoid(Type type)
    {
        return type == null || type == typeof(void);
    }
}