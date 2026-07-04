using System.Dynamic;
using System.Reflection;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

internal sealed class SemanticColumnPropertyBindingService(
    SourceBindingState sourceBinding,
    ResultShapeState resultShape)
{
    public SemanticIdentifierBinding ResolveIdentifier(TableSymbol tableSymbol, string identifierName)
    {
        var column = tableSymbol.GetColumnByAliasAndName(sourceBinding.Identifier, identifierName);

        if (column != null)
            return SemanticIdentifierBinding.ForColumn(column, string.Empty);

        if (tableSymbol.IsCompoundTable)
        {
            var (_, table, sourceAlias) = tableSymbol.GetTableByColumnName(identifierName);
            if (table != null && sourceAlias != null)
            {
                var singleColumn = table.GetColumnsByName(identifierName)[0];
                return SemanticIdentifierBinding.ForColumn(singleColumn, sourceAlias);
            }

            if (identifierName != sourceBinding.Identifier &&
                TryResolveSingleColumnAlias(tableSymbol, identifierName, out var aliasBinding))
            {
                return aliasBinding;
            }
        }

        if (identifierName == sourceBinding.Identifier)
            return SemanticIdentifierBinding.ForIdentifier();

        return SemanticIdentifierBinding.ForUnknown(tableSymbol.GetColumns());
    }

    public ExpandoObjectPropertyInfo ResolveDynamicProperty(
        Type parentType,
        string propertyName,
        Type mostInnerIdentifierType,
        Type fallbackType)
    {
        foreach (var typeHint in GetCachedTypeHintAttributes(parentType))
        {
            if (typeHint.Name == propertyName)
                return new ExpandoObjectPropertyInfo(propertyName, typeHint.Type);
        }

        var defaultTypeHint = parentType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();
        if (defaultTypeHint != null)
            return new ExpandoObjectPropertyInfo(propertyName, defaultTypeHint.Type);

        Type type;
        try
        {
            type = parentType.GetProperty(propertyName)?.PropertyType ?? ResolveDynamicFallback(propertyName,
                mostInnerIdentifierType, fallbackType);
        }
        catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
        {
            type = ResolveDynamicFallback(propertyName, mostInnerIdentifierType, fallbackType);
        }

        return new ExpandoObjectPropertyInfo(propertyName, type);
    }

    public PropertyInfo? TryResolveTypedProperty(Type parentType, string propertyName, out Exception? error)
    {
        try
        {
            error = null;
            return parentType.GetProperty(propertyName);
        }
        catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
        {
            error = ex;
            return null;
        }
    }

    public bool HasTypedParentForArrayAccess(Node? parentNode)
    {
        var parentType = parentNode?.ReturnType;
        return parentNode != null &&
               parentType != null &&
               !parentType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)) &&
               !IsRowSourceType(parentType) &&
               !IsPrimitiveType(parentType);
    }

    public bool CanUseAsArrayOrIndexer(PropertyInfo? propertyInfo)
    {
        return propertyInfo?.PropertyType.IsArray == true || HasIndexer(propertyInfo?.PropertyType);
    }

    public bool CanUseAsIndexer(PropertyInfo? propertyInfo)
    {
        return HasIndexer(propertyInfo?.PropertyType);
    }

    public AccessObjectArrayNode? TryCreateColumnArrayAccess(
        TableSymbol? tableSymbol,
        string alias,
        AccessObjectArrayNode node,
        string? resultTableAlias = null)
    {
        var column = tableSymbol?.GetColumnByAliasAndName(alias, node.ObjectName);
        if (column == null || !IsIndexableType(column.ColumnType))
            return null;

        return CreateColumnArrayAccess(node, column, resultTableAlias);
    }

    public AccessObjectArrayNode? TryCreateCurrentScopeArrayAccess(TableSymbol? tableSymbol, AccessObjectArrayNode node)
    {
        var column = tableSymbol?.GetColumnByAliasAndName(sourceBinding.Identifier, node.ObjectName);
        if (column == null || !IsIndexableType(column.ColumnType))
            return null;

        return CreateColumnArrayAccess(node, column, null);
    }

    public DotNode CreateObjectBackedDotNode(Node root, Node expression, DotNode original)
    {
        var expressionNode = expression is IdentifierNode identNode
            ? new PropertyValueNode(
                identNode.Name,
                new ExpandoObjectPropertyInfo(identNode.Name, typeof(object)))
            : expression;

        return new DotNode(root, expressionNode, original.IsTheMostInner, string.Empty, expressionNode.ReturnType);
    }

    private static AccessObjectArrayNode CreateColumnArrayAccess(
        AccessObjectArrayNode node,
        ISchemaColumn column,
        string? tableAlias)
    {
        var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
        return new AccessObjectArrayNode(node.Token, column.ColumnType, tableAlias, elementIntendedTypeName);
    }

    private bool TryResolveSingleColumnAlias(
        TableSymbol tableSymbol,
        string identifierName,
        out SemanticIdentifierBinding binding)
    {
        binding = default;
        if (!tableSymbol.ContainsAlias(identifierName))
            return false;

        if (!tableSymbol.TryGetColumns(identifierName, out var aliasColumns))
            return false;

        if (aliasColumns is not { Length: 1 })
            return false;

        binding = SemanticIdentifierBinding.ForColumn(aliasColumns[0], identifierName);
        return true;
    }

    private Type ResolveDynamicFallback(string propertyName, Type mostInnerIdentifierType, Type fallbackType)
    {
        return resultShape.TheMostInnerIdentifier?.Name == propertyName
            ? mostInnerIdentifierType
            : fallbackType;
    }

    private static bool IsRowSourceType(Type? type)
    {
        while (type != null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(RowSource<>))
                return true;

            type = type.BaseType;
        }

        return false;
    }
}

internal readonly record struct SemanticIdentifierBinding(
    SemanticIdentifierBindingKind Kind,
    ISchemaColumn? Column,
    string? SourceAlias,
    ISchemaColumn[] AvailableColumns)
{
    public static SemanticIdentifierBinding ForColumn(ISchemaColumn column, string sourceAlias)
    {
        return new SemanticIdentifierBinding(
            SemanticIdentifierBindingKind.Column,
            column,
            sourceAlias,
            []);
    }

    public static SemanticIdentifierBinding ForIdentifier()
    {
        return new SemanticIdentifierBinding(SemanticIdentifierBindingKind.Identifier, null, null, []);
    }

    public static SemanticIdentifierBinding ForUnknown(ISchemaColumn[] availableColumns)
    {
        return new SemanticIdentifierBinding(SemanticIdentifierBindingKind.Unknown, null, null, availableColumns);
    }
}

internal enum SemanticIdentifierBindingKind
{
    Unknown,
    Identifier,
    Column
}
