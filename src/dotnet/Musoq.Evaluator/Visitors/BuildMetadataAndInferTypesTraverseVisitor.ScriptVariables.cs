using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(ScriptVariableDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        node.Accept(Visitor);
    }
}