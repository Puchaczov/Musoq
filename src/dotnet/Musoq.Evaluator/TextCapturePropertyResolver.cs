using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class TextCapturePropertyResolver
{
    internal static (Type ClrType, string? IntendedTypeName)? Resolve(
        SchemaRegistry? registry,
        string intendedTypeName,
        string propertyName
    )
    {
        var field = Find(registry, intendedTypeName);
        return field?.CaptureGroups.Contains(propertyName, StringComparer.Ordinal) == true
            ? (typeof(string), null)
            : null;
    }

    internal static TextFieldDefinitionNode? Find(SchemaRegistry? registry, string intendedTypeName)
    {
        var offset = TextResultTypeNames.NestedTypeOffset(intendedTypeName, TextResultTypeNames.CapturePrefix);
        if (registry == null || offset < 0)
            return null;

        var schemaName = intendedTypeName[TextResultTypeNames.NamespacePrefix.Length..(offset - 1)].TrimStart('@');
        if (!registry.TryGetSchema(schemaName, out var registration) || registration?.Node is not TextSchemaNode schema)
            return null;

        return registry.TextResultTypes(schema).Find(intendedTypeName[offset..]);
    }
}
