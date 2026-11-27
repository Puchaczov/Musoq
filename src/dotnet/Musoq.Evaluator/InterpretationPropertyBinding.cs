using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal static class InterpretationPropertyBinding
{
    internal static DotNode CopyBoundDot(Node root, Node expression, DotNode source, string name) =>
        new(
            root,
            expression,
            source.IsTheMostInner,
            name,
            source.ReturnType ?? expression.ReturnType,
            source.IntendedTypeName
        )
        {
            IsNullSafePath = source.IsNullSafePath,
        };

    internal static string? GetIntendedTypeName(Node? root) =>
        root switch
        {
            CaseNode choice when !choice.ReturnType.IsValueType => CommonResultTypeName(
                CaseResults(choice.WhenThenPairs, choice.Else)
            ),
            CoalesceNode fallback when !fallback.ReturnType.IsValueType => CommonResultTypeName([
                fallback.Left,
                fallback.Right,
            ]),
            NullNode value => value.IntendedTypeName,
            AccessColumnNode column => column.IntendedTypeName,
            AccessObjectArrayNode array => array.IntendedTypeName,
            DotNode dot => dot.IntendedTypeName,
            ArrayIndexNode indexed => ArrayElementTypeName(GetIntendedTypeName(indexed.Array)),
            _ => null,
        };

    internal static IEnumerable<Node> CaseResults(IEnumerable<(Node When, Node Then)> branches, Node otherwise)
    {
        foreach (var (_, result) in branches)
            yield return result is ThenNode then ? then.Expression : result;
        yield return otherwise is ElseNode fallback ? fallback.Expression : otherwise;
    }

    private static string? CommonResultTypeName(IEnumerable<Node> results)
    {
        string? common = null;
        foreach (var result in results)
        {
            if (result is NullNode)
                continue;
            if (GetIntendedTypeName(result) is not { } name || common != null && name != common)
                return null;
            common = name;
        }
        return common;
    }

    private static string? ArrayElementTypeName(string? name) =>
        name?.EndsWith("[]", StringComparison.Ordinal) == true ? name[..^2] : null;

    internal static DotNode BindDynamicDotNode(
        Node root,
        Node expression,
        DotNode node,
        Func<string, string, (Type ClrType, string? IntendedTypeName)?> resolveProperty
    )
    {
        var intendedTypeName = GetIntendedTypeName(root);
        if (intendedTypeName == null || expression is not IdentifierNode property)
            return new DotNode(root, expression, node.IsTheMostInner, string.Empty, expression.ReturnType);

        var resolved = resolveProperty(intendedTypeName, property.Name);
        if (!resolved.HasValue)
            return new DotNode(root, expression, node.IsTheMostInner, string.Empty, expression.ReturnType);

        var propertyInfo = new ExpandoObjectPropertyInfo(property.Name, resolved.Value.ClrType);
        Node boundProperty = property is AccessObjectArrayNode indexed
            ? new AccessObjectArrayNode(indexed.Token, propertyInfo)
            : new PropertyValueNode(property.Name, propertyInfo);
        var childName = resolved.Value.IntendedTypeName;
        if (property is AccessObjectArrayNode && childName?.EndsWith("[]", StringComparison.Ordinal) == true)
            childName = childName[..^2];
        return new DotNode(root, boundProperty, node.IsTheMostInner, string.Empty, boundProperty.ReturnType, childName);
    }
}
