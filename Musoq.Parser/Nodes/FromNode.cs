using System;
using System.Linq;
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
            var args = Parameters.Length == 0 ? string.Empty : Parameters.Aggregate((a, b) => a + ',' + b);
            return $"from {Schema}.{Method}({args})";
        }
    }

    public class JoinFromNode : FromNode
    {
        public FromNode Source { get; }
        public FromNode With { get; }
        public Node Expression { get; }

        public JoinFromNode(FromNode joinFrom, FromNode from, Node expression) 
            : base(string.Empty, string.Empty, new string[0])
        {
            Source = joinFrom;
            With = @from;
            Expression = expression;
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{typeof(JoinFromNode)}{Source.Id}{With.Id}{Expression.Id}";
    }
}