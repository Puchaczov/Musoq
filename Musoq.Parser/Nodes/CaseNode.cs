using Musoq.Parser.Tokens;
using System;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CaseNode : Node
    {
        public CaseNode((Node When, Node Then)[] whenThenNodesPairs, Node elseNode)
        {
            WhenThenNodesPairs = whenThenNodesPairs;
            ElseNode = elseNode;
        }

        public (Node When, Node Then)[] WhenThenNodesPairs { get; }

        public Node ElseNode { get; }

        public override Type ReturnType => throw new NotImplementedException();

        public override string Id => throw new NotImplementedException();

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(CaseToken.TokenText);
            builder.Append(" ");

            foreach(var pair in WhenThenNodesPairs)
            {
                builder.Append(WhenToken.TokenText);
                builder.Append(" ");
                builder.Append(pair.When.ToString());
                builder.Append(" ");
                builder.Append(ThenToken.TokenText);
                builder.Append(" ");
                builder.Append(pair.Then.ToString());
            }

            builder.Append(" ");
            builder.Append(ElseToken.TokenText);
            builder.Append(" ");
            builder.Append(ElseNode.ToString());
            builder.Append(" ");
            builder.Append(EndToken.TokenText);

            return builder.ToString();
        }
    }
}