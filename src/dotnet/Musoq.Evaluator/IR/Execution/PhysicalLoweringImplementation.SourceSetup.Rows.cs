using System.Collections.Generic;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.IR.Logical.Nodes;
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
        LoweringScope scope)
    {
        return source switch
        {
            PhysicalSchemaScanNode scan => CreateSchemaScanRowsExpression(scan.Alias, sourceRowsScope),
            PhysicalInterpretSourceNode interpret when IsScalarInterpretSourceKind(interpret.Kind) =>
                new ExecutionScalarRowStream(new ExecutionVariable(CreateSourceRowsName(interpret.Alias, sourceRowsScope), typeof(object))),
            PhysicalInterpretSourceNode interpret => new ExecutionRowStream(
                new ExecutionVariable(CreateSourceRowsName(interpret.Alias, sourceRowsScope), typeof(object)),
                ExecutionRowStreamKind.Chunks),
            PhysicalPropertySourceNode property => CreateEnumerableRowsExpression(property.Alias, sourceShape, sourceRowsScope),
            PhysicalAccessMethodSourceNode accessMethod => CreateEnumerableRowsExpression(accessMethod.Alias, sourceShape, sourceRowsScope),
            PhysicalValuesScanNode values when sourceShape is ValuesRowShape valuesShape =>
                new ExecutionVariableRead(CreateValuesRowsVariable(values.Alias, valuesShape.GeneratedShape, sourceRowsScope)),
            PhysicalCteRefNode cteRef when scope?.RecursiveCteInvariantInputs.TryGetValue(
                                                cteRef.CteName,
                                                out var invariant) == true =>
                invariant.Rows,
            PhysicalCteRefNode cteRef when scope?.RecursiveCteSink is { } sink &&
                                                cteIndexes[cteRef.CteName] == sink.TableIndex =>
                new ExecutionVariableRead(sink.InputFrontier),
            PhysicalCteRefNode cteRef => new ExecutionStoredTableRows(
                cteIndexes[cteRef.CteName],
                ResolveCteGeneratedRowShape(cteRef, cteShapesByName)),
            _ => throw UnsupportedShape.Of($"Source node '{source.GetType().Name}'", "Execution IR lowering")
        };
    }

    private static ExecutionExpression CreateEnumerableRowsExpression(
        string alias,
        RowShape sourceShape,
        string? sourceRowsScope)
    {
        var rows = new ExecutionVariable(CreateSourceRowsName(alias, sourceRowsScope), typeof(object));

        return new ExecutionRowStream(rows, ExecutionRowStreamKind.Chunks);
    }

    private static ExecutionExpression CreateSchemaScanRowsExpression(
        string alias,
        string? sourceRowsScope)
    {
        var rows = new ExecutionVariable(CreateSourceRowsName(alias, sourceRowsScope), typeof(object));

        return new ExecutionRowStream(rows, ExecutionRowStreamKind.Chunks);
    }

    private static string? CreateSourceRowsScope(string resultTableName)
    {
        return string.Equals(resultTableName, "result", StringComparison.Ordinal)
            ? null
            : resultTableName;
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

        return sourceShape is SourceEntityShape { EntityType: not null } source &&
               source.EntityType.ResolveClrType() != typeof(object) &&
               RowShapeLookup.CanReferenceType(source.EntityType.ResolveClrType()) &&
               !RowShapeLookup.UsesReflectedMemberAccess(source)
            ? ExecutionEnumerableChunkMode.Direct
            : ExecutionEnumerableChunkMode.ObjectOrReflected;
    }

    private static bool IsDirectScalarSource(RowShape sourceShape)
    {
        return sourceShape is SourceEntityShape { Fields: [{ AccessStrategy: DirectScalarValueAccess }] };
    }

    private static bool IsScalarInterpretSourceKind(InterpretSourceKind kind)
    {
        return kind is not (InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse);
    }
}
