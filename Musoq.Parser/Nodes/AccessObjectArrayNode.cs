using System;
using System.Reflection;

namespace Musoq.Parser.Nodes
{
    public class AccessObjectArrayNode : Node
    {
        public AccessObjectArrayNode(NumericAccessToken token)
        {
            Token = token;
        }
        public AccessObjectArrayNode(NumericAccessToken token, PropertyInfo propertyInfo)
            : this(token)
        {
            PropertyInfo = propertyInfo;
        }

        public NumericAccessToken Token { get; }

        public string ObjectName => Token.Name;

        public override Type ReturnType { get; }

        public override void Accept(IExpressionVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string Id { get; }
        public PropertyInfo PropertyInfo { get; set; }

        public override string ToString()
        {
            return $"{ObjectName}[{Token.Index}]";
        }
    }
}