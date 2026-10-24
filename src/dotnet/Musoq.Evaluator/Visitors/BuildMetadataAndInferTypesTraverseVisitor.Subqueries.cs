using Musoq.Evaluator.Visitors.Helpers.Subqueries;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesTraverseVisitor
{
    public override void Visit(ScalarSubqueryNode node)
    {
        if (node is not SubqueryApplyNode apply || Visitor is not BuildMetadataAndInferTypesVisitor binder)
        {
            base.Visit(node);
            return;
        }
        apply.Operand?.Accept(this);
        var depth = _scopes.Count;
        try
        {
            binder.BindSubqueryScope(() =>
            {
                apply.Subquery.Accept(this);
                apply.Accept(Visitor);
            });
        }
        finally
        {
            while (_scopes.Count > depth)
                RestoreScope();
        }
    }
}
