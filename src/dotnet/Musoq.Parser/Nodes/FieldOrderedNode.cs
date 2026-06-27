namespace Musoq.Parser.Nodes;

public class FieldOrderedNode(Node expression, int fieldOrder, string? fieldName, bool hasExplicitFieldName, Order order, NullOrdering nullOrdering = NullOrdering.Default)
    : FieldNode(expression,
        fieldOrder, fieldName, hasExplicitFieldName)
{
    public FieldOrderedNode(Node expression, int fieldOrder, string? fieldName, Order order, NullOrdering nullOrdering = NullOrdering.Default) : this(expression,
        fieldOrder, fieldName, !string.IsNullOrEmpty(fieldName), order, nullOrdering)
    {
    }

    public Order Order { get; } = order;

    public NullOrdering NullOrdering { get; } = nullOrdering;

    public override string Id => $"{nameof(FieldOrderedNode)}{Expression.Id}{Order}{NullOrdering}";

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var order = Order == Order.Descending ? " desc" : string.Empty;
        var nulls = NullOrdering switch
        {
            NullOrdering.First => " nulls first",
            NullOrdering.Last => " nulls last",
            _ => string.Empty
        };

        return $"{Expression.ToString()}{order}{nulls}";
    }
}
