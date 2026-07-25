using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildSingleKeyAggregateTable(
        AggregateSingleKeyPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        if (pipeline.Source.Source is PhysicalNestedLoopApplyNode { Kind: ApplyKind.Cross } apply)
        {
            var chain = BuildCrossApplyChain(
                apply,
                cteIndexes,
                cteShapesByName,
                schemaFromIndex,
                new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                CreateSourceRowsScope(resultTableName),
                scope);
            if (chain.IsBuilt && !chain.Chain.Sources.Any(static source => source.OrdinalityVariable != null))
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
                schemaFromIndex,
                scope);
            if (hashJoinSource.IsBuilt)
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
            "single-key aggregate",
            scope);
        if (!aggregateSource.IsBuilt)
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
