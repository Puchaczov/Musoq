using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class RewriteQueryTraverseVisitor
{
    public override void Visit(ScalarSubqueryNode node)
    {
        if (node is not SubqueryApplyNode apply)
        {
            base.Visit(node);
            return;
        }
        apply.Operand?.Accept(this);
        foreach (var binding in apply.Bindings)
            binding.Accept(this);
        if (Visitor is RewriteQueryVisitor rewriter)
            rewriter.RewriteSubqueryScope(() => apply.Subquery.Accept(this));
        else
            apply.Subquery.Accept(this);
        apply.Accept(Visitor);
    }
}
