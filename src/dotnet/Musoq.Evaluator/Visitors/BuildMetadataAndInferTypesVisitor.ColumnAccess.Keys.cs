using System.Dynamic;
using System.Reflection;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Plugins.Attributes;
using static Musoq.Evaluator.Visitors.BuildMetadataAndInferTypesVisitorUtilities;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessObjectKeyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
        {
            if (TryReportConstructionNotSupported($"Construction ${node.ToString()} is not yet supported.", node))
                return;
            var keySpan = node.SpanOrEmpty();
            throw new ConstructionNotYetSupported($"Construction ${node.ToString()} is not yet supported.", keySpan);
        }

        var parentNode = SafePeek(Nodes, VisitorOperationNames.VisitAccessObjectKeyNode);
        if (parentNode?.ReturnType is not { } parentNodeType)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessObjectKeyNode,
                $"Parent node has no return type for key access '{node.Name}'"
            );
        if (parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

            foreach (var t in typeHintingAttributes)
            {
                if (t.Name != node.Name) continue;

                Nodes.Push(new AccessObjectKeyNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                return;
            }

            var defaultTypeHintingAttributes =
                parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

            if (defaultTypeHintingAttributes is not null)
            {
                Nodes.Push(new AccessObjectKeyNode(node.Token,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            var type = parentNodeType.GetProperty(node.Name)?.PropertyType ??
                       (_resultShape.TheMostInnerIdentifier?.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
            Nodes.Push(
                new AccessObjectKeyNode(node.Token, new ExpandoObjectPropertyInfo(node.Name, type)));
        }
        else
        {
            var isRoot = parentNode is AccessColumnNode;
            bool isIndexer;

            if (!isRoot)
            {
                PropertyInfo? propertyAccess = null;
                try
                {
                    propertyAccess = parentNodeType.GetProperty(node.Name);
                }
                catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
                {
                    if (TryReportNoIndexer(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                            node))
                        return;
                    var exSpan1 = node.SpanOrEmpty();
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                        exSpan1);
                }

                isIndexer = HasIndexer(propertyAccess?.PropertyType);

                if (!isIndexer)
                {
                    if (TryReportNoIndexer(
                            $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.", node))
                        return;
                    var niSpan1 = node.SpanOrEmpty();
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Object {parentNodeType.Name} property '{node.Name}' does not implement indexer.", niSpan1);
                }

                if (propertyAccess == null)
                {
                    if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                        return;
                    var propSpan1 = node.SpanOrEmpty();
                    throw new UnknownPropertyException(
                        node.Name, parentNodeType.Name, propSpan1);
                }

                Nodes.Push(new AccessObjectKeyNode(node.Token, propertyAccess));

                return;
            }

            PropertyInfo? property = null;
            try
            {
                property = parentNodeType.GetProperty(node.Name);
            }
            catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
            {
                if (TryReportNoIndexer(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}", node))
                    return;
                var exSpan2 = node.SpanOrEmpty();
                throw new ObjectDoesNotImplementIndexerException(
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}", exSpan2);
            }

            isIndexer = HasIndexer(property?.PropertyType);

            if (!isIndexer)
            {
                if (TryReportNoIndexer($"Object {node.Name} does not implement indexer.", node))
                    return;
                var niSpan2 = node.SpanOrEmpty();
                throw new ObjectDoesNotImplementIndexerException(
                    $"Object {node.Name} does not implement indexer.", niSpan2);
            }

            if (property == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                var propSpan2 = node.SpanOrEmpty();
                throw new UnknownPropertyException(
                    node.Name, parentNodeType.Name, propSpan2);
            }

            Nodes.Push(new AccessObjectKeyNode(node.Token, property));
        }
    }
}
