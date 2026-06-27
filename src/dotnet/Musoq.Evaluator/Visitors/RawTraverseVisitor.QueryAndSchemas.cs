using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.From;

namespace Musoq.Evaluator.Visitors;

public partial class RawTraverseVisitor<TExpressionVisitor> where TExpressionVisitor : class, IExpressionVisitor
{
    public virtual void Visit(InternalQueryNode node) { }

    public virtual void Visit(RootNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(SingleSetNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(UnionNode node) => TraverseSetOperator(node);

    public virtual void Visit(UnionAllNode node) => TraverseSetOperator(node);

    public virtual void Visit(ExceptNode node) => TraverseSetOperator(node);

    public virtual void Visit(RefreshNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(IntersectNode node) => TraverseSetOperator(node);

    public virtual void Visit(PutTrueNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public virtual void Visit(MultiStatementNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(CteExpressionNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(CteInnerExpressionNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(JoinNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(ApplyNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(OrderByNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(CreateTableNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public virtual void Visit(CoupleNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public virtual void Visit(StatementsArrayNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(StatementNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(CaseNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WhenNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(ThenNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(ElseNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WindowFunctionNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WindowSpecificationNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WindowFrameNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WindowFrameBoundNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    public virtual void Visit(WindowDefinitionNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(WindowNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(QualifyNode node) => VisitChildrenThenNode(node);

    public virtual void Visit(FromNode node) { ArgumentNullException.ThrowIfNull(node); node.Accept(Visitor); }

    private void TraverseSetOperator(SetOperatorNode node) => VisitChildrenThenNode(node);
}
