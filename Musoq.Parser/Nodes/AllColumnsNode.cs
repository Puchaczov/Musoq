using System;
using System.Collections.Generic;
using System.Text;

namespace FQL.Parser.Nodes
{
    public class AllColumnsNode : Node
    {
        public override Type ReturnType => typeof(object[]);
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{nameof(AllColumnsNode)}*";
        public override string ToString()
        {
            return "*";
        }
    }
}
