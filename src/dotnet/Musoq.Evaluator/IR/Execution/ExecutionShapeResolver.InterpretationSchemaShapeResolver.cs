using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Parser.Nodes;
using Musoq.Parser.Nodes.InterpretationSchema;
using Musoq.Schema.Interpreters;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionShapeResolver
{
    private Type ResolveInterpretEntityType(PhysicalInterpretSourceNode interpret)
    {
        if (_schemaRegistry != null &&
            _schemaRegistry.TryGetSchema(interpret.SchemaName, out var registration) &&
            registration?.GeneratedType != null)
        {
            if (interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse)
                return typeof(PartialInterpretResult<>).MakeGenericType(registration.GeneratedType);

            return registration.GeneratedType;
        }

        if (interpret.ResultType != typeof(object) && !IsRowSourceType(interpret.ResultType))
            return interpret.ResultType;

        return typeof(object);
    }

    private bool HasGeneratedInterpreterTypeName(string schemaName)
    {
        return _schemaRegistry != null &&
               _schemaRegistry.TryGetSchema(schemaName, out var registration) &&
               !string.IsNullOrWhiteSpace(registration?.GeneratedTypeName);
    }

    private IReadOnlyList<ColumnSchema> ResolveInterpretColumns(PhysicalInterpretSourceNode interpret)
    {
        if (interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse)
            return CreatePartialInterpretColumns();

        if (_schemaRegistry != null &&
            _schemaRegistry.TryGetSchema(interpret.SchemaName, out var registration) &&
            registration?.Node != null)
        {
            var schemaColumns = CreateColumnsFromSchemaNode(registration.Node, _schemaRegistry, registration.GeneratedType);
            if (schemaColumns.Count > 0)
                return schemaColumns;
        }

        return interpret.OutputSchema.Columns;
    }

    private static IReadOnlyList<ColumnSchema> CreatePartialInterpretColumns()
    {
        return
        [
            new ColumnSchema("ParsedFields", typeof(Dictionary<string, object?>), 0),
            new ColumnSchema("ErrorField", typeof(string), 1),
            new ColumnSchema("ErrorMessage", typeof(string), 2),
            new ColumnSchema("BytesConsumed", typeof(int), 3)
        ];
    }

    private static IReadOnlyList<ColumnSchema> CreateColumnsFromSchemaNode(
        Node schemaNode,
        SchemaRegistry? schemaRegistry,
        Type? generatedType)
    {
        return schemaNode switch
        {
            TextSchemaNode text => CreateTextColumns(text, schemaRegistry),
            BinarySchemaNode binary => CreateBinaryColumns(binary, schemaRegistry, generatedType),
            _ => []
        };
    }
}
