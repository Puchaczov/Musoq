using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private ExecutionPlanBuildResult BuildSingleKeyAggregatePipeline(SingleKeyAggregatePipeline pipeline, string identifier)
    {
        var cteIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var table = BuildSingleKeyAggregateTable(pipeline, "result", "ResultRow0", cteIndexes);
        if (!table.Supported)
            return ExecutionPlanBuildResult.CreateUnsupported(table.UnsupportedReason);

        return ExecutionPlanBuildResult.CreateSupported(CreateTableResultPlan(identifier, table));
    }

    private TableBuildResult BuildSingleKeyAggregateTable(
        SingleKeyAggregatePipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName = null,
        int schemaFromIndex = DefaultSchemaFromIndex,
        bool scopeAggregateVariables = false)
    {
        if (pipeline.Source.Source is PhysicalNestedLoopApplyNode { Kind: ApplyKind.Cross } apply)
        {
            var chain = BuildCrossApplyChain(
                apply,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                CreateSourceRowsScope(resultTableName));
            if (chain.Supported && !chain.Chain.Sources.Any(static source => source.OrdinalityVariable != null))
            {
                return BuildSingleKeyAggregateTableFromApplyChain(
                    pipeline,
                    chain.Chain,
                    resultTableName,
                    resultShapeName,
                    scopeAggregateVariables);
            }
        }

        if (pipeline.Source.Source is PhysicalHashJoinNode { Kind: JoinKind.Inner } hashJoin)
        {
            var hashJoinSource = BuildSingleKeyAggregateHashJoinSource(
                hashJoin,
                resultTableName,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex);
            if (hashJoinSource.Supported)
            {
                return BuildSingleKeyAggregateTableCore(
                    pipeline,
                    hashJoinSource.Source,
                    resultTableName,
                    resultShapeName,
                    scopeAggregateVariables);
            }
        }

        var aggregateSource = BuildAggregateSource(
            pipeline.Source.Source,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            "single-key aggregate");
        if (!aggregateSource.Supported)
            return TableBuildResult.Unsupported(aggregateSource.UnsupportedReason);

        var sourceShape = aggregateSource.Source.Shape;
        var source = new SingleKeyAggregateExecutionSource(
            RowShapeLookup.CreateSourceShapeLookup(sourceShape),
            aggregateSource.Source.Shapes,
            aggregateSource.Source.Setup,
            body => CreateSourceLoop(sourceShape, aggregateSource.Source.Rows, aggregateSource.Source.Variable, body),
            aggregateSource.Source.Variable,
            aggregateSource.Source.Rows);

        return BuildSingleKeyAggregateTableCore(
            pipeline,
            source,
            resultTableName,
            resultShapeName,
            scopeAggregateVariables);
    }

}
