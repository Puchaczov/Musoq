using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class AccessPropertyNode : UnaryNode
    {
        public Node Root { get; }

        public bool IsOuter { get; }

        public string Name { get; }

        public AccessPropertyNode(Node root, Node expression, bool isOuter, string name)
            : base(expression)
        {
            Root = root;
            IsOuter = isOuter;
            Name = name;
        }

        public AccessPropertyNode(Node root, Node expression, bool isOuter, string name, PropertyInfo propertyInfo)
            : this(root, expression, isOuter, name)
        {
            PropertyInfo = propertyInfo;
        }

        public override Type ReturnType => Expression.ReturnType;

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return $"{Root.ToString()}.{Expression.ToString()}";
        }
    }
}
