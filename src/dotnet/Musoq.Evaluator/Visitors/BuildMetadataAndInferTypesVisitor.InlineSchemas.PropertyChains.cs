using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.From;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private (Type ClrType, string? IntendedTypeName)? ResolveSchemaPropertyChain(
        string intendedTypeName,
        PropertyFromNode.PropertyNameAndTypePair[] remainingProperties)
    {
        if (SchemaRegistry == null || remainingProperties.Length == 0)
            return null;

        return ResolveSchemaFieldFromIntendedTypeName(
            intendedTypeName,
            remainingProperties[0].PropertyName);
    }

    private (Type ClrType, string? IntendedTypeName)? ResolveSchemaFieldFromIntendedTypeName(
        string intendedTypeName,
        string propertyName)
    {
        if (SchemaRegistry == null)
            return null;

        var reference = ParseSchemaReferenceFromTypeName(intendedTypeName);
        if (!SchemaRegistry.TryGetSchema(reference.SchemaName, out var schemaRegistration))
            return ResolveSwitchSchemaField(reference.SchemaName, propertyName)
                   ?? ResolveInlineSchemaField(reference.SchemaName, propertyName);

        if (schemaRegistration?.Node is BinarySchemaNode binaryNode)
        {
            var allFields = GetAllBinarySchemaFields(binaryNode);
            var field = allFields.OfType<FieldDefinitionNode>().FirstOrDefault(f =>
                string.Equals(f.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (field == null)
                return null;

            var typeParamMap = CreateTypeParameterMap(binaryNode, reference.TypeArguments);
            var resolved = ResolveTypeAnnotationWithSubstitution(field.TypeAnnotation, typeParamMap);
            return ApplyBinaryFieldIntendedTypeName(field, resolved);
        }

        if (schemaRegistration?.Node is TextSchemaNode textNode)
        {
            var field = GetAllTextSchemaFields(textNode)
                .FirstOrDefault(f => string.Equals(f.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            return field is null ? null : ResolveTextFieldClrTypeWithIntendedName(textNode.Name, field);
        }

        return null;
    }

    private (Type ClrType, string? IntendedTypeName)? ResolveInlineSchemaField(
        string schemaName,
        string propertyName)
    {
        if (!schemaName.StartsWith("Inline_", StringComparison.Ordinal))
            return null;

        var inlineFieldName = schemaName["Inline_".Length..];
        var inlineFields = FindInlineSchemaFields(inlineFieldName);
        var field = inlineFields?.FirstOrDefault(f =>
            string.Equals(f.Name, propertyName, StringComparison.OrdinalIgnoreCase));

        return field is FieldDefinitionNode fieldDefinition
            ? ResolveBinaryFieldClrTypeWithIntendedName(fieldDefinition)
            : null;
    }

    private (Type ClrType, string? IntendedTypeName)? ResolveSwitchSchemaField(
        string schemaName,
        string propertyName)
    {
        if (!schemaName.StartsWith("Switch_", StringComparison.Ordinal))
            return null;

        var switchFieldName = schemaName["Switch_".Length..];
        var switchType = FindSwitchSchemaType(switchFieldName);
        if (switchType is null)
            return null;

        if (string.Equals(propertyName, "Case", StringComparison.OrdinalIgnoreCase))
            return (typeof(string), null);

        var branch = switchType.Cases.FirstOrDefault(c =>
            string.Equals(c.BranchAlias, propertyName, StringComparison.OrdinalIgnoreCase));

        if (branch is null)
            return null;

        var resolved = ResolveTypeAnnotationClrTypeWithIntendedName(branch.BranchType);
        if (branch.BranchType is PrimitiveTypeNode && resolved.ClrType.IsValueType)
            return (typeof(Nullable<>).MakeGenericType(resolved.ClrType), resolved.IntendedTypeName);

        return resolved;
    }

    private static Dictionary<string, string> CreateTypeParameterMap(
        BinarySchemaNode binaryNode,
        string[] typeArguments)
    {
        var typeParamMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < binaryNode.TypeParameters.Length && i < typeArguments.Length; i++)
            typeParamMap[binaryNode.TypeParameters[i]] = typeArguments[i];

        return typeParamMap;
    }

    private (Type ClrType, string? IntendedTypeName) ResolveTypeAnnotationWithSubstitution(
        TypeAnnotationNode typeAnnotation,
        IReadOnlyDictionary<string, string> typeParamMap)
    {
        if (typeAnnotation is SchemaReferenceTypeNode schemaRef)
            return ResolveSchemaReferenceTypeWithIntendedName(
                ResolveSchemaReferenceWithSubstitution(schemaRef, typeParamMap));

        if (typeAnnotation is ArrayTypeNode arrayType)
        {
            var (elemType, elemIntended) = ResolveTypeAnnotationWithSubstitution(arrayType.ElementType, typeParamMap);
            return (elemType.MakeArrayType(), elemIntended != null ? $"{elemIntended}[]" : null);
        }

        if (typeAnnotation is RepeatUntilTypeNode repeatUntilType)
        {
            var (elemType, elemIntended) = ResolveTypeAnnotationWithSubstitution(
                repeatUntilType.ElementType,
                typeParamMap);
            return (elemType.MakeArrayType(), elemIntended != null ? $"{elemIntended}[]" : null);
        }


        return ResolveTypeAnnotationClrTypeWithIntendedName(typeAnnotation);
    }

    private SchemaReferenceTypeNode ResolveSchemaReferenceWithSubstitution(
        SchemaReferenceTypeNode schemaRef,
        IReadOnlyDictionary<string, string> typeParamMap)
    {
        if (!schemaRef.IsGenericInstantiation &&
            typeParamMap.TryGetValue(schemaRef.SchemaName, out var substitutedName))
        {
            return ParseSchemaReferenceFromTypeName(substitutedName);
        }

        if (!schemaRef.IsGenericInstantiation)
            return schemaRef;

        var typeArguments = schemaRef.TypeArguments
            .Select(typeArgument => ResolveTypeArgumentWithSubstitution(typeArgument, typeParamMap))
            .ToArray();

        return new SchemaReferenceTypeNode(schemaRef.SchemaName, typeArguments);
    }

    private string ResolveTypeArgumentWithSubstitution(
        string typeArgument,
        IReadOnlyDictionary<string, string> typeParamMap)
    {
        var reference = ParseSchemaReferenceFromTypeName(typeArgument);
        return ResolveSchemaReferenceWithSubstitution(reference, typeParamMap).FullTypeName;
    }
}
