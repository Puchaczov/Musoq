using System.Linq;

namespace FQL.Parser.Nodes
{
    public class SchemaFromNode : FromNode
    {
        public SchemaFromNode(string schema, string method, string[] parameters, string alias)
            : base(schema, method, parameters)
        {
            Alias = alias;
            var paramsId = parameters.Length == 0 ? string.Empty : parameters.Aggregate((a, b) => a + b);
            Id = $"{nameof(SchemaFromNode)}{schema}{method}{paramsId}";
        }

        public string Alias { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            var parameters = Parameters.Length == 0 ? string.Empty : Parameters.Aggregate((a, b) => a + "," + b);
            return $"from {Schema}.{Method}({parameters})";
        }
    }
}