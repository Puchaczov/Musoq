using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    internal void BindSubqueryScope(Action bind) => _semanticState.Subqueries.BindSubqueryScope(bind);

    public override void Visit(ScalarSubqueryNode node)
    {
        var query = PopSemanticNode();
        var operand = node is Helpers.Subqueries.SubqueryApplyNode { Operand: not null } ? PopSemanticNode() : null;
        PushSemanticNode(_semanticState.Subqueries.BindResult(node, query, operand, ValidateBinaryOperatorOperands));
    }
}
