using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(PartialParseCallNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var dataSource = Nodes.Pop();
        Nodes.Push(new PartialParseCallNode(dataSource, node.SchemaName, node.ReturnType));
    }
}
