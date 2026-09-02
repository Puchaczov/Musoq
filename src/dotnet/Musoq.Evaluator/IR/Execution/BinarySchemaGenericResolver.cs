using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

internal static class BinarySchemaGenericResolver
{
    public static IReadOnlyDictionary<string, SchemaReferenceTypeNode> CreateEmptyBindings()
    {
        return new Dictionary<string, SchemaReferenceTypeNode>(StringComparer.OrdinalIgnoreCase);
    }

    public static SchemaReferenceTypeNode ResolveReference(
        SchemaReferenceTypeNode reference,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> bindings)
    {
        if (!reference.IsGenericInstantiation && bindings.TryGetValue(reference.SchemaName, out var boundReference))
            return boundReference;

        if (reference.IsGenericInstantiation)
        {
            var typeArguments = reference.TypeArguments
                .Select(typeArgument => CreateTypeArgumentReference(typeArgument, bindings).FullTypeName)
                .ToArray();

            return new SchemaReferenceTypeNode(reference.SchemaName, typeArguments);
        }

        return reference;
    }

    public static IReadOnlyDictionary<string, SchemaReferenceTypeNode> CreateBindings(
        BinarySchemaNode schema,
        SchemaReferenceTypeNode reference,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> outerBindings)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(outerBindings);

        if (schema.TypeParameters.Length != reference.TypeArguments.Length)
            throw new ArgumentException(
                $"Schema '{schema.Name}' declares {schema.TypeParameters.Length} type parameters, " +
                $"but reference '{reference.FullTypeName}' supplies {reference.TypeArguments.Length}.",
                nameof(reference));

        var bindings = new Dictionary<string, SchemaReferenceTypeNode>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < schema.TypeParameters.Length; index++)
        {
            var typeArgument = reference.TypeArguments[index];
            bindings[schema.TypeParameters[index]] = CreateTypeArgumentReference(typeArgument, outerBindings);
        }

        return bindings;
    }

    public static string CreateSchemaKey(
        BinarySchemaNode schema,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> bindings)
    {
        if (!schema.IsGeneric)
            return schema.Name;

        var typeArguments = schema.TypeParameters.Select(parameter =>
            bindings.TryGetValue(parameter, out var reference)
                ? reference.FullTypeName
                : parameter);

        return $"{schema.Name}<{string.Join(",", typeArguments)}>";
    }

    private static SchemaReferenceTypeNode CreateTypeArgumentReference(
        string typeArgument,
        IReadOnlyDictionary<string, SchemaReferenceTypeNode> outerBindings)
    {
        var trimmedTypeArgument = typeArgument.Trim();
        if (outerBindings.TryGetValue(trimmedTypeArgument, out var boundReference))
            return boundReference;

        return ResolveReference(ParseReference(trimmedTypeArgument), outerBindings);
    }

    private static SchemaReferenceTypeNode ParseReference(string typeName)
    {
        var openIndex = typeName.IndexOf('<', StringComparison.Ordinal);
        if (openIndex < 0)
            return new SchemaReferenceTypeNode(typeName);

        var closeIndex = typeName.LastIndexOf('>');
        if (closeIndex <= openIndex)
            return new SchemaReferenceTypeNode(typeName);

        var schemaName = typeName[..openIndex].Trim();
        var argumentText = typeName[(openIndex + 1)..closeIndex];
        var typeArguments = SplitTypeArguments(argumentText);

        return new SchemaReferenceTypeNode(schemaName, typeArguments);
    }

    private static string[] SplitTypeArguments(string argumentText)
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
