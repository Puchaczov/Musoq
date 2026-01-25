using System;
using System.Diagnostics;

namespace Musoq.Parser.Nodes;

public abstract class Node
{
    public abstract Type ReturnType { get; }

    public abstract string Id { get; }

    [DebuggerStepThrough]
    public abstract void Accept(IExpressionVisitor visitor);

    public new abstract string ToString();
}
