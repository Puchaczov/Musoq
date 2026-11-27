using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var right = PopSemanticNode("Visit(CoalesceNode) right");
        var left = PopSemanticNode("Visit(CoalesceNode) left");

        var result = _expressionBindingService.BindCoalesce(node, left, right);
        if (result is CoalesceNode && result.ReturnType?.IsValueType == false)
            RegisterCoalesceInterpretationPresence(result, left, right);
        PushSemanticNode(result);
    }
}
