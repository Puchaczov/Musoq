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
            var propertyInfo = _columnPropertyBindingService.ResolveDynamicProperty(
                parentNodeType,
                node.Name,
                typeof(object),
                typeof(ExpandoObject));
            Nodes.Push(new PropertyValueNode(node.Name, propertyInfo));
        }
        else
        {
            var propertyInfo = _columnPropertyBindingService.TryResolveTypedProperty(
                parentNodeType,
                node.Name,
                out var error);

            if (error != null)
            {
                throw new VisitorException(
                    VisitorName,
                    VisitorOperationNames.VisitPropertyValueNode,
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                    error);
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
