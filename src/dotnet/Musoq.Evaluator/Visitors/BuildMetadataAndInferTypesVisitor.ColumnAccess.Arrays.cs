using System.Dynamic;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using Musoq.Schema.DataSources;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessObjectArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsColumnAccess)
        {
            var tableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(
                string.IsNullOrEmpty(node.TableAlias) ? _sourceBinding.Identifier : node.TableAlias);

            if (tableSymbol == null)
            {
                if (TryReportUnknownProperty(node.TableAlias ?? _sourceBinding.Identifier, null, node))
                    return;
                var span = node.SpanOrEmpty();
                throw new UnknownPropertyException(node.TableAlias ?? _sourceBinding.Identifier, "unknown", span);
            }

            var column = tableSymbol.GetColumnByAliasAndName(
                string.IsNullOrEmpty(node.TableAlias) ? _sourceBinding.Identifier : node.TableAlias,
                node.ObjectName);

            if (column == null)
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                var span = node.SpanOrEmpty();
                throw new UnknownPropertyException(node.ObjectName, "unknown", span);
            }

            Nodes.Push(node);
            return;
        }

        var parentNode = Nodes.Count > 0 ? Nodes.Peek() : null;
        var parentNodeType = parentNode?.ReturnType;

        var hasValidParentContext = parentNode != null && parentNodeType != null &&
                                    !parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)) &&
                                    !IsRowSourceType(parentNodeType) &&
                                    !IsPrimitiveType(parentNodeType);

        if (!hasValidParentContext)
        {
            var currentTableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_sourceBinding.Identifier);
            var column = currentTableSymbol?.GetColumnByAliasAndName(_sourceBinding.Identifier, node.ObjectName);
            if (column != null && IsIndexableType(column.ColumnType))
            {
                var elementIntendedTypeName = GetArrayElementIntendedTypeName(column.IntendedTypeName);
                var columnAccessNode =
                    new AccessObjectArrayNode(node.Token, column.ColumnType, null, elementIntendedTypeName);
                Nodes.Push(columnAccessNode);
                return;
            }
        }

        if (parentNodeType != null && parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

            foreach (var t in typeHintingAttributes)
            {
                if (t.Name != node.Name) continue;

                Nodes.Push(new AccessObjectArrayNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                return;
            }

            var defaultTypeHintingAttributes =
                parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

            if (defaultTypeHintingAttributes is not null)
            {
                Nodes.Push(new AccessObjectArrayNode(node.Token,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            var type = parentNodeType.GetProperty(node.Name)?.PropertyType ??
                       (_resultShape.TheMostInnerIdentifier?.Name == node.Name
                           ? typeof(object[])
                           : typeof(ExpandoObject[]));
            Nodes.Push(
                new AccessObjectArrayNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, type)));
        }
        else
        {
            var isNotRoot = parentNode is not AccessColumnNode;
            bool isArray;
            bool isIndexer;

            if (isNotRoot && parentNodeType != null)
            {
                PropertyInfo? propertyAccess;
                try
                {
                    propertyAccess = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
                {
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    var nodeSpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        nodeSpan);
                }

                isArray = propertyAccess?.PropertyType.IsArray == true;
                isIndexer = HasIndexer(propertyAccess?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray(
                            $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.",
                            node))
                        return;
                    var notArraySpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Object {parentNodeType.Name} property '{node.Name}' is not an array or indexable type.",
                        notArraySpan);
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan = node.SpanOrEmpty();
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan);
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, propertyAccess));

                return;
            }

            if (parentNodeType != null)
            {
                PropertyInfo? property;
                try
                {
                    property = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
                {
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    var exSpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        exSpan);
                }

                isArray = property?.PropertyType.IsArray == true;
                isIndexer = HasIndexer(property?.PropertyType);

                if (!isArray && !isIndexer)
                {
                    if (TryReportObjectNotArray($"Object {node.Name} is not an array or indexable type.", node))
                        return;
                    var naSpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Object {node.Name} is not an array or indexable type.", naSpan);
                }

                if (property == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan = node.SpanOrEmpty();
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan);
                }

                Nodes.Push(new AccessObjectArrayNode(node.Token, property));
            }
            else
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                var objSpan = node.SpanOrEmpty();
                throw new UnknownPropertyException(
                    node.ObjectName, "unknown", objSpan);
            }
        }
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
