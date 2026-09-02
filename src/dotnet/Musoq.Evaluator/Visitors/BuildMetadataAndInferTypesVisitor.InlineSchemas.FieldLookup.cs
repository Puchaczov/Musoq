using System.Collections.Generic;
using System.Dynamic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private SchemaFieldNode[]? FindInlineSchemaFields(string fieldName)
    {
        if (SchemaRegistry == null) return null;

        foreach (var registration in SchemaRegistry.Schemas)
            if (registration.Node is BinarySchemaNode binaryNode)
            {
                var allFields = GetAllBinarySchemaFields(binaryNode);
                foreach (var field in allFields)
                {
                    if (field is FieldDefinitionNode { TypeAnnotation: InlineSchemaTypeNode inlineSchema }
                        && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return inlineSchema.Fields;
                    }

                    if (field is FieldDefinitionNode
                        {
                            TypeAnnotation: ArrayTypeNode
                            {
                                ElementType: InlineSchemaTypeNode arrayInlineSchema
                            }
                        } && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return arrayInlineSchema.Fields;
                    }

                    if (field is FieldDefinitionNode
                        {
                            TypeAnnotation: RepeatUntilTypeNode
                            {
                                ElementType: InlineSchemaTypeNode repeatInlineSchema
                            }
                        } && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    {
                        return repeatInlineSchema.Fields;
                    }
                }
            }

        return null;
    }

    private BinarySwitchTypeNode? FindSwitchSchemaType(string fieldName)
    {
        if (SchemaRegistry == null) return null;

        foreach (var registration in SchemaRegistry.Schemas)
        {
            if (registration.Node is not BinarySchemaNode binaryNode)
                continue;

            foreach (var field in GetAllBinarySchemaFields(binaryNode))
                if (field is FieldDefinitionNode { TypeAnnotation: BinarySwitchTypeNode switchType }
                    && string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                    return switchType;
        }

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

    private static (Type ClrType, string? IntendedTypeName) ResolveTextFieldClrTypeWithIntendedName(
        string schemaName,
        TextFieldDefinitionNode field)
    {
        if (field is { FieldType: TextFieldType.Pattern, CaptureGroups.Length: > 0 })
            return (typeof(object), $"Musoq.Generated.Interpreters.{schemaName}.CaptureResult_{field.Name}");

        if (field.FieldType == TextFieldType.Repeat)
        {
            var elementSchemaName = field.PrimaryValue ?? "object";
            return (typeof(object[]), $"Musoq.Generated.Interpreters.{elementSchemaName}[]");
        }

        if (field.FieldType == TextFieldType.Switch)
            return (typeof(ExpandoObject), null);

        if (field.FieldType == TextFieldType.SchemaReference)
        {
            var referencedSchemaName = field.PrimaryValue ?? "object";
            return (typeof(object), $"Musoq.Generated.Interpreters.{referencedSchemaName}");
        }

        return (typeof(string), null);
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

        if (field.TypeAnnotation is InlineSchemaTypeNode)
            return (type, CreateInlineSchemaIntendedTypeName(field.Name));

        if (field.TypeAnnotation is ArrayTypeNode { ElementType: InlineSchemaTypeNode })
            return (type, $"{CreateInlineSchemaIntendedTypeName(field.Name)}[]");

        if (field.TypeAnnotation is RepeatUntilTypeNode { ElementType: InlineSchemaTypeNode })
            return (type, $"{CreateInlineSchemaIntendedTypeName(field.Name)}[]");

        return (type, intendedTypeName);
    }

    private static string CreateInlineSchemaIntendedTypeName(string fieldName)
    {
        return $"Musoq.Generated.Interpreters.Inline_{fieldName}";
    }

    private static string CreateSwitchSchemaIntendedTypeName(string fieldName)
    {
        return $"Musoq.Generated.Interpreters.Switch_{fieldName}";
    }
}
