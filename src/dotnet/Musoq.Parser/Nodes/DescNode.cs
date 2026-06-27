using Musoq.Parser.Nodes.From;

namespace Musoq.Parser.Nodes;

public class DescNode : Node
{
    private const string QueryAlias = "__desc_query";

    public DescNode(FromNode from, DescForType type)
        : this(from, type, null, null)
    {
    }

    public DescNode(FromNode from, DescForType type, Node? column)
        : this(from, type, column, null)
    {
    }

    public DescNode(Node query)
        : this(new InMemoryTableFromNode(QueryAlias, QueryAlias, typeof(object)), DescForType.Query, null, query)
    {
    }

    private DescNode(FromNode from, DescForType type, Node? column, Node? query)
    {
        From = from;
        Type = type;
        Column = column;
        Query = query;
    }

    public DescForType Type { get; set; }

    public FromNode From { get; }

    public Node? Column { get; }

    public Node? Query { get; }

    public override Type? ReturnType => null;

    public override string Id => Query != null
        ? $"{nameof(DescNode)}Query{Query.Id}"
        : Column != null
            ? $"{nameof(DescNode)}{From.Id}_{Column.Id}"
            : $"{nameof(DescNode)}{From.Id}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        if (Query != null)
            return $"desc query ({Query})";

        var prefix = Type == DescForType.Settings ? "desc settings" : "desc";

        return Column != null
            ? $"{prefix} {From.ToString()} column {Column.ToString()}"
            : $"{prefix} {From.ToString()}";
    }
}
