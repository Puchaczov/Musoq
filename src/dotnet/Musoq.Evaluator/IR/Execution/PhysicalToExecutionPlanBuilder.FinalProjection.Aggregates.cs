using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildFinalJoinAggregateProjectionTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables)
    {
        if (multiStatement.Statements.Length != 3)
            return null;

        var joinProducerCteName = ResolveStatementCteName(0, indexes);
        var aggregateProducerCteName = ResolveStatementCteName(1, indexes);
        if (string.IsNullOrWhiteSpace(joinProducerCteName) ||
            string.IsNullOrWhiteSpace(aggregateProducerCteName))
        {
            return null;
        }

        var classifications = ClassifyMultiStatementCteReferences(multiStatement, indexes);
        if (!CanFuseReadOnceCte(joinProducerCteName, classifications) ||
            !CanFuseReadOnceCte(aggregateProducerCteName, classifications))
        {
            return null;
        }

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[2]));
        if (finalPipeline is not { Source: PhysicalCteRefNode finalCteRef, Filter: null } ||
            finalPipeline.PostOperations.Count != 0 ||
            !string.Equals(finalCteRef.CteName, aggregateProducerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var joinProducerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[0]));
        if (joinProducerPipeline is not { Filter: null, PostOperations.Count: 0 } ||
            !CanInlineFinalJoinProjectionSource(joinProducerPipeline.Source))
        {
            return null;
        }

        var aggregate = DecomposeSingleKeyAggregatePipeline(UnwrapSingleStatement(multiStatement.Statements[1]));
        if (aggregate is not { Source.Source: PhysicalCteRefNode aggregateSourceCteRef, PostOperations.Count: 0 } ||
            aggregate.Source.Filter != null ||
            !string.Equals(aggregateSourceCteRef.CteName, joinProducerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var projectedExpressions = CreateProducerProjectionExpressionMap(joinProducerPipeline.Project.Fields);
        var rewrittenGroupKey = RewriteFinalJoinExpression(
            aggregate.GroupKey,
            projectedExpressions,
            aggregateSourceCteRef);
        if (rewrittenGroupKey == null)
            return null;

        var rewrittenBindings = RewriteAggregateBindings(
            aggregate.Bindings,
            projectedExpressions,
            aggregateSourceCteRef);
        if (rewrittenBindings == null)
            return null;

        var rewrittenProject = RewriteAggregateProject(
            aggregate.Project,
            projectedExpressions,
            aggregateSourceCteRef);
        if (rewrittenProject == null)
            return null;

        var groupKeys = new AggregateFinalizationGroupKeys(
            [rewrittenGroupKey],
            [aggregate.GroupKeyName],
            [aggregate.GroupKeyType]);
        if (!TryRewriteFinalAggregateProjection(
                finalPipeline.Project,
                finalPipeline.PostOperations,
                rewrittenBindings,
                groupKeys,
                out var finalProject,
                out var postOperations))
        {
            return null;
        }

        return BuildSingleKeyAggregateTable(
            aggregate with
            {
                Project = finalProject with { Input = rewrittenProject.Input },
                Bindings = rewrittenBindings,
                Source = new SourcePipeline(joinProducerPipeline.Source, joinProducerPipeline.Filter),
                GroupKey = rewrittenGroupKey,
                PostOperations = postOperations
            },
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            scopeAggregateVariables: scopeAggregateVariables);
    }

    private TableBuildResult? TryBuildFinalAggregateProjectionTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables)
    {
        if (multiStatement.Statements.Length != 2)
            return null;

        var producerCteName = ResolveStatementCteName(0, indexes);
        if (string.IsNullOrWhiteSpace(producerCteName))
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[1]));
        if (finalPipeline is not { Source: PhysicalCteRefNode cteRef, Filter: null } ||
            !string.Equals(cteRef.CteName, producerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var producer = UnwrapSingleStatement(multiStatement.Statements[0]);

        var aggregateOnly = DecomposeAggregateOnlyPipeline(producer);
        if (aggregateOnly is { PostOperations.Count: 0 })
        {
            var groupKeys = new AggregateFinalizationGroupKeys([], [], []);
            if (!TryRewriteFinalAggregateProjection(
                    finalPipeline.Project,
                    finalPipeline.PostOperations,
                    aggregateOnly.Bindings,
                    groupKeys,
                    out var project,
                    out var postOperations))
                return null;

            return BuildAggregateOnlyTable(
                aggregateOnly with
                {
                    Project = project,
                    PostOperations = postOperations
                },
                resultTableName,
                resultShapeName,
                indexes.CteIndexes,
                indexes.CteShapesByName,
                scopeAggregateVariables: scopeAggregateVariables);
        }

        var singleKeyAggregate = DecomposeSingleKeyAggregatePipeline(producer);
        if (singleKeyAggregate is { PostOperations.Count: 0 })
        {
            var groupKeys = new AggregateFinalizationGroupKeys(
                [singleKeyAggregate.GroupKey],
                [singleKeyAggregate.GroupKeyName],
                [singleKeyAggregate.GroupKeyType]);
            if (!TryRewriteFinalAggregateProjection(
                    finalPipeline.Project,
                    finalPipeline.PostOperations,
                    singleKeyAggregate.Bindings,
                    groupKeys,
                    out var project,
                    out var postOperations))
                return null;

            return BuildSingleKeyAggregateTable(
                singleKeyAggregate with
                {
                    Project = project,
                    PostOperations = postOperations
                },
                resultTableName,
                resultShapeName,
                indexes.CteIndexes,
                indexes.CteShapesByName,
                scopeAggregateVariables: scopeAggregateVariables);
        }

        var valueTupleAggregate = DecomposeValueTupleAggregatePipeline(producer);
        if (valueTupleAggregate is { PostOperations.Count: 0 })
        {
            var groupKeys = new AggregateFinalizationGroupKeys(
                valueTupleAggregate.GroupKeys,
                valueTupleAggregate.GroupKeyNames,
                valueTupleAggregate.GroupKeyTypes);
            if (!TryRewriteFinalAggregateProjection(
                    finalPipeline.Project,
                    finalPipeline.PostOperations,
                    valueTupleAggregate.Bindings,
                    groupKeys,
                    out var project,
                    out var postOperations))
                return null;

            return BuildValueTupleAggregateTable(
                valueTupleAggregate with
                {
                    Project = project,
                    PostOperations = postOperations
                },
                resultTableName,
                resultShapeName,
                indexes.CteIndexes,
                indexes.CteShapesByName,
                scopeAggregateVariables: scopeAggregateVariables);
        }

        return null;
    }
}
