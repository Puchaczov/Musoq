using System;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes
{
    public class TranslatedSetOperatorNode : Node
    {
        public TranslatedSetOperatorNode(CreateTableNode[] createTableNode, InternalQueryNode fQuery,
            InternalQueryNode sQuery, string resultTableName, string[] keys)
        {
            CreateTableNodes = createTableNode;
            FQuery = fQuery;
            SQuery = sQuery;
            ResultTableName = resultTableName;
            Keys = keys;
            Id =
                $"{nameof(TranslatedSetOperatorNode)}{createTableNode.Select(f => f.Id).Aggregate((a, b) => a + b)}{fQuery.Id}{sQuery.Id}{resultTableName}{keys.Aggregate((a, b) => a + b)}";
        }

        public override Type ReturnType => null;

        public CreateTableNode[] CreateTableNodes { get; }
        public InternalQueryNode FQuery { get; }
        public InternalQueryNode SQuery { get; }
        public string ResultTableName { get; }

        public string[] Keys { get; }

        public override string Id { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            foreach (var table in CreateTableNodes)
            {
                builder.Append(table.ToString());
                builder.Append(Environment.NewLine);
            }

            builder.Append(FQuery.ToString());
            builder.Append(SQuery.ToString());

            return builder.ToString();
        }
    }
}