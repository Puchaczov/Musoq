using System;
using System.Linq;

namespace Musoq.Parser.Nodes
{
    public class CreateTableNode : Node
    {
        public CreateTableNode(string schema, string method, string[] parameters, string[] keys, FieldNode[] fields)
        {
            Schema = schema;
            Method = method;
            Parameters = parameters;
            Keys = keys;
            Fields = fields;
            var paramsId = parameters.Length == 0 ? string.Empty : parameters.Aggregate((a, b) => a + b);
            var keysId = keys.Length == 0 ? string.Empty : keys.Aggregate((a, b) => a + b);
            Id = $"{nameof(CreateTableNode)}{schema}{method}{paramsId}{keysId}";
        }

        public string Schema { get; }
        public string Method { get; }
        public string[] Parameters { get; }
        public string[] Keys { get; }
        public FieldNode[] Fields { get; }

        public override Type ReturnType => null;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public override string ToString()
        {
            return $"CREATE TABLE {Schema}";
        }
    }
}