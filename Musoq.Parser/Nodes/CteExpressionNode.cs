using System;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CteExpressionNode : Node
    {
        public CteExpressionNode(CteInnerExpressionNode[] sets, Node outerSets)
        {
            InnerExpression = sets;
            OuterExpression = outerSets;
        }

        public override Type ReturnType => typeof(void);

        public CteInnerExpressionNode[] InnerExpression { get; }

        public Node OuterExpression { get; }

        public override string Id => $"{nameof(CteExpressionNode)}{OuterExpression.Id}";

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var query = new StringBuilder();

            query.Append("with");
            query.Append(" ");

            for (var i = 0; i < InnerExpression.Length - 1; i++)
            {
                query.Append("(");
                query.Append(InnerExpression[i].ToString());
                query.Append("), ");
            }

            query.Append("(");
            query.Append(InnerExpression[^1].ToString());
            query.Append(") ");
            query.Append(OuterExpression.ToString());

            return query.ToString();
        }
    }
}