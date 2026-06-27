using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private List<ExecutionNode> CreateInterpretSourceSetup(
        PhysicalInterpretSourceNode interpret,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        IReadOnlyDictionary<string, int> cteIndexes,
        string? sourceRowsScope)
    {
        var sourceRows = new ExecutionVariable(CreateSourceRowsName(interpret.Alias, sourceRowsScope), typeof(object));

        return
        [
            new ExecutionInterpretSource(
                sourceRows,
                interpret.SchemaName,
                ResolveInterpreterTypeName(interpret),
                interpret.Kind,
                interpret.Arguments.Select(argument => ExecutionExpressionConverter.Convert(argument, sourceLookup, cteIndexes)).ToArray(),
                interpret.ApplyKind)
        ];
    }

    private static List<ExecutionNode> CreateEnumerableSourceSetup(
        string alias,
        Type enumerableType,
        ExecutionExpression source,
        ExecutionEnumerableChunkMode chunkMode,
        string? sourceRowsScope,
        string? enumerableTypeName = null)
    {
        var sourceRows = new ExecutionVariable(CreateSourceRowsName(alias, sourceRowsScope), typeof(object));

        return [new ExecutionEnumerableSource(sourceRows, source, enumerableType, chunkMode, enumerableTypeName)];
    }

    private static List<ExecutionNode> CreateValuesSourceSetup(
        PhysicalValuesScanNode values,
        RowShape sourceShape,
        string? sourceRowsScope)
    {
        if (sourceShape is not ValuesRowShape valuesShape)
        {
            throw new InvalidOperationException(
                $"Execution IR values-source lowering expected a values row shape for alias '{values.Alias}'.");
        }

        return
        [
            new ExecutionCreateValuesRows(
                CreateValuesRowsVariable(values.Alias, valuesShape.GeneratedShape, sourceRowsScope),
                valuesShape.GeneratedShape,
                CreateValuesRowValues(values, valuesShape.GeneratedShape))
        ];
    }

    private static ExecutionVariable CreateValuesRowsVariable(
        string alias,
        GeneratedRowShape rowShape,
        string? sourceRowsScope)
    {
        return new ExecutionVariable(
            CreateSourceRowsName(alias, sourceRowsScope),
            typeof(object),
            $"{rowShape.TypeName}[]");
    }

    private static List<IReadOnlyList<ExecutionRowValue>> CreateValuesRowValues(
        PhysicalValuesScanNode values,
        GeneratedRowShape rowShape)
    {
        var rows = new List<IReadOnlyList<ExecutionRowValue>>(values.Rows.Count);

        foreach (var row in values.Rows)
        {
            var fields = row.Fields.ToDictionary(
                static field => field.Name,
                StringComparer.OrdinalIgnoreCase);
            var rowValues = rowShape.Fields
                .Select(field => new ExecutionRowValue(
                    field.Name,
                    ExecutionExpressionConverter.Convert(fields[field.Name].Value, RowShapeLookup.EmptySourceShapeLookup())))
                .ToArray();

            rows.Add(rowValues);
        }

        return rows;
    }

    private string? ResolvePropertyEnumerableTypeName(PhysicalPropertySourceNode property)
    {
        return InterpretationPropertyTypeNameResolver.ResolveEnumerableTypeName(property, _schemaRegistry);
    }
}
