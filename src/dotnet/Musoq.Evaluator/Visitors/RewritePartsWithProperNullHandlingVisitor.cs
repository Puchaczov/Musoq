using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsWithProperNullHandlingVisitor(Type nullableType) : CloneQueryVisitor
{
    public Node RewrittenNode => Nodes.Peek();

    public override void Visit(NullNode node)
    {
        Nodes.Push(new NullNode(nullableType));
    }
}
