using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class TextCapturePropertyResolver
{
    internal static (Type ClrType, string? IntendedTypeName)? Resolve(
        SchemaRegistry? registry,
        Func<TextSchemaNode, List<TextFieldDefinitionNode>> fields,
        string intendedTypeName,
        string propertyName)
    {
        const string prefix = "Musoq.Generated.Interpreters.";
        const string captureMarker = ".CaptureResult_";
        if (registry == null || !intendedTypeName.StartsWith(prefix, StringComparison.Ordinal))
            return null;

        var markerIndex = intendedTypeName.IndexOf(captureMarker, prefix.Length, StringComparison.Ordinal);
        if (markerIndex < 0)
            return null;

        var schemaName = intendedTypeName[prefix.Length..markerIndex];
        if (!registry.TryGetSchema(schemaName, out var registration) ||
            registration?.Node is not TextSchemaNode schema)
            return null;

        var fieldName = intendedTypeName[(markerIndex + captureMarker.Length)..];
        var field = fields(schema).FirstOrDefault(candidate =>
            candidate.FieldType == TextFieldType.Pattern &&
            string.Equals(candidate.Name, fieldName, StringComparison.OrdinalIgnoreCase));
        return field?.CaptureGroups.Any(group =>
            string.Equals(group, propertyName, StringComparison.OrdinalIgnoreCase)) == true
            ? (typeof(string), null)
            : null;
    }
}
