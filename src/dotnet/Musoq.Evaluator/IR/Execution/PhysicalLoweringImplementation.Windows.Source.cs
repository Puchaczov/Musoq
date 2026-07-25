using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private SourceBuildResult BuildWindowSource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        if (IsAggregateSource(source))
        {
            var table = BuildPlanTable(
                source,
                "windowSourceTable",
                "WindowSourceRow0",
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                scopeAggregateVariables: false,
                scope: scope);
            if (!table.IsBuilt)
                return SourceBuildResult.Unsupported(table.UnsupportedReason);

            const string sourceAlias = "windowSource";
            var sourceShape = CreateTypedMaterializedTransitionTableRowShape(sourceAlias, table.RowShape);
            var sourceVariable = new ExecutionVariable(sourceAlias, typeof(Row), table.RowShape.TypeName);
            var materializedRows = new ExecutionRowStream(
                table.Table,
                ExecutionRowStreamKind.Rows,
                ExecutionRowStreamRowsAccess.TableRows);
            var shapes = table.Shapes.Concat([sourceShape]).ToArray();

            return SourceBuildResult.Success(new JoinSource(
                source,
                sourceShape,
                sourceVariable,
                table.Nodes.ToList(),
                materializedRows,
                shapes,
                CountSchemaScans(source),
                GeneratedRowShape: table.RowShape));
        }

        if (source is PhysicalNestedLoopApplyNode apply)
        {
            return BuildNestedApplySource(
                apply,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                scope,
                NestedApplyGeneratedRowPreservation.Enabled);
        }

        if (source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildNestedJoinSource(source, cteIndexes, cteShapesByName, schemaFromIndex, scope);

        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
            return SourceBuildResult.Unsupported($"Execution IR window lowering cannot resolve source shape for {source.GetType().Name}.");

        var variable = CreateSourceVariable(source, shape, cteShapesByName);
        var setup = CreateSourceSetup(source, shape, variable, schemaFromIndex, cteIndexes, sourceRowsScope);
        var rows = CreateSourceRowsExpression(source, shape, cteIndexes, cteShapesByName, sourceRowsScope, scope);
        var schemaSourceCount = source is PhysicalSchemaScanNode ? 1 : 0;

        return SourceBuildResult.Success(new JoinSource(source, shape, variable, setup, rows, [shape], schemaSourceCount, GeneratedRowShape: source is PhysicalCteRefNode cteRef ? ResolveCteGeneratedRowShape(cteRef, cteShapesByName) : null));
    }

    private static ExecutionRowAccessMode ResolveWindowRowAccessMode(
        RowShape sourceShape)
    {
        if (sourceShape is ExpandoAdapterShape ||
            RowShapeLookup.ResolveSourceRuntimeType(sourceShape) == typeof(object))
        {
            return ExecutionRowAccessMode.ExpandoAdapter;
        }

        return ExecutionRowAccessMode.Direct;
    }
}
