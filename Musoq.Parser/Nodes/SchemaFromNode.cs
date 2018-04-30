using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class SchemaFromNode : FromNode
    {
        public SchemaFromNode(string schema, string method, string[] parameters, string alias)
            : base(alias)
        {
            Schema = schema;
            Method = method;
            Parameters = parameters;
            var paramsId = parameters.Length == 0 ? string.Empty : parameters.Aggregate((a, b) => a + b);
            Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}{Alias}";
        }

        public string Schema { get; }

        public string Method { get; }

        public string[] Parameters { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            var parameters = Parameters.Length == 0 ? string.Empty : Parameters.Aggregate((a, b) => a + "," + b);
            return $"from {Schema}.{Method}({parameters}) {Alias}";
        }
    }
}