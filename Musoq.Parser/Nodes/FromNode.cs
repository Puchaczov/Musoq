using System;
using Musoq.Schema.DataSources;

namespace Musoq.Parser.Nodes
{
    public abstract class FromNode : Node
    {
        public FromNode(string schema, string method, string[] parameters)
        {
            Schema = schema;
            Method = method;
            Parameters = parameters;
        }

        public string Schema { get; }
        public string Method { get; }
        public string[] Parameters { get; }

        public override Type ReturnType => typeof(RowSource);

        public override string ToString()
        {
            return $"from {Schema}.{Method}";
        }
    }
}