using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private JoinSourcesBuildResult BuildJoinSources(
        PhysicalNode left,
        PhysicalNode right,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        var leftSource = BuildJoinSource(left, cteIndexes, cteShapesByName, schemaFromIndex, sourceRowsScope, scope);
        if (!leftSource.IsBuilt)
            return JoinSourcesBuildResult.Unsupported(leftSource.UnsupportedReason);

        var rightSource = BuildJoinSource(
            right,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex + leftSource.Source.SchemaSourceCount,
            sourceRowsScope,
            scope);
        if (!rightSource.IsBuilt)
            return JoinSourcesBuildResult.Unsupported(rightSource.UnsupportedReason);

        return JoinSourcesBuildResult.Success(leftSource.Source, rightSource.Source);
    }

    private SourceBuildResult BuildJoinSource(
        PhysicalNode source,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        if (source is PhysicalCteRefNode cteRef &&
            TryBuildFusedCteHashBuildJoinSource(cteRef, cteIndexes, cteShapesByName, scope, out var fusedSource))
        {
            return SourceBuildResult.Success(fusedSource);
        }

        if (source is PhysicalFilterNode filter)
            return BuildFilteredJoinSource(filter, cteIndexes, cteShapesByName, schemaFromIndex, sourceRowsScope, scope);

        if (source is PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode)
            return BuildNestedJoinSource(source, cteIndexes, cteShapesByName, schemaFromIndex, scope);

        if (source is not (PhysicalSchemaScanNode or PhysicalCteRefNode or PhysicalValuesScanNode))
        {
            return SourceBuildResult.Unsupported(
                $"Execution IR join lowering currently supports flat schema-scan, CTE-ref, values, or nested join inputs. Found {source.GetType().Name}.");
        }

        var shape = ResolveSourceShape(source, cteIndexes, cteShapesByName);
        if (shape == null)
            return SourceBuildResult.Unsupported($"Execution IR join lowering cannot resolve source shape for {source.GetType().Name}.");

        var variable = CreateSourceVariable(source, shape, cteShapesByName);
        var setup = CreateSourceSetup(source, shape, variable, schemaFromIndex, cteIndexes, sourceRowsScope);
        var rows = CreateSourceRowsExpression(source, shape, cteIndexes, cteShapesByName, sourceRowsScope, scope);

        return SourceBuildResult.Success(new JoinSource(
            source, shape, variable, setup, rows, [shape], source is PhysicalSchemaScanNode ? 1 : 0));
    }

    private SourceBuildResult BuildFilteredJoinSource(
        PhysicalFilterNode filter,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        string? sourceRowsScope,
        LoweringScope scope)
    {
        var filterSource = CollectFilterPredicate(filter);
        var baseSource = BuildJoinSource(filterSource.Source, cteIndexes, cteShapesByName, schemaFromIndex, sourceRowsScope, scope);
        if (!baseSource.IsBuilt)
            return baseSource;

        if (baseSource.Source.Shape is ExpandoAdapterShape)
        {
            return SourceBuildResult.Unsupported(
                "Execution IR join prefilter lowering keeps dynamic source inputs on the existing runtime path.");
        }

        var filteredRows = CreateMaterializedRowsBufferVariable(
            CreateIdentifierCandidate($"{baseSource.Source.Variable.Name}PrefilteredRows", 0),
            baseSource.Source.GeneratedRowShape);
        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(baseSource.Source.Shape);
        var filterPredicate = ExecutionExpressionConverter.Convert(filterSource.Predicate, sourceLookup);
        var setup = new List<ExecutionNode>(baseSource.Source.Setup.Count + 1);

        setup.AddRange(baseSource.Source.Setup);
        setup.Add(CreateMaterializeFilteredListNode(
            baseSource.Source.Rows,
            filteredRows,
            baseSource.Source.Variable,
            ExecutionRowAccessMode.Direct,
            filterPredicate,
            baseSource.Source.GeneratedRowShape));

        return SourceBuildResult.Success(baseSource.Source with
        {
            Node = filter,
            Setup = setup,
            Rows = new ExecutionVariableRead(filteredRows)
        });
    }

    private static FilteredSource CollectFilterPredicate(PhysicalFilterNode filter)
    {
        var predicate = filter.Predicate;
        var source = filter.Input;

        while (source is PhysicalFilterNode nestedFilter)
        {
            predicate = new BinaryOp(BinaryOpKind.And, predicate, nestedFilter.Predicate, typeof(bool));
            source = nestedFilter.Input;
        }

        return new FilteredSource(source, predicate);
    }

    private SourceBuildResult BuildNestedJoinSource(
        PhysicalNode join,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        LoweringScope scope)
    {
        var projection = CreateNestedSourceProjectionFields(join, cteIndexes, cteShapesByName);
        if (!projection.IsBuilt)
            return SourceBuildResult.Unsupported(projection.UnsupportedReason);

        var sourceAlias = CreateNestedJoinSourceAlias(join, schemaFromIndex);
        var tableName = $"{sourceAlias}Table";
        var shapeName = CreateNestedSourceShapeName(sourceAlias);
        var project = new PhysicalProjectNode(projection.Value, join);
        var pipeline = new CteSupportedPipeline(project, join, null, []);
        var table = BuildJoinTable(
            pipeline,
            tableName,
            shapeName,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            scope);

        if (!table.IsBuilt)
            return SourceBuildResult.Unsupported(table.UnsupportedReason);

        var transitionShape = CreateMaterializedTransitionTableRowShape(sourceAlias, table.RowShape);
        var source = new ExecutionVariable(sourceAlias, typeof(Row));
        var rows = new ExecutionRowStream(
            table.Table,
            ExecutionRowStreamKind.Rows,
            ExecutionRowStreamRowsAccess.TableRows);
        var shapes = table.Shapes.Concat([transitionShape]).ToArray();

        return SourceBuildResult.Success(new JoinSource(
            join,
            transitionShape,
            source,
            table.Nodes.ToList(),
            rows,
            shapes,
            CountSchemaScans(join)));
    }

    private static List<ExecutionNode> CreateJoinPrelude(
        JoinSources sources,
        ExecutionVariable resultTable,
        GeneratedRowShape resultShape,
        ExecutionCapacityHint? capacityHint = null)
    {
        var nodes = new List<ExecutionNode>(sources.Left.Setup.Count + sources.Right.Setup.Count + 1);

        nodes.AddRange(sources.Left.Setup);
        nodes.AddRange(sources.Right.Setup);
        nodes.Add(CreateTable(resultTable, resultShape, capacityHint));

        return nodes;
    }

    private static ExecutionExpression CreateNestedLoopInnerRows(
        JoinSource inner,
        List<ExecutionNode> nodes)
    {
        if (!ExecutionRowStreams.IsChunked(inner.Rows))
            return inner.Rows;

        var buffer = CreateMaterializedRowsBufferVariable(
            CreateIdentifierCandidate($"{inner.Variable.Name}RowsBuffer", 0),
            inner.GeneratedRowShape);

        nodes.Add(CreateMaterializeListNode(inner.Rows, buffer, inner.GeneratedRowShape));

        return new ExecutionVariableRead(buffer);
    }

    private ExecutionBlock CreateJoinLoopBody(
        IrExpression? joinCondition,
        PhysicalFilterNode? filter,
        ExecutionAppendRow appendRow,
        IReadOnlyDictionary<string, RowShape> sourceLookup)
    {
        if (joinCondition == null && filter == null)
            return CreateAppendBlock(appendRow);

        var condition = joinCondition == null
            ? ExecutionExpressionConverter.Convert(filter!.Predicate, sourceLookup)
            : ExecutionExpressionConverter.Convert(joinCondition, sourceLookup);

        if (joinCondition != null && filter != null)
        {
            condition = new ExecutionBinary(
                BinaryOpKind.And,
                condition,
                ExecutionExpressionConverter.Convert(filter.Predicate, sourceLookup),
                typeof(bool));
        }

        return CreateFilteredAppendBlock(condition, appendRow);
    }

    private static Type ResolveCommonKeyType(Type buildType, Type probeType)
    {
        if (buildType == probeType)
            return buildType;

        var buildUnderlying = Nullable.GetUnderlyingType(buildType) ?? buildType;
        var probeUnderlying = Nullable.GetUnderlyingType(probeType) ?? probeType;

        if (buildUnderlying == probeUnderlying && buildUnderlying.IsValueType)
            return typeof(Nullable<>).MakeGenericType(buildUnderlying);

        return buildType;
    }

    private static TableBuildResult UnsupportedJoinKind(JoinKind kind)
    {
        return TableBuildResult.Unsupported(
            $"Execution IR join lowering currently supports inner, outer, and semi joins. Found {kind}.");
    }

    private static ExecutionSourceLoop CreateSourceLoop(
        RowShape sourceShape,
        ExecutionExpression sourceRows,
        ExecutionVariable source,
        ExecutionBlock loopBody)
    {
        return ExecutionRowStreams.CreateForEach(sourceShape, sourceRows, source, loopBody);
    }

    private static ExecutionNode CreateSourceLoopWithOrdinality(
        RowShape sourceShape,
        ExecutionExpression sourceRows,
        ExecutionVariable source,
        ExecutionVariable ordinal,
        ExecutionBlock loopBody)
    {
        return ExecutionRowStreams.CreateForEachWithOrdinality(sourceShape, sourceRows, source, ordinal, loopBody);
    }

    private static ExecutionNode CreateApplySourceLoop(
        JoinSource source,
        ExecutionBlock loopBody)
    {
        return source.OrdinalityVariable == null
            ? CreateSourceLoop(source.Shape, source.Rows, source.Variable, loopBody)
            : CreateSourceLoopWithOrdinality(source.Shape, source.Rows, source.Variable, source.OrdinalityVariable, loopBody);
    }
}
