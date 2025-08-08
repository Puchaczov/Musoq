using System;
using System.Linq;
using System.Reflection;

namespace Musoq.Parser.Nodes;

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

    public override Type ReturnType
    {
        get
        {
            if (PropertyInfo == null)
                return typeof(string); // String character access returns string (for SQL compatibility)
                
            if (PropertyInfo.PropertyType.IsArray)
                return PropertyInfo.PropertyType.GetElementType();

            return (from propertyInfo in PropertyInfo.PropertyType.GetProperties() where propertyInfo.GetIndexParameters().Length == 1 select propertyInfo.PropertyType).FirstOrDefault();
        }
    }

    public override string Id { get; }
    public PropertyInfo PropertyInfo { get; private set; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        return $"{ObjectName}[{Token.Index}]";
    }
}