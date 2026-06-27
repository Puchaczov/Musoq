using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(CoalesceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        node.Left.Accept(this);
        if (Visitor is BuildMetadataAndInferTypesVisitor metadataVisitor &&
            metadataVisitor.ShouldSkipCoalesceRightOperand())
            return;

        node.Right.Accept(this);
        node.Accept(Visitor);
    }
}