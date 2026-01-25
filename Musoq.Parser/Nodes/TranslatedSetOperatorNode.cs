using System;
using System.Linq;
using System.Text;

namespace Musoq.Parser.Nodes;

public class TranslatedSetOperatorNode : Node
{
    public TranslatedSetOperatorNode(CreateTransformationTableNode[] createTableNode, InternalQueryNode fQuery,
        InternalQueryNode sQuery, string resultTableName, string[] keys)
    {
        CreateTableNodes = createTableNode;
        FQuery = fQuery;
        SQuery = sQuery;
        ResultTableName = resultTableName;
        Keys = keys;
        Id =
            $"{nameof(TranslatedSetOperatorNode)}{string.Concat(createTableNode.Select(f => f.Id))}{fQuery.Id}{sQuery.Id}{resultTableName}{string.Concat(keys)}";
    }

    public override Type ReturnType => null;

    public CreateTransformationTableNode[] CreateTableNodes { get; }
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
