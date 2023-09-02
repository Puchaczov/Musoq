using System;

namespace Musoq.Parser.Nodes.From
{
    public class SchemaMethodFromNode : FromNode
    {
        internal SchemaMethodFromNode(string schema, string method)
            : base(string.Empty)
        {
            Schema = schema;
            Method = method;
            Id = $"{nameof(SchemaMethodFromNode)}{schema}{method}";
        }
        
        public SchemaMethodFromNode(string schema, string method, Type returnType)
            : base(string.Empty, returnType)
        {
            Schema = schema;
            Method = method;
            Id = $"{nameof(SchemaMethodFromNode)}{schema}{method}";
        }

        public string Schema { get; }

        public string Method { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{Schema}.{Method}";
        }
    }
}