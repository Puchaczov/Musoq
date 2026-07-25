using System.Collections.Generic;
using System.Globalization;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult? TryBuildSingleUseProjectionChainTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        bool scopeAggregateVariables,
        LoweringScope scope)
    {
        if (multiStatement.Statements.Length < 2)
            return null;

        var producerIndex = multiStatement.Statements.Length - 2;
        var recursiveSink = scope.RecursiveCteSink;
        var producerCteName = ResolveStatementCteName(producerIndex, indexes);
        if (recursiveSink == null)
        {
            if (string.IsNullOrWhiteSpace(producerCteName))
                return null;

            var classifications = ClassifyMultiStatementCteReferences(multiStatement, indexes);
            if (!CanFuseReadOnceCte(producerCteName, classifications))
                return null;
        }

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[^1]));
        if (finalPipeline is not { Source: PhysicalCteRefNode cteRef } ||
            finalPipeline.PostOperations.Count != 0 ||
            (recursiveSink == null &&
             !string.Equals(cteRef.CteName, producerCteName, StringComparison.OrdinalIgnoreCase)))
        {
            return recursiveSink == null
                ? null
                : TableBuildResult.Unsupported(
                    $"Recursive CTE '{recursiveSink.CteName}' final projection is not a direct read of its branch producer.");
        }

        var producerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[producerIndex]));
        if (producerPipeline == null ||
            producerPipeline.Project.IsDistinct ||
            producerPipeline.PostOperations.Count != 0 ||
            (!CanInlineFinalProjectionSource(producerPipeline.Source) &&
             (scope.RecursiveCteSink == null ||
              !CanInlineRecursiveCteProjectionSource(producerPipeline.Source))))
        {
            return recursiveSink == null
                ? null
                : TableBuildResult.Unsupported(
                    $"Recursive CTE '{recursiveSink.CteName}' branch producer is not a supported direct projection pipeline " +
                    $"({producerPipeline?.Source.GetType().Name ?? "unknown source"}).");
        }

        var rewrite = RewriteFinalJoinProjection(
            finalPipeline.Project,
            finalPipeline.Filter,
            producerPipeline.Project,
            cteRef);
        if (rewrite == null)
        {
            return recursiveSink == null
                ? null
                : TableBuildResult.Unsupported(
                    $"Recursive CTE '{recursiveSink.CteName}' final projection cannot be fused with its branch producer.");
        }

        var prefix = BuildMultiStatementPrefix(
            multiStatement,
            producerIndex,
            indexes,
            scopeAggregateVariables,
            scope);
        if (!prefix.IsBuilt)
            return TableBuildResult.Unsupported(prefix.UnsupportedReason);

        var result = BuildTable(
            producerPipeline with
            {
                Project = rewrite.Project,
                Filter = ReadOnceCteProjectionLowerer.CombineProducerAndFinalFilters(
                    producerPipeline.Filter,
                    rewrite.Filter,
                    producerPipeline.Source)
            },
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            schemaFromIndex: DefaultSchemaFromIndex,
            scope: scope);
        if (!result.IsBuilt)
            return result;

        var tableIndex = recursiveSink == null
            ? ResolveStatementTableIndex(producerIndex, indexes)
            : -1;
        if (recursiveSink == null && tableIndex < 0)
        {
            return TableBuildResult.Unsupported(
                $"Execution IR single-use projection fusion cannot resolve table storage slot for statement {producerIndex.ToString(CultureInfo.InvariantCulture)}.");
        }

        IReadOnlyList<ExecutionNode> resultNodes = recursiveSink == null
            ? [CreateSingleUsePipelineFusionCandidate(tableIndex, result.Nodes)]
            : result.Nodes;

        return TableBuildResult.Success(
            [..prefix.Shapes, ..result.Shapes],
            [..prefix.Nodes, ..resultNodes],
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

    private static bool CanInlineRecursiveCteProjectionSource(PhysicalNode source)
    {
        return source switch
        {
            PhysicalNestedLoopJoinNode or PhysicalHashJoinNode or PhysicalSortMergeJoinNode => true,
            PhysicalNestedLoopApplyNode apply =>
                CanInlineRecursiveCteProjectionInput(apply.Left) &&
                CanInlineRecursiveCteProjectionInput(apply.Right),
            _ => false
        };
    }

    private static bool CanInlineRecursiveCteProjectionInput(PhysicalNode source)
    {
        return source switch
        {
            PhysicalCteRefNode or PhysicalSchemaScanNode or PhysicalValuesScanNode => true,
            PhysicalNestedLoopApplyNode apply =>
                CanInlineRecursiveCteProjectionInput(apply.Left) &&
                CanInlineRecursiveCteProjectionInput(apply.Right),
            _ => false
        };
    }
}
