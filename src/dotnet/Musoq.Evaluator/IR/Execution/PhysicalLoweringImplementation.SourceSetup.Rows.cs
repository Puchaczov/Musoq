using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private static ExecutionExpression CreateSourceRowsExpression(
        PhysicalNode source,
        RowShape sourceShape,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        string? sourceRowsScope,
        LoweringScope scope
    )
    {
        if (source is PhysicalCteRefNode reference)
        {
            foreach (var binding in scope.SubqueryCtes)
                if (string.Equals(reference.CteName, binding.Name, StringComparison.OrdinalIgnoreCase))
                    return new ExecutionVariableRead(
                        new ExecutionVariable(
                            binding.ParameterName,
                            typeof(object),
                            $"IReadOnlyList<{binding.Shape.TypeName}>"
                        )
                    );
        }
        return source switch
        {
            PhysicalSchemaScanNode scan => CreateSchemaScanRowsExpression(scan.Alias, sourceRowsScope),
            PhysicalInterpretSourceNode interpret => new ExecutionScalarRowStream(
                new ExecutionVariable(CreateSourceRowsName(interpret.Alias, sourceRowsScope), typeof(object))
            ),
            PhysicalPropertySourceNode property => CreateEnumerableRowsExpression(
                property.Alias,
                property.ResultType,
                sourceShape,
                sourceRowsScope
            ),
            PhysicalAccessMethodSourceNode accessMethod => CreateEnumerableRowsExpression(
                accessMethod.Alias,
                accessMethod.ResultType,
                sourceShape,
                sourceRowsScope
            ),
            PhysicalValuesScanNode values when sourceShape is ValuesRowShape valuesShape => new ExecutionVariableRead(
                CreateValuesRowsVariable(values.Alias, valuesShape.GeneratedShape, sourceRowsScope)
            ),
            PhysicalCteRefNode cteRef
                when scope?.RecursiveCteInvariantInputs.TryGetValue(cteRef.CteName, out var invariant) == true =>
                invariant.Rows,
            PhysicalCteRefNode cteRef
                when scope?.RecursiveCteSink is { } sink && cteIndexes[cteRef.CteName] == sink.TableIndex =>
                new ExecutionVariableRead(sink.InputFrontier),
            PhysicalCteRefNode cteRef => new ExecutionStoredTableRows(
                cteIndexes[cteRef.CteName],
                ResolveCteGeneratedRowShape(cteRef, cteShapesByName)
            ),
            _ => throw UnsupportedShape.Of($"Source node '{source.GetType().Name}'", "Execution IR lowering"),
        };
    }

    private static ExecutionExpression CreateEnumerableRowsExpression(
        string alias,
        Type enumerableType,
        RowShape sourceShape,
        string? sourceRowsScope
    )
    {
        var rows = new ExecutionVariable(CreateSourceRowsName(alias, sourceRowsScope), typeof(object));

        var mode = CreateEnumerableChunkMode(sourceShape);
        var directScalar = mode == ExecutionEnumerableChunkMode.DirectScalar;
        return new ExecutionRowStream(rows, directScalar ? ExecutionRowStreamKind.Rows : ExecutionRowStreamKind.Chunks)
        {
            HasDirectScalarElements = directScalar,
            HasRecordBatches = mode == ExecutionEnumerableChunkMode.DirectRecord,
            IsValueEnumerable = directScalar && enumerableType.IsValueType,
        };
    }

    private static ExecutionExpression CreateSchemaScanRowsExpression(string alias, string? sourceRowsScope)
    {
        var rows = new ExecutionVariable(CreateSourceRowsName(alias, sourceRowsScope), typeof(object));

        return new ExecutionRowStream(rows, ExecutionRowStreamKind.Chunks);
    }

    private static string? CreateSourceRowsScope(string resultTableName)
    {
        return string.Equals(resultTableName, "result", StringComparison.Ordinal) ? null : resultTableName;
    }

    private static string CreateSourceRowsName(string alias, string? sourceRowsScope)
    {
        var rowsName = alias.ToRowsSource();
        return string.IsNullOrWhiteSpace(sourceRowsScope)
            ? rowsName
            : CreateIdentifierCandidate($"{sourceRowsScope}_{rowsName}", 0);
    }

    private static ExecutionEnumerableChunkMode CreateEnumerableChunkMode(RowShape sourceShape)
    {
        if (IsDirectScalarSource(sourceShape))
            return ExecutionEnumerableChunkMode.DirectScalar;

        if (sourceShape is SourceEntityShape { GeneratedTypeName: not null })
            return ExecutionEnumerableChunkMode.DirectRecord;

        if (sourceShape is not SourceEntityShape source)
            return ExecutionEnumerableChunkMode.ObjectOrReflected;
        var type = source.EntityType.ResolveClrType();
        if (
            type == typeof(object)
            || !RowShapeLookup.CanReferenceType(type)
            || RowShapeLookup.UsesReflectedMemberAccess(source)
        )
            return ExecutionEnumerableChunkMode.ObjectOrReflected;
        return source.IsRecordEnumerable
            ? ExecutionEnumerableChunkMode.DirectRecord
            : ExecutionEnumerableChunkMode.Direct;
    }

    private static bool IsDirectScalarSource(RowShape sourceShape)
    {
        return sourceShape is SourceEntityShape source
            && source.Fields.Any(static field => field.AccessStrategy is DirectScalarValueAccess);
    }
}
