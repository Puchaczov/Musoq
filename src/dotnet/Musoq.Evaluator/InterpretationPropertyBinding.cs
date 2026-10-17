using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal static class InterpretationPropertyBinding
{
    internal static DotNode CopyBoundDot(Node root, Node expression, DotNode source, string name) =>
        new(root, expression, source.IsTheMostInner, name, source.ReturnType ?? expression.ReturnType, source.IntendedTypeName)
        { IsNullSafePath = source.IsNullSafePath };

    internal static string? GetIntendedTypeName(Node? root) => root switch
    {
        AccessColumnNode column => column.IntendedTypeName,
        AccessObjectArrayNode array => array.IntendedTypeName,
        DotNode dot => dot.IntendedTypeName,
        _ => null
    };

    internal static DotNode BindDynamicDotNode(Node root, Node expression, DotNode node,
        Func<string, string, (Type ClrType, string? IntendedTypeName)?> resolveProperty)
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
        return new DotNode(root, boundProperty, node.IsTheMostInner, string.Empty, boundProperty.ReturnType,
            childName);
    }
}
