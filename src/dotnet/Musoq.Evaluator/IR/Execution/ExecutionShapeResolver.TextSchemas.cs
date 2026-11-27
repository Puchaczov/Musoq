using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Parser.Nodes.InterpretationSchema;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private static List<ColumnSchema> CreateTextColumns(TextSchemaNode text, SchemaRegistry? schemaRegistry)
    {
        var columns = new List<ColumnSchema>();
        var fields = InterpretationSchemaFields.GetFields(text, schemaRegistry);
        var names = schemaRegistry == null ? new TextResultTypeNames(text.Name, fields) : null;
        foreach (var field in fields)
        {
            if (field.IsDiscard)
                continue;

            var shape =
                schemaRegistry == null ? null : new TextResultShapeBinding(schemaRegistry).Field(text.Name, field);
            var (columnType, intendedTypeName) =
                shape == null
                    ? InterpretationTextFieldTypeResolver.Resolve(text.Name, field, names: names)
                    : (shape.ClrType, shape.TypeName);

            columns.Add(new ColumnSchema(field.Name, columnType, columns.Count, intendedTypeName));
        }

        return columns;
    }
}
