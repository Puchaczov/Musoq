using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor
{
    public override void Visit(WindowFunctionNode node)
    {
        node.FunctionCall.Accept(this);
        node.WindowSpecification?.Accept(this);
        node.Accept(Visitor);
    }

    public override void Visit(WindowSpecificationNode node) => VisitChildrenThenNode(node);

    public override void Visit(WindowFrameNode node) => node.Accept(Visitor);

    public override void Visit(WindowFrameBoundNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public override void Visit(WindowDefinitionNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public override void Visit(WindowNode node) => VisitChildrenThenNode(node);
}
