using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    public override void Visit(PartialParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = SafePop(Nodes, VisitorOperationNames.VisitPartialParseCallNode);


        Nodes.Push(new PartialParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }
}
