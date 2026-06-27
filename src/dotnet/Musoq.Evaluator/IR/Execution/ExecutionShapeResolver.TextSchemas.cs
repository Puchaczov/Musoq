using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static List<ColumnSchema> CreateTextColumns(TextSchemaNode text, SchemaRegistry? schemaRegistry)
    {
        var columns = new List<ColumnSchema>();
        var fieldsByName = new Dictionary<string, TextFieldDefinitionNode>(StringComparer.OrdinalIgnoreCase);

        AddInheritedTextFields(text, fieldsByName, schemaRegistry);

        foreach (var field in text.Fields)
            fieldsByName[field.Name] = field;

        foreach (var field in fieldsByName.Values)
        {
            if (field.Name.StartsWith('_'))
                continue;

            var columnType = field.FieldType switch
            {
                TextFieldType.Pattern when field.CaptureGroups.Length > 0 => typeof(object),
                TextFieldType.Repeat => typeof(object[]),
                TextFieldType.Switch => DynamicEntityBoundary.ExpandoType,
                _ => typeof(string)
            };

            columns.Add(new ColumnSchema(field.Name, columnType, columns.Count));
        }

        return columns;
    }

    private static void AddInheritedTextFields(
        TextSchemaNode text,
        IDictionary<string, TextFieldDefinitionNode> fieldsByName,
        SchemaRegistry? schemaRegistry)
    {
        if (string.IsNullOrWhiteSpace(text.Extends) ||
            schemaRegistry == null ||
            !schemaRegistry.TryGetSchema(text.Extends, out var registration) ||
            registration?.Node is not TextSchemaNode parent)
        {
            return;
        }

        AddInheritedTextFields(parent, fieldsByName, schemaRegistry);

        foreach (var field in parent.Fields)
            fieldsByName[field.Name] = field;
    }
}
