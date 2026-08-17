using System.Dynamic;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(AccessObjectKeyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.DestinationKind == AccessObjectKeyNode.Destination.Variable)
        {
            const string message = "Variable key access is not supported.";
            if (DiagnosticContext != null)
            {
                DiagnosticContext.ReportError(
                DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess,
                    message,
                    node);
                // Keep the semantic stack balanced so this root error does
                // not become an unrelated internal visitor failure.
                PushSemanticNode(new IdentifierNode(node.ToString(), typeof(object), node.SpanOrEmpty()));
                return;
            }

            var keySpan = node.SpanOrEmpty();
            throw new ConstructionNotYetSupported(
                message,
                DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess,
                keySpan);
        }

        var parentNode = PeekSemanticNode(VisitorOperationNames.VisitAccessObjectKeyNode);
        if (parentNode?.ReturnType is not { } parentNodeType)
            throw VisitorException.CreateForProcessingFailure(
                VisitorName,
                VisitorOperationNames.VisitAccessObjectKeyNode,
                $"Parent node has no return type for key access '{node.Name}'"
            );
        if (parentNodeType.IsAssignableTo(typeof(IDynamicMetaObjectProvider)))
        {
            var propertyInfo = _columnPropertyBindingService.ResolveDynamicProperty(
                parentNodeType,
                node.Name,
                typeof(object),
                typeof(ExpandoObject));
            PushSemanticNode(new AccessObjectKeyNode(node.Token, propertyInfo));
        }
        else
        {
            var isRoot = parentNode is AccessColumnNode;

            if (!isRoot)
            {
                var propertyAccess = _columnPropertyBindingService.TryResolveTypedProperty(
                    parentNodeType,
                    node.Name,
                    out var error);

                if (error != null)
                {
                    if (TryReportNoIndexer(
                            $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                            node))
                        return;
                    var exSpan1 = node.SpanOrEmpty();
                    throw new ObjectDoesNotImplementIndexerException(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {error.Message}",
                        exSpan1);
                }

                if (!_columnPropertyBindingService.CanUseAsIndexer(propertyAccess))
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

                PushSemanticNode(new AccessObjectKeyNode(node.Token, propertyAccess));

                return;
            }

            var property = _columnPropertyBindingService.TryResolveTypedProperty(
                parentNodeType,
                node.Name,
                out var rootError);

            if (rootError != null)
            {
                if (TryReportNoIndexer(
                        $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {rootError.Message}", node))
                    return;
                var exSpan2 = node.SpanOrEmpty();
                throw new ObjectDoesNotImplementIndexerException(
                    $"Failed to access property '{node.Name}' on object {parentNodeType.Name}: {rootError.Message}", exSpan2);
            }

            if (!_columnPropertyBindingService.CanUseAsIndexer(property))
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

            PushSemanticNode(new AccessObjectKeyNode(node.Token, property));
        }
    }
}
