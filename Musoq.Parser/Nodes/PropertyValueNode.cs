using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class PropertyValueNode : IdentifierNode
    {
        public PropertyValueNode(string name)
            : base(name)
        {
            Id = $"{nameof(PropertyValueNode)}{name}";
        }

        public PropertyValueNode(string name, PropertyInfo propertyInfo)
            : base(name)
        {
            Id = $"{nameof(PropertyValueNode)}{name}";
            PropertyInfo = propertyInfo;
        }

        public override Type ReturnType => PropertyInfo?.PropertyType;

        public override string Id { get; }
        
        public PropertyInfo PropertyInfo { get; private set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}