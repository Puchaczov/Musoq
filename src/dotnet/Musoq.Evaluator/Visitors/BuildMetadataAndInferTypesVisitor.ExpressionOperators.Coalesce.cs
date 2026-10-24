using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var right = PopSemanticNode("Visit(CoalesceNode) right");
        var left = PopSemanticNode("Visit(CoalesceNode) left");

        PushSemanticNode(_expressionBindingService.BindCoalesce(node, left, right));
    }
}
