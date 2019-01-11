using System;

namespace Musoq.Parser.Nodes
{
    public class CoupleNode : Node
    {
        public CoupleNode(SchemaFromNode from, string names)
        {
            SchemaNode = from;
            TableName = names;
        }

        public SchemaFromNode SchemaNode { get; }

        public string TableName { get; }

        public override Type ReturnType => typeof(void);

        public override string Id => $"couple {TableName}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"couple {SchemaNode.ToString()} with table {TableName}";
        }
    }
}
