using System;

namespace Musoq.Parser.Nodes
{
    public class SingleSetNode : Node
    {
        public SingleSetNode(QueryNode query)
        {
            Query = query;
        }

        public QueryNode Query { get; }

        public override Type ReturnType => typeof(void);

        public override string Id => $"{nameof(SingleSetNode)}{Query.Id}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Query.ToString();
        }
    }
}