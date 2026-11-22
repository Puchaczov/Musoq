using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class CaseExpressionBinding
{
    internal static Node Bind(
        CaseNode source,
        IReadOnlyList<(Node When, Node Then)> branches,
        Node otherwise,
        IReadOnlyList<Type> resultTypes,
        Func<AccessColumnNode, bool> isStableColumn
    )
    {
        var common = SemanticTypeInferenceService.FindGreatestCommonSubtype(resultTypes);
        if (
            common == typeof(object)
            && resultTypes.Contains(typeof(byte[]))
            && otherwise is ElseNode fallback
            && IsByteArrayResult(fallback.Expression)
            && branches.All(static branch => branch.Then is ThenNode result && IsByteArrayResult(result.Expression))
        )
            common = typeof(byte[]);
        var hasLiteralNull = resultTypes.Any(static type => type == NullNode.NullType.Instance);
        var hasNullableSubquery =
            branches.Any(static pair => SubqueryBindingScope.ContainsNullableSubquery(pair.Then))
            || SubqueryBindingScope.ContainsNullableSubquery(otherwise);
        var type = common;
        if (
            hasLiteralNull
            || hasNullableSubquery
            || CaseExpressionNullability.HasNullableResult(branches, otherwise, isStableColumn)
        )
            type = BuildMetadataAndInferTypesVisitorUtilities.MakeTypeNullable(common);
        var expression = new CaseNode(branches.ToArray(), otherwise, type).CopySpansFrom(source);
        if (!hasLiteralNull)
            return expression;
        var rewrite = new RewritePartsWithProperNullHandlingVisitor(common);
        expression.Accept(new RewritePartsWithProperNullHandlingTraverseVisitor(rewrite));
        return rewrite.RewrittenNode;
    }

    private static bool IsByteArrayResult(Node value) =>
        value.ReturnType == typeof(byte[])
        || value is NullNode
        || value is ArrayLiteralNode array && array.Elements.All(static element => element.ReturnType == typeof(byte));
}
