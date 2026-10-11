using System.Dynamic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(PropertyValueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parentNode = PeekSemanticNode(VisitorOperationNames.VisitPropertyValueNode);
        if (StructuralProjectionBinding.Property(parentNode, node.Name) is { } structural)
        {
            PushSemanticNode(structural);
            return;
        }
        var parentNodeType = SemanticPropertyParentType.Require(parentNode, node);


        if (parentNodeType == typeof(object) || parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var propertyInfo = _columnPropertyBindingService.ResolveDynamicProperty(
                parentNodeType,
                node.Name,
                typeof(object),
                typeof(ExpandoObject));
            PushSemanticNode(new PropertyValueNode(node.Name, propertyInfo));
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
                {
                    // Keep the semantic stack balanced and poison the remainder of
                    // this property chain so later members do not become unrelated
                    // unknown-column diagnostics.
                    PushSemanticNode(new IdentifierNode(node.Name, typeof(object), node.SpanOrEmpty()));
                    return;
                }
                var span = node.SpanOrEmpty();
                throw new UnknownPropertyException(node.Name, parentNodeType.Name, span);
            }

            PushSemanticNode(new PropertyValueNode(node.Name, propertyInfo));
        }
    }
}
