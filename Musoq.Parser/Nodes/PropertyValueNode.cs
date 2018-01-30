using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class PropertyValueNode : Node
    {
        public string Name { get; }

        public PropertyValueNode(string name)
        {
            Name = name;
        }
        public PropertyValueNode(string name, PropertyInfo propertyInfo)
        {
            Name = name;
            PropertyInfo = propertyInfo;
        }

        public override Type ReturnType { get; }
        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}