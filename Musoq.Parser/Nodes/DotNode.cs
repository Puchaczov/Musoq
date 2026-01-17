using System;

namespace Musoq.Parser.Nodes;

public class DotNode : UnaryNode
{
    public DotNode(Node root, Node expression, string name, Type returnType = null)
        : base(expression)
    {
        Root = root;
        Name = name;
        IsTheMostInner = false;
        Id = $"{nameof(DotNode)}{root.ToString()}{expression.ToString()}{name}";
        ReturnType = returnType;
    }

    public DotNode(Node root, Node expression, bool isTheMostInner, string name, Type returnType = null)
        : base(expression)
    {
        Root = root;
        Name = name;
        IsTheMostInner = isTheMostInner;
        Id = $"{nameof(DotNode)}{root.ToString()}{expression.ToString()}{isTheMostInner}{name}";
        ReturnType = returnType;
    }

    public Node Root { get; }

    public bool IsTheMostInner { get; }

    public string Name { get; }

    public override Type ReturnType { get; }

    public override string Id { get; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{Root.ToString()}.{Expression.ToString()}";
    }
}