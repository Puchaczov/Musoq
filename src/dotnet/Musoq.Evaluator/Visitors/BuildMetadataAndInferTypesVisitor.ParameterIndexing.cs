using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(ArrayIndexNode node)
    {
        var index = PopSemanticNode("Visit(ArrayIndexNode).Index");
        var array = PopSemanticNode("Visit(ArrayIndexNode).Array");
        var elementType = array.ReturnType?.IsArray == true
            ? array.ReturnType.GetElementType()
            : null;
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
