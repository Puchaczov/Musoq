using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class AccessObjectArrayNode : IdentifierNode
    {
        public AccessObjectArrayNode(NumericAccessToken token)
            : base(token.Name)
        {
            Token = token;
            Id = $"{nameof(AccessObjectArrayNode)}{token.Value}";
        }

        public AccessObjectArrayNode(NumericAccessToken token, PropertyInfo propertyInfo)
            : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public NumericAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType => PropertyInfo.PropertyType.GetElementType();

        public override string Id { get; }
        public PropertyInfo PropertyInfo { get; set; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Index}]";
        }
    }
}