using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private (Type ClrType, string? IntendedTypeName) ResolveTypeAnnotationClrTypeWithIntendedName(
        TypeAnnotationNode typeAnnotation)
    {
        if (typeAnnotation is SchemaReferenceTypeNode schemaRef)
            return ResolveSchemaReferenceTypeWithIntendedName(schemaRef);


        if (typeAnnotation is StringTypeNode stringType &&
            !string.IsNullOrEmpty(stringType.AsTextSchemaName))
        {
            if (SchemaRegistry != null &&
                SchemaRegistry.TryGetSchema(stringType.AsTextSchemaName, out var textSchema))
            {
                if (textSchema?.GeneratedType != null)
                    return (textSchema.GeneratedType, null);

                return (typeof(object), textSchema?.GeneratedTypeName);
            }

            return (typeof(object), null);
        }


        if (typeAnnotation is ArrayTypeNode arrayType)
        {
            var (elementType, elementIntendedTypeName) =
                ResolveTypeAnnotationClrTypeWithIntendedName(arrayType.ElementType);
            var arrayClrType = elementType.MakeArrayType();
            var arrayIntendedTypeName = elementIntendedTypeName != null ? $"{elementIntendedTypeName}[]" : null;
            return (arrayClrType, arrayIntendedTypeName);
        }

        if (typeAnnotation is InlineSchemaTypeNode) return (typeof(object), null);

        if (typeAnnotation is RepeatUntilTypeNode repeatUntilType)
        {
            var (elementType, elementIntendedTypeName) =
                ResolveTypeAnnotationClrTypeWithIntendedName(repeatUntilType.ElementType);
            var arrayClrType = elementType.MakeArrayType();
            var arrayIntendedTypeName = elementIntendedTypeName != null ? $"{elementIntendedTypeName}[]" : null;
            return (arrayClrType, arrayIntendedTypeName);
        }

        return (typeAnnotation.ClrType, null);
    }

    private string QualifyInterpretersTypeArgument(string typeArgument)
    {
        if (string.IsNullOrWhiteSpace(typeArgument))
            return typeArgument;

        var reference = ParseSchemaReferenceFromTypeName(typeArgument);
        if (!reference.IsGenericInstantiation)
            return QualifyInterpretersSimpleTypeArgument(reference.SchemaName);

        var qualifiedTypeArguments = reference.TypeArguments
            .Select(QualifyInterpretersTypeArgument);

        return $"{QualifyInterpretersSimpleTypeArgument(reference.SchemaName)}<{string.Join(",", qualifiedTypeArguments)}>";
    }

    private string QualifyInterpretersSimpleTypeArgument(string typeArgument)
    {
        if (typeArgument.Contains('.', StringComparison.Ordinal))
            return typeArgument;

        if (SchemaRegistry == null)
            return typeArgument;

        return SchemaRegistry.TryGetSchema(typeArgument, out _)
            ? $"Musoq.Generated.Interpreters.{typeArgument}"
            : typeArgument;
    }

    private (Type ClrType, string? IntendedTypeName) ResolveSchemaReferenceTypeWithIntendedName(
        SchemaReferenceTypeNode schemaRef)
    {
        if (SchemaRegistry == null || !SchemaRegistry.TryGetSchema(schemaRef.SchemaName, out var refSchema))
            return (typeof(object), null);

        if (refSchema?.GeneratedType != null && !schemaRef.IsGenericInstantiation)
            return (refSchema.GeneratedType, null);

        var typeName = refSchema?.GeneratedTypeName;
        if (typeName != null && schemaRef.IsGenericInstantiation)
        {
            var qualifiedTypeArguments = schemaRef.TypeArguments
                .Select(QualifyInterpretersTypeArgument);
            typeName = $"{typeName}<{string.Join(",", qualifiedTypeArguments)}>";
        }

        return (typeof(object), typeName);
    }

    private static string StripNamespaceFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var depth = 0;
        var lastDotOutsideAngles = -1;
        for (var i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];
            if (c == '<')
                depth++;
            else if (c == '>')
                depth--;
            else if (c == '.' && depth == 0)
                lastDotOutsideAngles = i;
        }

        return lastDotOutsideAngles >= 0 ? typeName.Substring(lastDotOutsideAngles + 1) : typeName;
    }

    private static SchemaReferenceTypeNode ParseSchemaReferenceFromTypeName(string typeName)
    {
        var elementTypeName = RemoveArraySuffix(typeName.Trim());
        var simpleName = StripNamespaceFromTypeName(elementTypeName);
        var openIndex = simpleName.IndexOf('<', StringComparison.Ordinal);
        if (openIndex < 0 || !simpleName.EndsWith('>'))
            return new SchemaReferenceTypeNode(simpleName);

        var schemaName = simpleName[..openIndex].Trim();
        var argumentText = simpleName[(openIndex + 1)..^1];
        return new SchemaReferenceTypeNode(schemaName, SplitGenericTypeArguments(argumentText));
    }

    private static string RemoveArraySuffix(string typeName)
    {
        return typeName.EndsWith("[]", StringComparison.Ordinal)
            ? typeName[..^2]
            : typeName;
    }

    private static string[] SplitGenericTypeArguments(string argumentText)
    {
        var arguments = new List<string>();
        var depth = 0;
        var start = 0;

        for (var index = 0; index < argumentText.Length; index++)
        {
            var character = argumentText[index];
            if (character == '<')
            {
                depth++;
                continue;
            }

            if (character == '>')
            {
                depth--;
                continue;
            }

            if (character != ',' || depth != 0)
                continue;

            arguments.Add(argumentText[start..index].Trim());
            start = index + 1;
        }

        arguments.Add(argumentText[start..].Trim());
        return arguments.ToArray();
    }
}
