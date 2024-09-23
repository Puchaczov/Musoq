namespace Musoq.Parser.Nodes
{
    public class FieldOrderedNode : FieldNode
    {
        public FieldOrderedNode(Node expression, int fieldOrder, string fieldName, Order order) : base(expression, fieldOrder, fieldName)
        {
            Order = order;
        }

        public Order Order { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id => $"{nameof(FieldOrderedNode)}{Expression.Id}{Order}";

        public override string ToString()
        {
            if (Order == Order.Descending)
                return $"{Expression.ToString()} desc";
            
            return Expression.ToString();
        }
    }
}
