using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed class BinaryRepeatConditionTypes(string fieldName, TypeAnnotationNode element, SchemaRegistry registry)
{
    private readonly IReadOnlyList<SchemaFieldNode>? _fields = Fields(element, registry);

    internal bool IsBoolean(Node expression, Func<Node, bool> isKnownBoolean)
    {
        if (isKnownBoolean(expression))
            return true;
        return expression switch
        {
            AndNode and => IsBoolean(and.Left, isKnownBoolean) && IsBoolean(and.Right, isKnownBoolean),
            OrNode or => IsBoolean(or.Left, isKnownBoolean) && IsBoolean(or.Right, isKnownBoolean),
            NotNode not => IsBoolean(not.Expression, isKnownBoolean),
            DotNode dot => ResolveMember(dot) == typeof(bool),
            _ => false
        };
    }

    private Type? ResolveMember(DotNode expression)
    {
        Node root = expression;
        while (root is DotNode dot)
            root = dot.Root;
        var name = root switch
        {
            ArrayIndexNode { Array: IdentifierNode identifier } => identifier.Name,
            IdentifierNode identifier => identifier.Name,
            _ => null
        };
        if (!fieldName.Equals(name, StringComparison.OrdinalIgnoreCase) || _fields is null)
            return null;
        var parts = BinarySchemaExpressionTypes.PathParts(expression).ToArray();
        if (root is IdentifierNode)
            parts = parts.Skip(1).ToArray();
        if (parts.Length == 0)
            return null;
        Node relative = new IdentifierNode(parts[0]);
        foreach (var part in parts.Skip(1))
            relative = new DotNode(relative, new IdentifierNode(part), false, string.Empty);
        return BinarySchemaExpressionTypes.Resolve(relative, _fields, registry);
    }

    private static IReadOnlyList<SchemaFieldNode>? Fields(TypeAnnotationNode annotation, SchemaRegistry registry) => annotation switch
    {
        SubstreamTypeNode { Target: { } target } => Fields(target, registry),
        SchemaReferenceTypeNode reference when registry.TryGetSchema(reference.SchemaName, out var registration) &&
                                               registration?.Node is BinarySchemaNode schema => schema.Fields,
        InlineSchemaTypeNode inline => inline.Fields,
        _ => null
    };
}
