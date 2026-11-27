using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator;

internal static class InterpretationSchemaFields
{
    internal static List<SchemaFieldNode> GetFields(BinarySchemaNode schema, SchemaRegistry registry)
    {
        if (string.IsNullOrEmpty(schema.Extends))
            return schema.Fields.ToList();

        return GetFields(schema, registry, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private static List<SchemaFieldNode> GetFields(
        BinarySchemaNode schema,
        SchemaRegistry registry,
        ISet<string> inheritancePath
    )
    {
        if (!inheritancePath.Add(schema.Name))
            throw CreateInvalidInheritanceException(
                $"Binary schema inheritance contains a cycle involving '{schema.Name}'."
            );

        if (string.IsNullOrEmpty(schema.Extends))
            return schema.Fields.ToList();

        if (!registry.TryGetSchema(schema.Extends, out var parent) || parent?.Node is not BinarySchemaNode parentSchema)
            throw CreateInvalidInheritanceException(
                $"Binary schema '{schema.Name}' extends undefined or non-binary schema '{schema.Extends}'."
            );

        if (parentSchema.IsGeneric)
            throw CreateInvalidInheritanceException(
                $"Binary schema '{schema.Name}' cannot extend generic schema '{parentSchema.Name}' without type arguments."
            );

        return Merge(GetFields(parentSchema, registry, inheritancePath), schema.Fields, static field => field.Name);
    }

    internal static List<TextFieldDefinitionNode> GetFields(TextSchemaNode schema, SchemaRegistry? registry)
    {
        if (
            string.IsNullOrEmpty(schema.Extends)
            || registry == null
            || !registry.TryGetSchema(schema.Extends, out var parent)
            || parent?.Node is not TextSchemaNode parentSchema
        )
            return schema.Fields.ToList();

        return Merge(GetFields(parentSchema, registry), schema.Fields, static field => field.Name);
    }

    private static List<T> Merge<T>(List<T> inherited, T[] declarations, Func<T, string> nameOf)
    {
        var positions = new Dictionary<string, int>(inherited.Count, StringComparer.Ordinal);
        for (var index = 0; index < inherited.Count; index++)
        {
            var name = nameOf(inherited[index]);
            if (name != "_")
                positions.Add(name, index);
        }
        foreach (var field in declarations)
        {
            if (positions.TryGetValue(nameOf(field), out var index))
                inherited[index] = field;
            else
                inherited.Add(field);
        }
        return inherited;
    }

    private static ConstructionNotYetSupported CreateInvalidInheritanceException(string message)
    {
        return new ConstructionNotYetSupported(message, DiagnosticCode.MQ4016_UnsupportedSchemaConstruction);
    }
}
