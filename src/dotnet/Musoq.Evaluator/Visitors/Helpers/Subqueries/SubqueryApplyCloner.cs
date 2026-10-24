using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers.Subqueries;

internal static class SubqueryApplyCloner
{
    internal static ScalarSubqueryNode Clone(ScalarSubqueryNode node, Stack<Node> nodes)
    {
        if (node is not SubqueryApplyNode apply)
            return new ScalarSubqueryNode(nodes.Pop()).CopySpansFrom(node);
        var bindings = apply.IsBound ? PopBindings(apply, nodes) : null;
        var query = apply.IsBound ? apply.Subquery : nodes.Pop();
        var operand = apply.Operand is null ? null : nodes.Pop();
        return new SubqueryApplyNode(query, apply.Kind, operand, apply.ReturnType, bindings) { Comparison = apply.Comparison }.CopySpansFrom(node);
    }

    internal static SubqueryApplyNode Rewrite(ScalarSubqueryNode node, Stack<Node> nodes)
    {
        if (node is not SubqueryApplyNode apply)
            throw new InvalidOperationException("Subquery must have a selected lowering strategy before rewriting.");
        var query = nodes.Pop();
        var bindings = PopBindings(apply, nodes);
        var operand = apply.Operand is null ? null : nodes.Pop();
        return new SubqueryApplyNode(query, apply.Kind, operand, apply.ReturnType, bindings) { Comparison = apply.Comparison }.CopySpansFrom(node);
    }

    private static Node[] PopBindings(SubqueryApplyNode apply, Stack<Node> nodes)
    {
        var bindings = new Node[apply.Bindings.Count];
        for (var index = bindings.Length - 1; index >= 0; index--)
            bindings[index] = nodes.Pop();
        return bindings;
    }
}
