using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class InterpretationTextFieldTypeResolver
{
    internal static IEnumerable<TextFieldDefinitionNode> EnumerateDefinitions(
        IEnumerable<TextFieldDefinitionNode> fields
    ) => EnumerateLocatedDefinitions(fields).Select(item => item.Field);

    internal static IEnumerable<(TextFieldDefinitionNode Field, TextSpan? Span)> EnumerateLocatedDefinitions(
        IEnumerable<TextFieldDefinitionNode> fields,
        TextSpan? ownerSpan = null
    )
    {
        foreach (var field in fields)
        {
            TextSpan? span = field.HasSpan ? field.Span : ownerSpan;
            yield return (field, span);
            var children = field.SwitchCases.Select(arm => arm.Body);
            if (field.RepeatElement is { } element)
                children = children.Append(element);
            foreach (var nested in EnumerateLocatedDefinitions(children, span))
                yield return nested;
        }
    }

    internal static bool MayBeNull(TextFieldDefinitionNode field) =>
        (field.Modifiers & TextFieldModifier.Optional) != 0
        || field.FieldType == TextFieldType.Switch && field.SwitchCases.Any(arm => MayBeNull(arm.Body));

    internal static (Type ClrType, string? IntendedTypeName) Resolve(
        string schemaName,
        TextFieldDefinitionNode field,
        SchemaRegistry? registry = null,
        TextResultTypeNames? names = null
    )
    {
        if (field is { FieldType: TextFieldType.Repeat, RepeatElement: { } element })
        {
            var elementShape = Resolve(schemaName, element, registry, names);
            var intendedTypeName = elementShape.IntendedTypeName is { } intended ? $"{intended}[]" : null;
            return (elementShape.ClrType.MakeArrayType(), intendedTypeName);
        }

        if (registry != null && field.FieldType == TextFieldType.Switch)
        {
            var shape = new TextResultShapeBinding(registry).Field(schemaName, field);
            var name = shape.TypeName;
            if (shape.Alternatives.Count > 0 || shape.Element?.Alternatives.Count > 0)
            {
                name = registry.TextResultTypes(schemaName).Qualified(field);
                if (shape.Element != null)
                    name += "[]";
            }
            return (shape.ClrType, name);
        }

        return field.FieldType switch
        {
            TextFieldType.Repeat => (typeof(object[]), $"{InterpretationCSharpNames.Qualified(field.PrimaryValue!)}[]"),
            TextFieldType.SchemaReference => (typeof(object), InterpretationCSharpNames.Qualified(field.PrimaryValue!)),
            TextFieldType.Pattern when field.CaptureGroups.Length > 0 => (
                typeof(object),
                (names ?? registry!.TextResultTypes(schemaName)).Qualified(field)
            ),
            TextFieldType.Switch => (typeof(object), names!.Qualified(field)),
            _ => (typeof(string), null),
        };
    }
}
