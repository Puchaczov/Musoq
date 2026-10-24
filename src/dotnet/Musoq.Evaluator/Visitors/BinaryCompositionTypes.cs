using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal static class BinaryCompositionTypes
{
    internal static IEnumerable<(string Name, TypeAnnotationNode Type)> Enumerate(string name, TypeAnnotationNode type)
    {
        yield return (name, type);
        IEnumerable<(string Name, TypeAnnotationNode Type)> children = type switch
        {
            ArrayTypeNode array => Enumerate(name, array.ElementType),
            RepeatUntilTypeNode repeat => Enumerate(name, repeat.ElementType),
            SubstreamTypeNode { Target: { } target } => Enumerate(name, target),
            BinarySwitchTypeNode choice => EnumerateBranches(name, choice),
            InlineSchemaTypeNode inline => EnumerateFields(inline.Fields, name),
            _ => []
        };
        foreach (var child in children)
            yield return child;
    }

    internal static IEnumerable<(string Name, TypeAnnotationNode Type)> EnumerateFields(IEnumerable<SchemaFieldNode> fields, string? prefix = null)
    {
        foreach (var field in fields)
            if (field is FieldDefinitionNode parsed)
                foreach (var type in Enumerate(prefix == null ? field.Name : $"{prefix}_{field.Name}", parsed.TypeAnnotation))
                    yield return type;
    }

    private static IEnumerable<(string Name, TypeAnnotationNode Type)> EnumerateBranches(string name, BinarySwitchTypeNode choice)
    {
        foreach (var branch in choice.Cases)
            foreach (var type in Enumerate($"{name}_{branch.BranchAlias}", branch.BranchType))
                yield return type;
    }

    internal static string? IntendedTypeName(string name, TypeAnnotationNode type) => type switch
    {
        InlineSchemaTypeNode => $"Musoq.Generated.Interpreters.Inline_{name}",
        BinarySwitchTypeNode => $"Musoq.Generated.Interpreters.Switch_{name}",
        SubstreamTypeNode { Target: { } target } => IntendedTypeName(name, target),
        ArrayTypeNode array => ArrayName(name, array.ElementType),
        RepeatUntilTypeNode repeat => ArrayName(name, repeat.ElementType),
        _ => null
    };

    private static string? ArrayName(string name, TypeAnnotationNode element) =>
        IntendedTypeName(name, element) is { } intended ? $"{intended}[]" : null;
}
