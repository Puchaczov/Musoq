using System.Linq;
using System.Reflection;
using Musoq.Parser.Tokens;

namespace Musoq.Parser.Nodes;

public class AccessObjectKeyNode(KeyAccessToken token) : IdentifierNode(token.Name, null, token.Span)
{
    public enum Destination
    {
        Constant,
        Variable
    }

    public AccessObjectKeyNode(KeyAccessToken token, PropertyInfo? propertyInfo)
        : this(token)
    {
        PropertyInfo = propertyInfo;
    }

    public KeyAccessToken Token { get; } = new(token.Name, token.Key.Trim('\''), token.Span);

    public string ObjectName => Token.Name;

    public override Type? ReturnType
    {
        get
        {
            if (PropertyInfo == null)
                return null;

            return (from propertyInfo in PropertyInfo.PropertyType.GetProperties()
                where propertyInfo.GetIndexParameters().Length == 1
                select propertyInfo.PropertyType).FirstOrDefault();
        }
    }

    public override string Id { get; } = $"{nameof(AccessObjectKeyNode)}{token.Value}";

    public PropertyInfo? PropertyInfo { get; }

    public Destination DestinationKind { get; set; } = token.Value.StartsWith('\'') && token.Value.EndsWith('\'')
        ? Destination.Constant
        : Destination.Variable;

    public override void Accept(IExpressionVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.Visit(this);
    }

    public override string ToString()
    {
        var key = DestinationKind == Destination.Constant ? $"'{Token.Key}'" : Token.Key;

        return $"{ObjectName}[{key}]";
    }
}
