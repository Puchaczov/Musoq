using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildSingleUseProjectionChainTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables)
    {
        if (multiStatement.Statements.Length < 2)
            return null;

        var producerIndex = multiStatement.Statements.Length - 2;
        var producerCteName = ResolveStatementCteName(producerIndex, indexes);
        if (string.IsNullOrWhiteSpace(producerCteName))
            return null;

        var classifications = ClassifyMultiStatementCteReferences(multiStatement, indexes);
        if (!CanFuseReadOnceCte(producerCteName, classifications))
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[^1]));
        if (finalPipeline is not { Source: PhysicalCteRefNode cteRef } ||
            finalPipeline.PostOperations.Count != 0 ||
            !string.Equals(cteRef.CteName, producerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var producerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[producerIndex]));
        if (producerPipeline == null ||
            producerPipeline.Project.IsDistinct ||
            producerPipeline.PostOperations.Count != 0 ||
            !CanInlineFinalProjectionSource(producerPipeline.Source))
        {
            return null;
        }

        var rewrite = RewriteFinalJoinProjection(
            finalPipeline.Project,
            finalPipeline.Filter,
            producerPipeline.Project,
            cteRef);
        if (rewrite == null)
            return null;

        var prefix = BuildMultiStatementPrefix(
            multiStatement,
            producerIndex,
            indexes,
            scopeAggregateVariables);
        if (!prefix.Supported)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var result = BuildTable(
            producerPipeline with
            {
                Project = rewrite.Project,
                Filter = CombineProducerAndFinalFilters(
                    producerPipeline.Filter,
                    rewrite.Filter,
                    producerPipeline.Source)
            },
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName);
        if (!result.Supported)
            return result;

        var tableIndex = ResolveStatementTableIndex(producerIndex, indexes);
        if (tableIndex < 0)
        {
            return TableBuildResult.Unsupported(
                $"Execution IR single-use projection fusion cannot resolve table storage slot for statement {producerIndex.ToString(CultureInfo.InvariantCulture)}.");
        }

        return TableBuildResult.Success(
            [..prefix.Shapes, ..result.Shapes],
            [..prefix.Nodes, CreateSingleUsePipelineFusionCandidate(tableIndex, result.Nodes)],
            result.Table,
            result.RowShape);
    }

    private static ExecutionSingleUsePipelineFusionCandidate CreateSingleUsePipelineFusionCandidate(
        int relatedTableIndex,
        IReadOnlyList<ExecutionNode> body)
    {
        return new ExecutionSingleUsePipelineFusionCandidate(
            relatedTableIndex,
            new ExecutionBlock(body));
    }
}
