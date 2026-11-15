using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal bool TryBindUnsupportedIndex(ArrayIndexNode node)
    {
        var array = PeekSemanticNode("ArrayIndex.Receiver");
        if (array.ReturnType?.IsArray == true)
            return false;

        var code = DiagnosticCode.MQ3018_NoIndexer;
        var message = $"Expression '{node.Array}' requires a collection and an integer index.";
        if (array.ReturnType?.IsAssignableTo(typeof(System.Collections.IDictionary)) == true)
        {
            code = DiagnosticCode.MQ3096_UnsupportedVariableKeyAccess;
            message = "Variable key access is not supported.";
        }
        else if (node.Array is DotNode { Expression: IdentifierNode property } && node.Index is IdentifierNode key)
        {
            code = DiagnosticCode.MQ3028_UnknownProperty;
            message = $"Unknown property '{property.Name}[{key.Name}]' on '{array.ReturnType?.Name}'.";
        }
        if (DiagnosticContext == null)
            throw new ConstructionNotYetSupported(message, code, node.SpanOrEmpty());
        DiagnosticContext.ReportError(code, message, node);
        PopSemanticNode("ArrayIndex.UnsupportedReceiver");
        PushSemanticNode(new ArrayIndexNode(array, node.Index).CopySpansFrom(node));
        return true;
    }

    public override void Visit(ArrayIndexNode node)
    {
        var index = PopSemanticNode("Visit(ArrayIndexNode).Index");
        var array = PopSemanticNode("Visit(ArrayIndexNode).Array");
        var elementType = array.ReturnType?.IsArray == true ? array.ReturnType.GetElementType() : null;
        if (elementType == null || index.ReturnType != typeof(int))
        {
            var message = $"Expression '{array}' requires a collection and an integer index.";
            if (!TryReportNoIndexer(message, node))
                throw new ObjectDoesNotImplementIndexerException(message, node.SpanOrEmpty());

            PushSemanticNode(new ArrayIndexNode(array, index).CopySpansFrom(node));
            return;
        }

        PushSemanticNode(new ArrayIndexNode(array, index, elementType).CopySpansFrom(node));
    }
}
