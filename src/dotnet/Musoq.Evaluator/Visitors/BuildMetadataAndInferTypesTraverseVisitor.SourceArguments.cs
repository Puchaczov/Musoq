using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(SchemaFromNode node)
    {
        if (Visitor is not BuildMetadataAndInferTypesVisitor metadataVisitor)
        {
            base.Visit(node);
            return;
        }

        var previous = metadataVisitor.SourceArgumentContext;
        metadataVisitor.SourceArgumentContext = new SourceArgumentBindingContext(node);
        try
        {
            base.Visit(node);
        }
        finally
        {
            metadataVisitor.SourceArgumentContext = previous;
        }
    }

}
