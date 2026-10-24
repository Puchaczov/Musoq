using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(StringConcatenationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var right = PopSemanticNode(nameof(StringConcatenationNode));
        var left = PopSemanticNode(nameof(StringConcatenationNode));
        if (!StringConcatenationTyping.IsOperand(left) || !StringConcatenationTyping.IsOperand(right))
            ThrowOrReportInvalidOperandTypes(left.ReturnType ?? typeof(object), right.ReturnType ?? typeof(object), node,
                "The || operator accepts strings or contextual NULL only. Convert other values explicitly.");
        PushSemanticNode(new StringConcatenationNode(StringConcatenationTyping.Contextualize(left),
            StringConcatenationTyping.Contextualize(right)).CopySpansFrom(node));
    }
}
