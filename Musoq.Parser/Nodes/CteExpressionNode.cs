using System;
using System.Collections.Generic;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CteExpressionNode : Node
    {
        private readonly Node _outerSets;

        public CteExpressionNode(CteInnerExpressionNode[] sets, Node outerSets)
        {
            InnerExpression = sets;
            _outerSets = outerSets;
        }

        public override Type ReturnType => typeof(void);
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public CteInnerExpressionNode[] InnerExpression { get; }

        public Node OuterExpression => _outerSets;

        public override string Id => $"{nameof(CteExpressionNode)}{_outerSets.Id}";

        public override string ToString()
        {
            var query = new StringBuilder();

            query.Append("with");
            query.Append(" ");

            for (int i = 0; i < InnerExpression.Length  - 1; i++)
            {
                query.Append("(");
                query.Append(InnerExpression[i].ToString());
                query.Append("), ");
                query.Append(Environment.NewLine);
            }

            query.Append("(");
            query.Append(InnerExpression[InnerExpression.Length - 1].ToString());
            query.Append(") ");
            query.Append(Environment.NewLine);
            query.Append(_outerSets.ToString());

            return query.ToString();
        }
    }
}
