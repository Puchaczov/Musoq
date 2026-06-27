using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(DescNode node)
    {
        LoadScope("Desc");
        if (Visitor is BuildMetadataAndInferTypesVisitor enteringVisitor)
            enteringVisitor.EnterDesc(node.Type);

        try
        {
            VisitChildrenThenNode(node);
        }
        finally
        {
            if (Visitor is BuildMetadataAndInferTypesVisitor exitingVisitor)
                exitingVisitor.ExitDesc();

            RestoreScope();
        }
    }
}
