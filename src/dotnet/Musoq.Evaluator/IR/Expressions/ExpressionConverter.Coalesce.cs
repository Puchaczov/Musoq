using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private IrExpression ConvertCoalesce(CoalesceNode node)
    {
        if (CoalesceOperandFacts.KnownResult(node) is { } operand)
            return Convert(operand);

        var expressions = new List<IrExpression>();
        CollectCoalesceExpressions(node, expressions);

        return expressions.Count == 1
            ? expressions[0]
            : new Coalesce([.. expressions], RequireReturnType(node))
            {
                InterpretationTypeName = InterpretationPropertyBinding.GetIntendedTypeName(node),
            };
    }

    private void CollectCoalesceExpressions(Node node, ICollection<IrExpression> expressions)
    {
        if (node is not CoalesceNode coalesce)
        {
            expressions.Add(Convert(node));
            return;
        }

        if (CoalesceOperandFacts.KnownResult(coalesce) is { } operand)
        {
            CollectCoalesceExpressions(operand, expressions);
            return;
        }

        CollectCoalesceExpressions(coalesce.Left, expressions);
        CollectCoalesceExpressions(coalesce.Right, expressions);
    }
}
