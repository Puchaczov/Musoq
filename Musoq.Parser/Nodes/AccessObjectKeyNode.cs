using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class AccessObjectKeyNode : IdentifierNode
    {
        public AccessObjectKeyNode(KeyAccessToken token)
            : base(token.Name)
        {
            Token = token;
            Id = $"{nameof(AccessObjectKeyNode)}{token.Value}";
        }

        public AccessObjectKeyNode(KeyAccessToken token, PropertyInfo propertyInfo)
            : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public KeyAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType => PropertyInfo.PropertyType.GetElementType();

        public override string Id { get; }

        public PropertyInfo PropertyInfo { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Key}]";
        }
    }
}