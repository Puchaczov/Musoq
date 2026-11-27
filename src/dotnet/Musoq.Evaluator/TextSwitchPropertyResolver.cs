using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class TextSwitchPropertyResolver
{
    internal static (Type ClrType, string? IntendedTypeName)? Resolve(
        SchemaRegistry registry,
        string intendedTypeName,
        string propertyName
    )
    {
        var receiver = Find(registry, intendedTypeName, out var fieldName);
        if (receiver is null)
            return null;
        var member = RequireMember(receiver, fieldName, propertyName);
        var typeName = member.Alternatives.Count == 0 ? member.TypeName : $"{intendedTypeName}.{propertyName}";
        return (member.ClrType, typeName);
    }

    internal static TextResultShape RequireMember(TextResultShape receiver, string? fieldName, string propertyName) =>
        receiver.Member(propertyName)
        ?? throw new SourcePlanContractException(
            $"Switch field '{fieldName}' does not declare property '{propertyName}' in any branch."
        );

    internal static TextResultShape? Find(SchemaRegistry registry, string intendedTypeName) =>
        Find(registry, intendedTypeName, out _);

    private static TextResultShape? Find(SchemaRegistry registry, string intendedTypeName, out string? fieldName)
    {
        fieldName = null;
        var offset = TextResultTypeNames.NestedTypeOffset(intendedTypeName, TextResultTypeNames.SwitchPrefix);
        if (offset < 0)
            return null;
        var schemaName = intendedTypeName[TextResultTypeNames.NamespacePrefix.Length..(offset - 1)].TrimStart('@');
        if (!registry.TryGetSchema(schemaName, out var registration) || registration?.Node is not TextSchemaNode schema)
            return null;
        var path = intendedTypeName[offset..].Split('.');
        var switchField = registry.TextResultTypes(schema).Find(path[0]);
        if (switchField is null)
            return null;
        fieldName = switchField.Name;
        var result = new TextResultShapeBinding(registry).Field(schema.Name, switchField);
        TextResultShape? shape = result.Element ?? result;
        foreach (var part in path.Skip(1))
            shape = shape?.Member(part);
        return shape;
    }
}
