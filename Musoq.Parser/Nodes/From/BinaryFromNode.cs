using System;

namespace Musoq.Parser.Nodes.From;

public abstract class BinaryFromNode : FromNode
{
    protected BinaryFromNode(FromNode source, FromNode with, string alias)
        : base(alias)
    {
        Source = source;
        With = with;
    }

    protected BinaryFromNode(FromNode source, FromNode with, string alias, Type returnType)
        : base(alias, returnType)
    {
        Source = source;
        With = with;
    }

    public FromNode Source { get; }
    public FromNode With { get; }
}
