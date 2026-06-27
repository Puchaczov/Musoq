using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal static class InterpretationPropertyTypeNameResolver
{
    public static string? ResolveEnumerableTypeName(
        PhysicalPropertySourceNode property,
        SchemaRegistry? schemaRegistry)
    {
        string? currentTypeName = null;

        foreach (var propertyPart in property.PropertiesChain)
        {
            if (!string.IsNullOrWhiteSpace(propertyPart.IntendedTypeName))
            {
                currentTypeName = propertyPart.IntendedTypeName;
                continue;
            }

            if (string.IsNullOrWhiteSpace(currentTypeName))
                continue;

            currentTypeName = ResolveFieldTypeName(currentTypeName, propertyPart.PropertyName, schemaRegistry);
            if (string.IsNullOrWhiteSpace(currentTypeName))
                return null;
        }

        return !string.IsNullOrWhiteSpace(currentTypeName)
            ? currentTypeName
            : ResolveFromRegisteredSchemaPath(property, schemaRegistry);
    }

    public static bool HasGeneratedEnumerableElementType(
        PhysicalPropertySourceNode property,
        SchemaRegistry? schemaRegistry)
    {
        return !string.IsNullOrWhiteSpace(ResolveEnumerableTypeName(property, schemaRegistry));
    }

    private static string? ResolveFieldTypeName(
        string typeName,
        string fieldName,
        SchemaRegistry? schemaRegistry)
    {
        if (schemaRegistry == null)
            return null;

        var reference = ParseSchemaReference(typeName);
        if (!schemaRegistry.TryGetSchema(reference.SchemaName, out var registration) ||
            registration?.Node is not BinarySchemaNode binary)
        {
            return null;
        }

        var bindings = CreateBindings(binary, reference);
        var field = FindBinaryField(binary, fieldName, schemaRegistry);
        return field == null
            ? null
            : ResolveFieldTypeName(field, bindings, schemaRegistry);
    }

    private static IReadOnlyDictionary<string, SchemaReferenceTypeNode> CreateBindings(
        BinarySchemaNode binary,
        SchemaReferenceTypeNode reference)
    {
        if (!binary.IsGeneric || reference.TypeArguments.Length == 0)
            return BinarySchemaGenericResolver.CreateEmptyBindings();

        return BinarySchemaGenericResolver.CreateBindings(
            binary,
            reference,
            BinarySchemaGenericResolver.CreateEmptyBindings());
    }

    private static SchemaFieldNode? FindBinaryField(
        BinarySchemaNode binary,
        string fieldName,
        SchemaRegistry schemaRegistry)
    {
        if (!string.IsNullOrWhiteSpace(binary.Extends) &&
            schemaRegistry.TryGetSchema(binary.Extends, out var parentRegistration) &&
            parentRegistration?.Node is BinarySchemaNode parent)
        {
            var inherited = FindBinaryField(parent, fieldName, schemaRegistry);
            if (inherited != null)
                return inherited;
        }

        return binary.Fields.FirstOrDefault(
            field => string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveFieldTypeName(
        SchemaFieldNode field,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> bindings,
        SchemaRegistry? schemaRegistry)
    {
        if (field is not FieldDefinitionNode definition)
            return null;

        return definition.TypeAnnotation switch
        {
            SchemaReferenceTypeNode reference =>
                CreateGeneratedTypeName(
                    BinarySchemaGenericResolver.ResolveReference(reference, bindings),
                    schemaRegistry),
            ArrayTypeNode { ElementType: SchemaReferenceTypeNode reference } =>
                $"{CreateGeneratedTypeName(BinarySchemaGenericResolver.ResolveReference(reference, bindings), schemaRegistry)}[]",
            RepeatUntilTypeNode { ElementType: SchemaReferenceTypeNode reference } =>
                $"{CreateGeneratedTypeName(BinarySchemaGenericResolver.ResolveReference(reference, bindings), schemaRegistry)}[]",
            InlineSchemaTypeNode => CreateInlineTypeName(field.Name),
            ArrayTypeNode { ElementType: InlineSchemaTypeNode } => $"{CreateInlineTypeName(field.Name)}[]",
            RepeatUntilTypeNode { ElementType: InlineSchemaTypeNode } => $"{CreateInlineTypeName(field.Name)}[]",
            _ => null
        };
    }

    private static string CreateInlineTypeName(string fieldName)
    {
        return $"Musoq.Generated.Interpreters.Inline_{fieldName}";
    }

    private static SchemaReferenceTypeNode ParseSchemaReference(string typeName)
    {
        var elementTypeName = RemoveArraySuffix(typeName.Trim());
        var simpleTypeName = StripNamespaceFromTypeName(elementTypeName);
        var genericStart = simpleTypeName.IndexOf('<', StringComparison.Ordinal);
        if (genericStart < 0)
            return new SchemaReferenceTypeNode(simpleTypeName);

        var schemaName = simpleTypeName[..genericStart].Trim();
        var genericEnd = simpleTypeName.LastIndexOf('>');
        var argumentText = genericEnd > genericStart
            ? simpleTypeName.Substring(genericStart + 1, genericEnd - genericStart - 1)
            : string.Empty;

        return new SchemaReferenceTypeNode(
            schemaName,
            SplitGenericTypeArguments(argumentText)
                .Select(argument => ParseSchemaReference(argument).FullTypeName)
                .ToArray());
    }

    private static string? ResolveFromRegisteredSchemaPath(
        PhysicalPropertySourceNode property,
        SchemaRegistry? schemaRegistry)
    {
        if (schemaRegistry == null)
            return null;

        var path = property.PropertiesChain
            .SelectMany(part => part.PropertyName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
        if (path.Length == 0)
            return null;

        foreach (var registration in schemaRegistry.Schemas)
        {
            if (registration.Node is not BinarySchemaNode)
                continue;

            for (var start = 0; start < path.Length; start++)
            {
                var typeName = ResolvePathFromTypeName(registration.Name, path.AsSpan(start), schemaRegistry);
                if (!string.IsNullOrWhiteSpace(typeName))
                    return typeName;
            }
        }

        return null;
    }

    private static string? ResolvePathFromTypeName(
        string rootTypeName,
        ReadOnlySpan<string> path,
        SchemaRegistry schemaRegistry)
    {
        var currentTypeName = rootTypeName;

        foreach (var part in path)
        {
            currentTypeName = ResolveFieldTypeName(currentTypeName, part, schemaRegistry);
            if (string.IsNullOrWhiteSpace(currentTypeName))
                return null;
        }

        return currentTypeName;
    }

    private static string CreateGeneratedTypeName(
        SchemaReferenceTypeNode reference,
        SchemaRegistry? schemaRegistry)
    {
        var typeName = reference.SchemaName;
        if (schemaRegistry != null &&
            schemaRegistry.TryGetSchema(reference.SchemaName, out var registration) &&
            !string.IsNullOrWhiteSpace(registration?.GeneratedTypeName))
        {
            typeName = registration.GeneratedTypeName;
        }

        if (!reference.IsGenericInstantiation)
            return typeName;

        var arguments = reference.TypeArguments
            .Select(argument => CreateGeneratedTypeName(ParseSchemaReference(argument), schemaRegistry));
        return $"{typeName}<{string.Join(",", arguments)}>";
    }

    private static string RemoveArraySuffix(string typeName)
    {
        return typeName.EndsWith("[]", StringComparison.Ordinal)
            ? typeName[..^2]
            : typeName;
    }

    private static string StripNamespaceFromTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return typeName;

        var depth = 0;
        var lastDotOutsideAngles = -1;

        for (var index = 0; index < typeName.Length; index++)
        {
            var character = typeName[index];
            if (character == '<')
                depth++;
            else if (character == '>')
                depth--;
            else if (character == '.' && depth == 0)
                lastDotOutsideAngles = index;
        }

        return lastDotOutsideAngles >= 0
            ? typeName[(lastDotOutsideAngles + 1)..]
            : typeName;
    }

    private static string[] SplitGenericTypeArguments(string argumentText)
    {
        if (string.IsNullOrWhiteSpace(argumentText))
            return [];

        var arguments = new List<string>();
        var depth = 0;
        var start = 0;

        for (var index = 0; index < argumentText.Length; index++)
        {
            var character = argumentText[index];
            if (character == '<')
                depth++;
            else if (character == '>')
                depth--;
            else if (character == ',' && depth == 0)
            {
                arguments.Add(argumentText[start..index].Trim());
                start = index + 1;
            }
        }

        arguments.Add(argumentText[start..].Trim());
        return arguments.ToArray();
    }
}
