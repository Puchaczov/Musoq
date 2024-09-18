using System;

namespace Musoq.Parser.Nodes.From
{
    public class SchemaMethodFromNode : FromNode
    {
        internal SchemaMethodFromNode(string alias, string schema, string method)
            : base(alias)
        {
            Schema = schema;
            Method = method;
            Id = $"{nameof(SchemaMethodFromNode)}{schema}{method}";
        }
        
        public SchemaMethodFromNode(string alias, string schema, string method, Type returnType)
            : base(alias, returnType)
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
            if (string.IsNullOrWhiteSpace(Alias))
                return $"{Schema}.{Method}";
            
            return $"{Schema}.{Method} {Alias}";
        }
    }
}