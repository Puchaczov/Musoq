using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal bool ShouldSkipCoalesceRightOperand()
    {
        var left = PeekSemanticNode("Visit(CoalesceNode) left");
        return _expressionBindingService.ShouldSkipCoalesceRightOperand(left);
    }

    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var right = PopSemanticNode("Visit(CoalesceNode) right");
        var left = PopSemanticNode("Visit(CoalesceNode) left");

        PushSemanticNode(_expressionBindingService.BindCoalesce(node, left, right));
    }
}
