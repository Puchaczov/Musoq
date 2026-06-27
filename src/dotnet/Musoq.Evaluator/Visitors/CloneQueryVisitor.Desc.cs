using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(DescNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Type == DescForType.Query)
        {
            Nodes.Push(new DescNode(SafePop(Nodes, VisitorOperationNames.VisitDescNode)));
            return;
        }

        var fromNode = SafeCast<FromNode>(SafePop(Nodes, VisitorOperationNames.VisitDescNode),
            VisitorOperationNames.VisitDescNode);
        Nodes.Push(new DescNode(fromNode, node.Type, node.Column));
    }
}
