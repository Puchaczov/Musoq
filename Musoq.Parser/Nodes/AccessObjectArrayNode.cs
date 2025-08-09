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

    /// <summary>
    /// Constructor for column-based indexed access (e.g., Name[0], f.Name[0])
    /// </summary>
    public AccessObjectArrayNode(NumericAccessToken token, Type columnType, string tableAlias = null)
        : this(token)
    {
        ColumnType = columnType;
        TableAlias = tableAlias;
        IsColumnAccess = true;
    }

    public NumericAccessToken Token { get; }

    public string ObjectName => Token.Name;

    /// <summary>
    /// True if this represents column access (Name[0]), false if property access (Self.Array[2])
    /// </summary>
    public bool IsColumnAccess { get; private set; }

    /// <summary>
    /// Table alias for column access (null for direct access)
    /// </summary>
    public string TableAlias { get; private set; }

    /// <summary>
    /// Column type for column access
    /// </summary>
    public Type ColumnType { get; private set; }

    public override Type ReturnType
    {
        get
        {
            // Handle column-based indexed access
            if (IsColumnAccess)
            {
                if (ColumnType == typeof(string))
                {
                    // String character access returns string for SQL compatibility
                    return typeof(string);
                }
                
                if (ColumnType.IsArray)
                {
                    return ColumnType.GetElementType();
                }
                
                // Handle other indexable types
                var indexProperty = ColumnType.GetProperties()
                    .FirstOrDefault(p => p.GetIndexParameters().Length == 1);
                return indexProperty?.PropertyType;
            }

            // Handle property-based access (original logic)
            if (PropertyInfo == null)
                return null;
                
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
        var prefix = IsColumnAccess && !string.IsNullOrEmpty(TableAlias) ? $"{TableAlias}." : "";
        return $"{prefix}{ObjectName}[{Token.Index}]";
    }
}