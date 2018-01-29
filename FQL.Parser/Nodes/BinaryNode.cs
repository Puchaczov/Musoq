using System;
using FQL.Parser.Helpers;

namespace FQL.Parser.Nodes
{
    public abstract class BinaryNode : Node
    {
        private readonly Node[] _nodes;

        public BinaryNode(Node left, Node right)
        {
            _nodes = new[] {left, right};
        }

        public Node Left => _nodes[0];

        public Node Right => _nodes[1];

        public override Type ReturnType => Left.ReturnType == typeof(void) || Right.ReturnType == typeof(void) ? typeof(void) : NodeHelper.GetReturnTypeMap(Left.ReturnType, Right.ReturnType);

        protected static string CalculateId<T>(T node)
            where T : BinaryNode
        {
            return $"{typeof(T).Name}{node.Left.Id}{node.Right.Id}{node?.ReturnType.Name}";
        }
    }
}