using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class CloneTraverseVisitor(IExpressionVisitor visitor) : RawTraverseVisitor<IExpressionVisitor>(visitor)
{
    public override void Visit(WindowFunctionNode node)
    {
        node.Accept(Visitor);
    }
}
