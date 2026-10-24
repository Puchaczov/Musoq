using System.Linq;

namespace Musoq.Evaluator.IR.Expressions;

public abstract partial class ExpressionArrayRewriter
{
    protected override IrExpression VisitSubquery(SubqueryExpression node)
    {
        var bindings = node.Bindings.Select(Visit).ToArray();
        var operand = node.Operand is null ? null : Visit(node.Operand);
        return bindings.SequenceEqual(node.Bindings) && ReferenceEquals(operand, node.Operand)
            ? node
            : node with { Bindings = bindings, Operand = operand };
    }

    protected override IrExpression VisitSubqueryParameter(SubqueryParameterRef node) => node;
}
