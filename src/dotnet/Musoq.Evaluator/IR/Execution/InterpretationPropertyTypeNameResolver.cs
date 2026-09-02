using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal static class InterpretationPropertyTypeNameResolver
{
    public static string? ResolvePropertyTypeName(
        string schemaName,
        string propertyPath,
        SchemaRegistry? schemaRegistry)
    {
        if (schemaRegistry == null || string.IsNullOrWhiteSpace(schemaName))
            return null;

        var currentTypeName = CreateGeneratedTypeName(
            new SchemaReferenceTypeNode(schemaName),
            schemaRegistry);
        var properties = CreatePropertyPathSegments(propertyPath);
        if (properties.Length == 0)
            return currentTypeName;

        foreach (var property in properties)
        {
            currentTypeName = ResolveFieldTypeName(currentTypeName, property, schemaRegistry);
            if (string.IsNullOrWhiteSpace(currentTypeName))
                return null;
        }

        return currentTypeName;
    }

    public static string? ResolvePropertyTypeNameFromGeneratedType(
        string generatedTypeName,
        string propertyPath,
        SchemaRegistry? schemaRegistry)
    {
        if (string.IsNullOrWhiteSpace(generatedTypeName))
            return null;

        var properties = CreatePropertyPathSegments(propertyPath);
        if (properties.Length == 0)
            return null;

        var currentTypeName = generatedTypeName;
        foreach (var property in properties)
        {
            currentTypeName = ResolveFieldTypeName(currentTypeName, property, schemaRegistry);
            if (string.IsNullOrWhiteSpace(currentTypeName))
                return null;
        }

        return currentTypeName;
    }

    private static string[] CreatePropertyPathSegments(string propertyPath)
    {
        return propertyPath
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static property =>
            {
                var indexStart = property.IndexOf('[', StringComparison.Ordinal);
                return indexStart >= 0 ? property[..indexStart] : property;
            })
            .Where(static property => !string.IsNullOrWhiteSpace(property))
            .ToArray();
    }

    public static string? ResolveRootTypeName(PhysicalPropertySourceNode property)
    {
        foreach (var propertyPart in property.PropertiesChain)
        {
            if (!string.IsNullOrWhiteSpace(propertyPart.IntendedTypeName))
                return RemoveArraySuffix(propertyPart.IntendedTypeName);

            break;
        }

        return null;
    }

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

    internal static string? ResolveFieldTypeName(
        string typeName,
        string fieldName,
        SchemaRegistry? schemaRegistry)
    {
        if (schemaRegistry == null)
            return null;

        var reference = ParseSchemaReference(typeName);
        if (!schemaRegistry.TryGetSchema(reference.SchemaName, out var registration) ||
            registration?.Node is null)
        {
            return null;
        }

        if (registration.Node is TextSchemaNode text)
        {
            var textField = FindTextField(text, fieldName, schemaRegistry);
            return textField == null
                ? null
                : ResolveTextFieldTypeName(text, textField, schemaRegistry);
        }

        if (registration.Node is not BinarySchemaNode binary)
            return null;

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

    private static TextFieldDefinitionNode? FindTextField(
        TextSchemaNode text,
        string fieldName,
        SchemaRegistry schemaRegistry)
    {
        if (!string.IsNullOrWhiteSpace(text.Extends) &&
            schemaRegistry.TryGetSchema(text.Extends, out var parentRegistration) &&
            parentRegistration?.Node is TextSchemaNode parent)
        {
            var inherited = FindTextField(parent, fieldName, schemaRegistry);
            if (inherited != null)
                return inherited;
        }

        return text.Fields.FirstOrDefault(field =>
            string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ResolveTextFieldTypeName(
        TextSchemaNode text,
        TextFieldDefinitionNode field,
        SchemaRegistry schemaRegistry)
    {
        return field.FieldType switch
        {
            TextFieldType.SchemaReference => CreateGeneratedTypeName(
                new SchemaReferenceTypeNode(field.PrimaryValue ?? "object"),
                schemaRegistry),
            TextFieldType.Repeat =>
                $"{CreateGeneratedTypeName(new SchemaReferenceTypeNode(field.PrimaryValue ?? "object"), schemaRegistry)}[]",
            TextFieldType.Pattern when field.CaptureGroups.Length > 0 =>
                $"Musoq.Generated.Interpreters.{text.Name}.CaptureResult_{field.Name}",
            _ => null
        };
    }

    internal static string? ResolveFieldTypeName(
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
            StringTypeNode { AsTextSchemaName: { } textSchemaName } =>
                CreateGeneratedTypeName(new SchemaReferenceTypeNode(textSchemaName), schemaRegistry),
            BinarySwitchTypeNode => $"Musoq.Generated.Interpreters.Switch_{field.Name}",
            SubstreamTypeNode { Target: InlineSchemaTypeNode } => CreateInlineTypeName(field.Name),
            SubstreamTypeNode { Target: SchemaReferenceTypeNode reference } =>
                CreateGeneratedTypeName(reference, schemaRegistry),
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
