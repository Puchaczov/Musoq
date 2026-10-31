using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class SemanticPropertyParentType
{
    internal static Type Require(Node? parent, PropertyValueNode property)
    {
        if (parent?.ReturnType is null || parent.ReturnType == typeof(void))
            throw new UnknownColumnOrAliasException(property.Name, "while resolving property access", property.SpanOrEmpty());
        if (parent.ReturnType is NullNode.NullType)
            throw new UnknownPropertyException(property.Name, "Null", property.SpanOrEmpty());
        return parent.ReturnType;
    }
}
