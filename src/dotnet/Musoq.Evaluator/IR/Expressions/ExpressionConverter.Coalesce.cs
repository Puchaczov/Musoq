using Musoq.Parser.Nodes;
using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class ExpressionConverter
{
    private IrExpression ConvertCoalesce(CoalesceNode node)
    {
        if (IsNullLiteral(node.Left))
            return Convert(node.Right);

        if (IsStaticallyNonNullableValueType(node.Left.ReturnType))
            return Convert(node.Left);

        var expressions = new List<IrExpression>();
        CollectCoalesceExpressions(node, expressions);

        return expressions.Count == 1
            ? expressions[0]
            : new Coalesce([.. expressions], RequireReturnType(node));
    }

    private void CollectCoalesceExpressions(Node node, ICollection<IrExpression> expressions)
    {
        if (node is not CoalesceNode coalesce)
        {
            expressions.Add(Convert(node));
            return;
        }

        if (IsNullLiteral(coalesce.Left))
        {
            CollectCoalesceExpressions(coalesce.Right, expressions);
            return;
        }

        if (IsStaticallyNonNullableValueType(coalesce.Left.ReturnType))
        {
            expressions.Add(Convert(coalesce.Left));
            return;
        }

        CollectCoalesceExpressions(coalesce.Left, expressions);
        CollectCoalesceExpressions(coalesce.Right, expressions);
    }

    private static bool IsStaticallyNonNullableValueType(Type? type)
    {
        return type is { IsValueType: true } && Nullable.GetUnderlyingType(type) == null && !IsNullType(type);
    }

    private static bool IsNullLiteral(Node node)
    {
        return node is NullNode || IsNullType(node.ReturnType);
    }

    private static bool IsNullType(Type? type)
    {
        return type is NullNode.NullType;
    }
}