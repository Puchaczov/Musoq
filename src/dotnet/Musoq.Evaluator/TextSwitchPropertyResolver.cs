using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class TextSwitchPropertyResolver
{
    internal static (Type ClrType, string? IntendedTypeName)? Resolve(
        SchemaRegistry registry,
        Func<TextSchemaNode, List<TextFieldDefinitionNode>> fields,
        string intendedTypeName,
        string propertyName)
    {
        const string prefix = "Musoq.Generated.Interpreters.";
        const string marker = ".TextSwitch_";
        if (!intendedTypeName.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var markerIndex = intendedTypeName.IndexOf(marker, prefix.Length, StringComparison.Ordinal);
        if (markerIndex < 0)
            return null;

        var schemaName = intendedTypeName[prefix.Length..markerIndex];
        if (!registry.TryGetSchema(schemaName, out var registration) ||
            registration?.Node is not TextSchemaNode schema)
            return null;

        var path = intendedTypeName[(markerIndex + marker.Length)..].Split('.');
        var switchField = InterpretationTextFieldTypeResolver.EnumerateDefinitions(fields(schema)).FirstOrDefault(field =>
            field.FieldType == TextFieldType.Switch && string.Equals(field.Name, path[0], StringComparison.Ordinal));
        if (switchField == null) return null;
        var shape = new TextResultShapeBinding(registry).Field(schema.Name, switchField);
        if (shape.Element != null) shape = shape.Element;
        foreach (var part in path.Skip(1).Append(propertyName))
            shape = shape.Member(part) ?? throw new SourcePlanContractException(
                $"Switch field '{switchField.Name}' does not declare property '{part}' in any branch.");
        var typeName = shape.TypeName;
        if (shape.Alternatives.Count > 0)
            typeName = $"{intendedTypeName}.{propertyName}";
        return (shape.ClrType, typeName);
    }
}
