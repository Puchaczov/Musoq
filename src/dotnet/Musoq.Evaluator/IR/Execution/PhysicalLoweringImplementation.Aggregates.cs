using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private SourceBuildResult BuildAggregateSource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope,
        string aggregateKind,
        LoweringScope scope)
    {
        if (source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildNestedJoinSource(source, cteIndexes, cteShapesByName, schemaFromIndex, scope);
        if (source is PhysicalNestedLoopApplyNode apply)
        {
            return BuildNestedApplySource(
                apply,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                scope);
        }
        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
        {
            return SourceBuildResult.Unsupported(
                $"Execution IR {aggregateKind} lowering cannot resolve source shape for {source.GetType().Name}.");
        }

        var variable = CreateSourceVariable(source, shape, cteShapesByName);
        var setup = CreateSourceSetup(source, shape, variable, schemaFromIndex, cteIndexes, sourceRowsScope);
        var rows = CreateSourceRowsExpression(source, shape, cteIndexes, cteShapesByName, sourceRowsScope, scope);
        var schemaSourceCount = source is PhysicalSchemaScanNode ? 1 : 0;

        return SourceBuildResult.Success(new JoinSource(source, shape, variable, setup, rows, [shape], schemaSourceCount));
    }
}
