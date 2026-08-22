using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(ArrayIndexNode node)
    {
        var index = Nodes.Pop();
        var array = Nodes.Pop();
        Nodes.Push(new ArrayIndexNode(array, index));
    }

}
