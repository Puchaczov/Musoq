using System;
using System.Collections.Generic;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class BinaryNode : Node
{
    private readonly Node[] _nodes;
    private readonly Type _returnType;

    protected BinaryNode(Node left, Node right)
    {
        _nodes = [left, right];
        
        if (IsNullOrVoid(left.ReturnType) || IsNullOrVoid(right.ReturnType))
        {
            _returnType = typeof(void);
        }
        else
        {
            try
            {
                _returnType = NodeHelpers.GetReturnTypeMap(left.ReturnType, right.ReturnType);
            }
            catch (KeyNotFoundException)
            {
                _returnType = typeof(void);
            }
        }
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