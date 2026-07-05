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

            var columnAccess = _columnPropertyBindingService.TryCreateColumnArrayAccess(
                tableSymbol,
                string.IsNullOrEmpty(node.TableAlias) ? _sourceBinding.Identifier : node.TableAlias,
                node);

            if (columnAccess == null)
            {
                if (TryReportUnknownProperty(node.ObjectName, null, node))
                    return;
                var span = node.SpanOrEmpty();
                throw new UnknownPropertyException(node.ObjectName, "unknown", span);
            }

            PushSemanticNode(node);
            return;
        }

        var parentNode = TraversalFrame.NodeCount > 0 ? PeekSemanticNode("Visit(AccessObjectArrayNode).Parent") : null;
        var parentNodeType = parentNode?.ReturnType;

        if (!_columnPropertyBindingService.HasTypedParentForArrayAccess(parentNode))
        {
            var currentTableSymbol = _sourceBinding.CurrentScope.ScopeSymbolTable.GetSymbol<TableSymbol>(_sourceBinding.Identifier);
            var columnAccessNode = _columnPropertyBindingService.TryCreateCurrentScopeArrayAccess(currentTableSymbol, node);
            if (columnAccessNode != null)
            {
                PushSemanticNode(columnAccessNode);
                return;
            }
        }

        if (parentNodeType != null && parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var propertyInfo = _columnPropertyBindingService.ResolveDynamicProperty(
                parentNodeType,
                node.Name,
                typeof(object[]),
                typeof(ExpandoObject[]));
            PushSemanticNode(new AccessObjectArrayNode(node.Token, propertyInfo));
        }
        else
        {
            var isNotRoot = parentNode is not AccessColumnNode;

            if (isNotRoot && parentNodeType != null)
            {
                var propertyAccess = _columnPropertyBindingService.TryResolveTypedProperty(
                    parentNodeType,
                    node.Name,
                    out var error);

                if (error != null)
                {
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                            node))
                        return;
                    var nodeSpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                        nodeSpan);
                }

                if (!_columnPropertyBindingService.CanUseAsArrayOrIndexer(propertyAccess))
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

                PushSemanticNode(new AccessObjectArrayNode(node.Token, propertyAccess));

                return;
            }

            if (parentNodeType != null)
            {
                var property = _columnPropertyBindingService.TryResolveTypedProperty(
                    parentNodeType,
                    node.Name,
                    out var error);

                if (error != null)
                {
                    if (TryReportObjectNotArray(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                            node))
                        return;
                    var exSpan = node.SpanOrEmpty();
                    throw new ObjectIsNotAnArrayException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                        exSpan);
                }

                if (!_columnPropertyBindingService.CanUseAsArrayOrIndexer(property))
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

                PushSemanticNode(new AccessObjectArrayNode(node.Token, property));
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

}
