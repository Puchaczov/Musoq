using System;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Evaluator;

internal static class StructuralProjectionBinding
{
    internal static StructuralPropertyValueNode? Property(Node? root, string name)
    {
        if (root is null || StructuralProjectionColumns.Describe(root) is not { Kind: StructuralTypeKind.Record } shape)
            return null;
        for (var index = 0; index < shape.Fields.Count; index++)
            if (string.Equals(shape.Fields[index].Name, name, StringComparison.OrdinalIgnoreCase))
                return new StructuralPropertyValueNode(name, shape.Fields[index].Type, index);
        throw new UnknownPropertyException(name, "native structural record", root.SpanOrEmpty());
    }

    internal static DotNode? Dot(Node root, Node expression, DotNode original) =>
        expression is IdentifierNode identifier && Property(root, identifier.Name) is { } property
            ? new DotNode(root, property, original.IsTheMostInner, string.Empty, property.ReturnType)
            : null;

    internal static PropertyValueNode CloneProperty(PropertyValueNode node) => node is StructuralPropertyValueNode structural
        ? new StructuralPropertyValueNode(node.Name, structural.Shape, structural.Index)
        : new PropertyValueNode(node.Name, node.PropertyInfo);
}
