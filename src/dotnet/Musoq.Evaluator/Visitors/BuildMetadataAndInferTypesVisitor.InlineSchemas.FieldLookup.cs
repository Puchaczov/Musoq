using System.Collections.Generic;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private SchemaFieldNode[]? ResolveInlineSchemaFields(string? intendedTypeName)
    {
        if (string.IsNullOrEmpty(intendedTypeName))
            return null;

        var elementIntendedTypeName = intendedTypeName.EndsWith("[]", StringComparison.Ordinal)
            ? intendedTypeName.Substring(0, intendedTypeName.Length - 2)
            : intendedTypeName;
        var schemaName = ParseSchemaReferenceFromTypeName(elementIntendedTypeName).SchemaName;

        return SchemaRegistry?.BinaryCompositions.Find<InlineSchemaTypeNode>(schemaName)?.Fields;
    }

    private (Type ClrType, string? IntendedTypeName) ResolveBinaryFieldClrTypeWithIntendedName(
        FieldDefinitionNode field
    )
    {
        return ResolveBinaryFieldClrTypeWithIntendedName(
            field,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        );
    }

    private (Type ClrType, string? IntendedTypeName) ResolveBinaryFieldClrTypeWithIntendedName(
        FieldDefinitionNode field,
        IReadOnlyDictionary<string, string> typeParamMap
    )
    {
        var (type, intendedTypeName) = ResolveTypeAnnotationWithSubstitution(field.TypeAnnotation, typeParamMap);

        return ApplyBinaryFieldIntendedTypeName(field, (type, intendedTypeName));
    }
}
