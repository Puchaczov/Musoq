using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

internal sealed class BinaryCompositionTypes
{
    private readonly Dictionary<TypeAnnotationNode, string> _names = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<string, TypeAnnotationNode> _types = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedNames;

    internal BinaryCompositionTypes(SchemaRegistry registry)
    {
        _usedNames = registry.Schemas.Select(schema => schema.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var schema in registry.Schemas.Select(registration => registration.Node).OfType<BinarySchemaNode>())
            Register(schema.Fields);
    }

    internal void Register(IEnumerable<SchemaFieldNode> fields)
    {
        foreach (var (path, type) in EnumerateFields(fields))
        {
            if (type is not (InlineSchemaTypeNode or BinarySwitchTypeNode) || _names.ContainsKey(type))
                continue;
            var prefix = type is InlineSchemaTypeNode ? "Inline_" : "Switch_";
            var preferred = prefix + path;
            var name = preferred;
            for (var suffix = 2; !_usedNames.Add(name); suffix++)
                name = preferred + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            _names.Add(type, name);
            _types.Add(name, type);
        }
    }

    internal string Name(TypeAnnotationNode type) => _names[type];

    internal T? Find<T>(string name)
        where T : TypeAnnotationNode => _types.GetValueOrDefault(name) as T;

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
            _ => [],
        };
        foreach (var child in children)
            yield return child;
    }

    internal static IEnumerable<(string Name, TypeAnnotationNode Type)> EnumerateFields(
        IEnumerable<SchemaFieldNode> fields,
        string? prefix = null
    )
    {
        foreach (var field in fields)
            if (field is FieldDefinitionNode parsed)
                foreach (
                    var type in Enumerate(prefix == null ? field.Name : $"{prefix}_{field.Name}", parsed.TypeAnnotation)
                )
                    yield return type;
    }

    private static IEnumerable<(string Name, TypeAnnotationNode Type)> EnumerateBranches(
        string name,
        BinarySwitchTypeNode choice
    )
    {
        foreach (var branch in choice.Cases)
        foreach (var type in Enumerate($"{name}_{branch.BranchAlias}", branch.BranchType))
            yield return type;
    }

    internal string? IntendedTypeName(TypeAnnotationNode type) =>
        type switch
        {
            InlineSchemaTypeNode or BinarySwitchTypeNode => InterpretationCSharpNames.Qualified(Name(type)),
            SubstreamTypeNode { Target: { } target } => IntendedTypeName(target),
            ArrayTypeNode array => ArrayName(array.ElementType),
            RepeatUntilTypeNode repeat => ArrayName(repeat.ElementType),
            _ => null,
        };

    private string? ArrayName(TypeAnnotationNode element) =>
        IntendedTypeName(element) is { } intended ? $"{intended}[]" : null;
}
