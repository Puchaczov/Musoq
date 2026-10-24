using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private SchemaFieldNode[]? FindInlineSchemaFields(string fieldName) =>
        FindBinaryCompositionType<InlineSchemaTypeNode>(fieldName)?.Fields;

    private BinarySwitchTypeNode? FindSwitchSchemaType(string fieldName) =>
        FindBinaryCompositionType<BinarySwitchTypeNode>(fieldName);

    private T? FindBinaryCompositionType<T>(string fieldName) where T : TypeAnnotationNode
    {
        if (SchemaRegistry == null) return null;
        foreach (var registration in SchemaRegistry.Schemas)
            if (registration.Node is BinarySchemaNode binary)
                foreach (var (name, type) in BinaryCompositionTypes.EnumerateFields(GetAllBinarySchemaFields(binary)))
                    if (type is T matching && string.Equals(name, fieldName, StringComparison.OrdinalIgnoreCase))
                        return matching;
        return null;
    }

    private SchemaFieldNode[]? ResolveInlineSchemaFields(string? intendedTypeName)
    {
        if (string.IsNullOrEmpty(intendedTypeName))
            return null;

        var elementIntendedTypeName = intendedTypeName.EndsWith("[]", StringComparison.Ordinal)
            ? intendedTypeName.Substring(0, intendedTypeName.Length - 2)
            : intendedTypeName;
        var schemaName = ParseSchemaReferenceFromTypeName(elementIntendedTypeName).SchemaName;

        if (!schemaName.StartsWith("Inline_", StringComparison.Ordinal))
            return null;

        var fieldName = schemaName.Substring("Inline_".Length);
        return FindInlineSchemaFields(fieldName);
    }

    private (Type ClrType, string? IntendedTypeName) ResolveBinaryFieldClrTypeWithIntendedName(
        FieldDefinitionNode field)
    {
        return ResolveBinaryFieldClrTypeWithIntendedName(
            field,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
    }

    private (Type ClrType, string? IntendedTypeName) ResolveBinaryFieldClrTypeWithIntendedName(
        FieldDefinitionNode field,
        IReadOnlyDictionary<string, string> typeParamMap)
    {
        if (field.TypeAnnotation is BinarySwitchTypeNode)
            return (typeof(object), CreateSwitchSchemaIntendedTypeName(field.Name));

        var (type, intendedTypeName) = ResolveTypeAnnotationWithSubstitution(field.TypeAnnotation, typeParamMap);

        return ApplyBinaryFieldIntendedTypeName(field, (type, intendedTypeName));
    }

    private static string CreateSwitchSchemaIntendedTypeName(string fieldName) =>
        $"Musoq.Generated.Interpreters.Switch_{fieldName}";
}
