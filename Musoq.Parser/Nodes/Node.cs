using System;

namespace Musoq.Parser.Nodes
{
    public abstract class Node
    {
        public abstract Type ReturnType { get; }

        public abstract void Accept(IExpressionVisitor visitor);

        public abstract string Id { get; }

        public new abstract string ToString();
    }
}