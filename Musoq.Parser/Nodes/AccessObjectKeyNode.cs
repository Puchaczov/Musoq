using System;
using System.Linq;
using System.Reflection;

namespace Musoq.Parser.Nodes;

public class AccessObjectKeyNode : IdentifierNode
{
    public AccessObjectKeyNode(KeyAccessToken token)
        : base(token.Name)
    {
        Token = new KeyAccessToken(token.Name, token.Key.Trim('\''), token.Span);
        DestinationKind = token.Value.StartsWith('\'') && token.Value.EndsWith('\'') ? Destination.Constant : Destination.Variable;
        Id = $"{nameof(AccessObjectKeyNode)}{token.Value}";
    }

    public AccessObjectKeyNode(KeyAccessToken token, PropertyInfo propertyInfo)
        : this(token)
    {
        PropertyInfo = propertyInfo;
    }

    public KeyAccessToken Token { get; }

    public string ObjectName => Token.Name;

    public override Type ReturnType
    {
        get
        {
            if (PropertyInfo == null)
                return null;

            return (from propertyInfo in PropertyInfo.PropertyType.GetProperties() where propertyInfo.GetIndexParameters().Length == 1 select propertyInfo.PropertyType).FirstOrDefault();
        }
    }

    public override string Id { get; }

    public PropertyInfo PropertyInfo { get; }
        
    public Destination DestinationKind { get; set; }

    public override void Accept(IExpressionVisitor visitor)
    {
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var key = DestinationKind == Destination.Constant ? $"'{Token.Key}'" : Token.Key;
            
        return $"{ObjectName}[{key}]";
    }

    public enum Destination
    {
        Constant,
        Variable
    }
}