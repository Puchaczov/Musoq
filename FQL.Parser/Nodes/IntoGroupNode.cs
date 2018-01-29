using System.Collections.Generic;

namespace FQL.Parser.Nodes
{
    public class IntoGroupNode : IntoNode
    {
        public IntoGroupNode(string name, IDictionary<int, string> columnToValue)
            : base(name)
        {
            ColumnToValue = columnToValue;
        }

        public IDictionary<int, string> ColumnToValue { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}