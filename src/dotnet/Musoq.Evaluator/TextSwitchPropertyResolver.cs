using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser;
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

        var fieldName = intendedTypeName[(markerIndex + marker.Length)..];
        var switchField = fields(schema).FirstOrDefault(field =>
            field.FieldType == TextFieldType.Switch &&
            string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        if (switchField == null || switchField.SwitchCases.Length == 0)
            return null;

        return ResolveCommonProperty(registry, fields, switchField, propertyName);
    }

    private static (Type ClrType, string? IntendedTypeName) ResolveCommonProperty(
        SchemaRegistry registry,
        Func<TextSchemaNode, List<TextFieldDefinitionNode>> fields,
        TextFieldDefinitionNode switchField,
        string propertyName)
    {
        (Type ClrType, string? IntendedTypeName)? commonType = null;
        foreach (var arm in switchField.SwitchCases)
        {
            if (!registry.TryGetSchema(arm.TypeName, out var armRegistration) ||
                armRegistration?.Node is not TextSchemaNode armSchema)
                return (typeof(object), null);

            var property = fields(armSchema).FirstOrDefault(field => !field.IsDiscard &&
                string.Equals(field.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (property == null)
                continue;

            var resolved = InterpretationTextFieldTypeResolver.Resolve(armSchema.Name, property);
            if (commonType.HasValue && commonType.Value != resolved)
                return (typeof(object), null);

            commonType = resolved;
        }

        return commonType ?? throw new SourcePlanContractException(
            $"Switch field '{switchField.Name}' does not declare property '{propertyName}' in any branch.", TextSpan.Empty);
    }
}
