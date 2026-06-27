using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class SubqueryToCteRewriteVisitor
{
    private sealed class PredicateSubqueryExpressionTraverser(PredicateSubqueryExpressionRewriter visitor)
        : CloneTraverseVisitor(visitor)
    {
        public override void Visit(InQueryNode node)
        {
            node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(ExistsQueryNode node)
        {
            node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(NotNode node)
        {
            if (node.Expression is InQueryNode or ExistsQueryNode)
            {
                node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
                return;
            }

            base.Visit(node);
        }

        public override void Visit(ScalarSubqueryNode node)
        {
            node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(WindowFunctionNode node)
        {
            node.FunctionCall.Accept(this);
            node.WindowSpecification?.Accept(this);
            node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
        }

        public override void Visit(WindowSpecificationNode node)
        {
            foreach (var field in node.PartitionFields)
                field.Accept(this);
            foreach (var field in node.OrderByFields)
                field.Accept(this);
            node.Accept((PredicateSubqueryExpressionRewriter)Visitor);
        }
    }
}
