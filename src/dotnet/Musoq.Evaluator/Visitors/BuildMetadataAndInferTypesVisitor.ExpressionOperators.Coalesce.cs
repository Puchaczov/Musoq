using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal bool ShouldSkipCoalesceRightOperand()
    {
        var left = SafePeek(Nodes, "Visit(CoalesceNode) left");
        return _expressionBindingService.ShouldSkipCoalesceRightOperand(left);
    }

    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var right = SafePop(Nodes, "Visit(CoalesceNode) right");
        var left = SafePop(Nodes, "Visit(CoalesceNode) left");

        Nodes.Push(_expressionBindingService.BindCoalesce(node, left, right));
    }
}
