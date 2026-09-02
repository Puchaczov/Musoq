using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.Visitors;

public partial class InterpreterCodeGenerator
{
    private List<SchemaFieldNode> GetAllFieldsIncludingInherited(BinarySchemaNode schema)
    {
        return GetAllFieldsIncludingInherited(
            schema,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    }

    private List<SchemaFieldNode> GetAllFieldsIncludingInherited(
        BinarySchemaNode schema,
        ISet<string> inheritancePath)
    {
        if (!inheritancePath.Add(schema.Name))
            throw CreateInvalidInheritanceException(
                $"Binary schema inheritance contains a cycle involving '{schema.Name}'.");

        try
        {
            if (string.IsNullOrEmpty(schema.Extends))
                return schema.Fields.ToList();

            if (!_registry.TryGetSchema(schema.Extends, out var parentRegistration) ||
                parentRegistration?.Node is not BinarySchemaNode parentSchema)
                throw CreateInvalidInheritanceException(
                    $"Binary schema '{schema.Name}' extends undefined or non-binary schema '{schema.Extends}'.");

            if (parentSchema.IsGeneric)
                throw CreateInvalidInheritanceException(
                    $"Binary schema '{schema.Name}' cannot extend generic schema '{parentSchema.Name}' without type arguments.");

            var allFields = GetAllFieldsIncludingInherited(parentSchema, inheritancePath);

            var overriddenParentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var childField in schema.Fields)
                for (var i = 0; i < allFields.Count; i++)
                    if (string.Equals(allFields[i].Name, childField.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        allFields[i] = childField;
                        overriddenParentNames.Add(childField.Name);
                        break;
                    }

            foreach (var childField in schema.Fields)
                if (!overriddenParentNames.Contains(childField.Name))
                    allFields.Add(childField);

            return allFields;
        }
        finally
        {
            inheritancePath.Remove(schema.Name);
        }
    }

    private static ConstructionNotYetSupported CreateInvalidInheritanceException(string message)
    {
        return new ConstructionNotYetSupported(
            message,
            DiagnosticCode.MQ4016_UnsupportedSchemaConstruction);
    }
}
