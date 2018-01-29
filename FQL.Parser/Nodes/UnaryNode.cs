namespace FQL.Parser.Nodes
{
    public abstract class UnaryNode : Node
    {
        protected UnaryNode(Node expression)
        {
            Expression = expression;
        }

        public Node Expression { get; }

        protected static string CalculateId<T>(T node)
            where T : UnaryNode
        {
            return $"{typeof(T).Name}{node.Expression.Id}{node.ReturnType.Name}";
        }
    }
}