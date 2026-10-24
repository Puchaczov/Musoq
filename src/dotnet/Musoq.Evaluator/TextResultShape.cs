using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal sealed record TextResultShape(Type ClrType, string? TypeName)
{
    internal IReadOnlyDictionary<string, TextResultShape> Members { get; init; } = new Dictionary<string, TextResultShape>();
    internal IReadOnlyList<TextResultShape> Alternatives { get; init; } = [];
    internal TextResultShape? Element { get; init; }

    internal TextResultShape? Member(string name)
    {
        if (Alternatives.Count == 0)
            return Members.FirstOrDefault(pair => string.Equals(pair.Key, name, StringComparison.Ordinal)).Value;
        var members = Alternatives.Select(arm => arm.Member(name)).OfType<TextResultShape>().ToArray();
        if (members.Length == 0) return null;
        if (members.Length != Alternatives.Count)
            return Merge([.. members, new TextResultShape(typeof(object), null)]);
        return Merge(members);
    }

    internal static TextResultShape Merge(IReadOnlyList<TextResultShape> arms)
    {
        if (arms.Count == 0) return new(typeof(object), null);
        if (arms.All(arm => arm.Alternatives.Count == 0 && arm.ClrType == arms[0].ClrType && arm.TypeName == arms[0].TypeName))
        {
            if (arms[0].Element != null)
                return arms[0] with { Element = Merge(arms.Select(arm => arm.Element!).ToArray()) };
            return arms[0];
        }
        return new(typeof(object), null) { Alternatives = arms.SelectMany(arm => arm.Alternatives.Count > 0 ? arm.Alternatives : [arm]).ToArray() };
    }
}

internal sealed class TextResultShapeBinding(SchemaRegistry registry)
{
    internal TextResultShape Schema(string name)
    {
        if (!registry.TryGetSchema(name, out var registration) || registration?.Node is not TextSchemaNode schema)
            throw new InvalidOperationException($"'{name}' is not a registered text schema.");
        var members = Fields(schema).Where(field => !field.IsDiscard)
            .ToDictionary(field => field.Name, field => Field(schema.Name, field), StringComparer.Ordinal);
        return new(typeof(object), $"Musoq.Generated.Interpreters.{name}") { Members = members };
    }

    private IEnumerable<TextFieldDefinitionNode> Fields(TextSchemaNode schema)
    {
        var fields = new Dictionary<string, TextFieldDefinitionNode>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(schema.Extends) && registry.TryGetSchema(schema.Extends, out var parent) && parent?.Node is TextSchemaNode baseSchema)
            foreach (var field in Fields(baseSchema)) fields[field.Name] = field;
        foreach (var field in schema.Fields) fields[field.Name] = field;
        return fields.Values;
    }

    internal TextResultShape Field(string owner, TextFieldDefinitionNode field)
    {
        if (field.FieldType == TextFieldType.Switch)
            return TextResultShape.Merge(field.SwitchCases.Select(arm => Field(owner, arm.Body)).ToArray());
        if (field.FieldType == TextFieldType.SchemaReference)
            return Schema(field.PrimaryValue!);
        if (field.FieldType == TextFieldType.Repeat)
        {
            var element = field.RepeatElement is { } body ? Field(owner, body) : Schema(field.PrimaryValue!);
            return new(element.ClrType.MakeArrayType(), element.TypeName is { } name ? $"{name}[]" : null) { Element = element };
        }
        if (field.CaptureGroups.Length > 0)
            return new(typeof(object), $"Musoq.Generated.Interpreters.{owner}.CaptureResult_{field.Name}")
            {
                Members = field.CaptureGroups.ToDictionary(name => name, _ => new TextResultShape(typeof(string), null), StringComparer.Ordinal)
            };
        return new(typeof(string), null);
    }
}
