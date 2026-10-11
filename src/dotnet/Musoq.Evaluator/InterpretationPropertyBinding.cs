using Musoq.Parser.Nodes;

namespace Musoq.Evaluator;

internal static class InterpretationPropertyBinding
{
    internal static string? GetIntendedTypeName(Node root) => root switch
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

        var boundProperty = new PropertyValueNode(property.Name,
            new ExpandoObjectPropertyInfo(property.Name, resolved.Value.ClrType));
        return new DotNode(root, boundProperty, node.IsTheMostInner, string.Empty, boundProperty.ReturnType,
            resolved.Value.IntendedTypeName);
    }
}
