using System;
using System.Diagnostics;

namespace Musoq.Parser.Nodes
{
    public abstract class Node
    {
        public abstract Type ReturnType { get; }

        [DebuggerStepThrough]
        public abstract void Accept(IExpressionVisitor visitor);

        public abstract string Id { get; }

        public new abstract string ToString();
    }
}