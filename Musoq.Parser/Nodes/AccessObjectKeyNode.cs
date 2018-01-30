using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class AccessObjectKeyNode : Node
    {
        public AccessObjectKeyNode(KeyAccessToken token)
        {
            Token = token;
        }
        public AccessObjectKeyNode(KeyAccessToken token, PropertyInfo propertyInfo)
            : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public KeyAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }

        public PropertyInfo PropertyInfo { get; }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Key}]";
        }
    }
}