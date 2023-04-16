using System;

namespace Musoq.Parser.Nodes.From
{
    public class SchemaFromNode : FromNode
    {
        internal SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias)
            : base(alias)
        {
            Schema = schema;
            Method = method;
            Parameters = parameters;
            var paramsId = parameters.Id;
            Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}{Alias}";
        }
        
        public SchemaFromNode(string schema, string method, ArgsListNode parameters, string alias, Type returnType)
            : base(alias, returnType)
        {
            Schema = schema;
            Method = method;
            Parameters = parameters;
            var paramsId = parameters.Id;
            Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}{Alias}";
        }

        public string Schema { get; }

        public string Method { get; }

        public ArgsListNode Parameters { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"from {Schema}.{Method}({Parameters.Id}) {Alias}";
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SchemaFromNode node)
                return node.Id == Id;

            return base.Equals(obj);
        }
    }
}