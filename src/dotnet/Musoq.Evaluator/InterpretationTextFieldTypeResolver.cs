using System;
using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class InterpretationTextFieldTypeResolver
{
    internal static IEnumerable<TextFieldDefinitionNode> EnumerateDefinitions(IEnumerable<TextFieldDefinitionNode> fields)
    {
        foreach (var field in fields)
        {
            yield return field;
            if (field.RepeatElement is { } element)
                foreach (var nested in EnumerateDefinitions([element]))
                    yield return nested;
        }
    }

    internal static (Type ClrType, string? IntendedTypeName) Resolve(string schemaName, TextFieldDefinitionNode field)
    {
        if (field is { FieldType: TextFieldType.Repeat, RepeatElement: { } element })
        {
            var elementShape = Resolve(schemaName, element);
            var intendedTypeName = elementShape.IntendedTypeName is { } intended ? $"{intended}[]" : null;
            return (elementShape.ClrType.MakeArrayType(), intendedTypeName);
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
