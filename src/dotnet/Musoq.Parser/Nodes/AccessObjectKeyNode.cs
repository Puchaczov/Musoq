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

    public AccessObjectKeyNode(KeyAccessToken token, Type columnType, string? tableAlias)
        : this(token)
    {
        ColumnType = columnType;
        TableAlias = tableAlias;
    }

    public AccessObjectKeyNode(AccessObjectKeyNode source)
        : this(source.Token)
    {
        PropertyInfo = source.PropertyInfo;
        ColumnType = source.ColumnType;
        TableAlias = source.TableAlias;
        DestinationKind = source.DestinationKind;
        Span = source.Span;
        FullSpan = source.FullSpan;
    }

    public Type? ColumnType { get; }

    public string? TableAlias { get; }

    public bool IsColumnAccess => ColumnType != null;

    public KeyAccessToken Token { get; } = new(token.Name, token.Key.Trim('\''), token.Span);

    public string ObjectName => Token.Name;

    public override Type? ReturnType
    {
        get
        {
            var indexableType = ColumnType ?? PropertyInfo?.PropertyType;
            if (indexableType == null)
                return null;

            return (from propertyInfo in indexableType.GetProperties()
                where propertyInfo.GetIndexParameters() is [{ ParameterType: var parameterType }] &&
                      parameterType == typeof(string)
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

        var prefix = string.IsNullOrEmpty(TableAlias) ? string.Empty : $"{TableAlias}.";
        return $"{prefix}{ObjectName}[{key}]";
    }
}
