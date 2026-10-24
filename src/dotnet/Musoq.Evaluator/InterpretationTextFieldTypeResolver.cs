using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Parser;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class InterpretationTextFieldTypeResolver
{
    internal static IEnumerable<TextFieldDefinitionNode> EnumerateDefinitions(IEnumerable<TextFieldDefinitionNode> fields) =>
        EnumerateLocatedDefinitions(fields).Select(item => item.Field);

    internal static IEnumerable<(TextFieldDefinitionNode Field, TextSpan? Span)> EnumerateLocatedDefinitions(
        IEnumerable<TextFieldDefinitionNode> fields, TextSpan? ownerSpan = null)
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

    internal static (Type ClrType, string? IntendedTypeName) Resolve(string schemaName, TextFieldDefinitionNode field, SchemaRegistry? registry = null)
    {
        if (field is { FieldType: TextFieldType.Repeat, RepeatElement: { } element })
        {
            var elementShape = Resolve(schemaName, element, registry);
            var intendedTypeName = elementShape.IntendedTypeName is { } intended ? $"{intended}[]" : null;
            return (elementShape.ClrType.MakeArrayType(), intendedTypeName);
        }

        if (registry != null)
        {
            var shape = new TextResultShapeBinding(registry).Field(schemaName, field);
            var name = shape.TypeName;
            if (shape.Alternatives.Count > 0 || shape.Element?.Alternatives.Count > 0)
            {
                name = $"Musoq.Generated.Interpreters.{schemaName}.TextSwitch_{field.Name}";
                if (shape.Element != null) name += "[]";
            }
            return (shape.ClrType, name);
        }

        return field.FieldType switch
        {
            TextFieldType.Repeat => (typeof(object[]), $"Musoq.Generated.Interpreters.{field.PrimaryValue}[]"),
            TextFieldType.SchemaReference => (typeof(object), $"Musoq.Generated.Interpreters.{field.PrimaryValue}"),
            TextFieldType.Pattern when field.CaptureGroups.Length > 0 =>
                (typeof(object), $"Musoq.Generated.Interpreters.{schemaName}.CaptureResult_{field.Name}"),
            TextFieldType.Switch => (typeof(object),
                $"Musoq.Generated.Interpreters.{schemaName}.TextSwitch_{field.Name}"),
            _ => (typeof(string), null)
        };
    }
}
