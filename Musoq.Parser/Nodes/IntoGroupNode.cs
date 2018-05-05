using System;
using System.Collections.Generic;

namespace Musoq.Parser.Nodes
{
    public class QueryScope : Node
    {
        public QueryScope(Node[] statements)
        {
            Statements = statements;
        }

        public Node[] Statements { get; }

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public override string ToString()
        {
            return null;
        }
    }
}