using System;
using Musoq.Parser.Helpers;

namespace Musoq.Parser.Nodes;

public abstract class BinaryNode : Node
{
    private readonly Node[] _nodes;

    protected BinaryNode(Node left, Node right)
    {
        _nodes = [left, right];
    }

    public Node Left => _nodes[0];

    public Node Right => _nodes[1];

    public override Type ReturnType => IsNullOrVoid(Left.ReturnType) || IsNullOrVoid(Right.ReturnType)
        ? typeof(void)
        : NodeHelpers.GetReturnTypeMap(Left.ReturnType, Right.ReturnType);

    private static int _calculateIdCalls = 0;
    
    protected static string CalculateId<T>(T node)
        where T : BinaryNode
    {
        _calculateIdCalls++;
        if (_calculateIdCalls % 100 == 0 || _calculateIdCalls <= 50)
        {
            System.Console.WriteLine($"[DEBUG] CalculateId called {_calculateIdCalls} times");
        }
        return $"{typeof(T).Name}{node.Left.Id}{node.Right.Id}{node.ReturnType?.Name}";
    }

    private static bool IsNullOrVoid(Type type)
    {
        return type == null || type == typeof(void);
    }
}