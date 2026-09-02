using System.Collections.Generic;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.Visitors;

internal sealed record EnumBindingState
{
    public Dictionary<string, EnumTypeDescriptor> QueryLocalTypes { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Type, EnumTypeDescriptor> NativeTypes { get; } = new();

    public Dictionary<Node, EnumTypeDescriptor> ExpressionTypes { get; } =
        new(ReferenceEqualityComparer.Instance);
}
