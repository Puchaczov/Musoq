using Musoq.Parser.Tokens;
using System;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class CaseNode : Node
    {
        public CaseNode((Node When, Node Then)[] whenThenNodesPairs, Node elseNode, Type returnType = null)
        {
            WhenThenPairs = whenThenNodesPairs;
            Else = elseNode;
            ReturnType = returnType ?? typeof(void);
        }

        public (Node When, Node Then)[] WhenThenPairs { get; }

        public Node Else { get; }

        public override Type ReturnType { get; }

        public override string Id
        {
            get
            {
                var id = $"{nameof(CaseNode)}";

                foreach(var item in WhenThenPairs)
                {
                    id += $"{item.When.Id}{item.Then.Id}";
                }

                id += $"{Else.Id}";

                return id;
            }
        }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append(CaseToken.TokenText);
            builder.Append(" ");

            foreach(var pair in WhenThenPairs)
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
            builder.Append(Else.ToString());
            builder.Append(" ");
            builder.Append(EndToken.TokenText);

            return builder.ToString();
        }
    }
}