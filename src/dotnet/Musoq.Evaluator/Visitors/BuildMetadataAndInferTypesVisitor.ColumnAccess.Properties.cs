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
    public override void Visit(PropertyValueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parentNode = SafePeek(Nodes, VisitorOperationNames.VisitPropertyValueNode);
        if (parentNode?.ReturnType == null || parentNode.ReturnType == typeof(void))
        {
            var span = node.SpanOrEmpty();
            throw new UnknownColumnOrAliasException(
                node.Name,
                "while resolving property access",
                span);
        }

        var parentNodeType = parentNode.ReturnType;

        if (parentNodeType is NullNode.NullType)
        {
            var span = node.SpanOrEmpty();
            throw new UnknownPropertyException(node.Name, "Null", span);
        }


        if (parentNodeType == typeof(object) || parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var typeHintingAttributes = GetCachedTypeHintAttributes(parentNodeType);

            foreach (var t in typeHintingAttributes)
            {
                if (t.Name != node.Name) continue;

                Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, t.Type)));
                return;
            }

            var defaultTypeHintingAttributes =
                parentNodeType.GetCustomAttribute<DynamicObjectPropertyDefaultTypeHintAttribute>();

            if (defaultTypeHintingAttributes is not null)
            {
                Nodes.Push(new PropertyValueNode(node.Name,
                    new ExpandoObjectPropertyInfo(node.Name, defaultTypeHintingAttributes.Type)));
                return;
            }

            Type type;
            try
            {
                var propertyInfo = parentNode.ReturnType.GetProperty(node.Name);
                type = propertyInfo?.PropertyType ??
                       (_resultShape.TheMostInnerIdentifier?.Name == node.Name ? typeof(object) : typeof(ExpandoObject));
            }
            catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
            {
                type = _resultShape.TheMostInnerIdentifier?.Name == node.Name ? typeof(object) : typeof(ExpandoObject);
            }

            Nodes.Push(new PropertyValueNode(node.Name, new ExpandoObjectPropertyInfo(node.Name, type)));
        }
        else
        {
            PropertyInfo? propertyInfo = null;
            try
            {
                propertyInfo = parentNodeType.GetProperty(node.Name);
            }
            catch (Exception ex) when (ex is AmbiguousMatchException or ArgumentException)
            {
                throw new VisitorException(
                    VisitorName,
                    VisitorOperationNames.VisitPropertyValueNode,
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {ex.Message}",
                    ex);
            }

            if (propertyInfo == null)
            {
                if (TryReportUnknownProperty(node.Name, parentNodeType, node))
                    return;
                var span = node.SpanOrEmpty();
                throw new UnknownPropertyException(node.Name, parentNodeType.Name, span);
            }

            Nodes.Push(new PropertyValueNode(node.Name, propertyInfo));
        }
    }
}
