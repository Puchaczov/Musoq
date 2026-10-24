using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed partial class LogicalConstantExpressionFolder
{
    protected override IrExpression VisitSubquery(SubqueryExpression node) => node;
    protected override IrExpression VisitSubqueryParameter(SubqueryParameterRef node) => node;
}
