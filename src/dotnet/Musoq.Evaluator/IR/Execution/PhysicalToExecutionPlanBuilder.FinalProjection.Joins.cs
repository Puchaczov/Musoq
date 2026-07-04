using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult? TryBuildFinalJoinProjectionTable(
        PhysicalMultiStatementNode multiStatement,
        string resultTableName,
        string resultShapeName,
        MultiStatementIndexes indexes,
        PhysicalToExecutionLoweringSession session)
    {
        if (multiStatement.Statements.Length != 2)
            return null;

        var producerCteName = ResolveStatementCteName(0, indexes);
        if (string.IsNullOrWhiteSpace(producerCteName))
            return null;

        var finalPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[1]));
        if (finalPipeline is not { Source: PhysicalCteRefNode cteRef } ||
            finalPipeline.PostOperations.Count != 0 ||
            !string.Equals(cteRef.CteName, producerCteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var producerPipeline = DecomposeSupportedPipeline(UnwrapSingleStatement(multiStatement.Statements[0]));
        if (producerPipeline == null ||
            producerPipeline.Filter != null ||
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

        var result = BuildTable(
            producerPipeline with
            {
                Project = rewrite.Project,
                Filter = rewrite.Filter
            },
            resultTableName,
            resultShapeName,
            indexes.CteIndexes,
            indexes.CteShapesByName,
            session: session);
        if (!result.Supported)
            return result;

        var tableIndex = ResolveStatementTableIndex(0, indexes);
        if (tableIndex < 0)
            return TableBuildResult.Unsupported("Execution IR single-use final projection fusion cannot resolve table storage slot for statement 0.");

        return TableBuildResult.Success(
            result.Shapes,
            [CreateSingleUsePipelineFusionCandidate(tableIndex, result.Nodes)],
            result.Table,
            result.RowShape);
    }

    private static bool CanInlineFinalJoinProjectionSource(PhysicalNode source)
    {
        return source is PhysicalHashJoinNode or PhysicalNestedLoopJoinNode or PhysicalSortMergeJoinNode;
    }

    private static bool CanInlineFinalProjectionSource(PhysicalNode source)
    {
        return CanInlineFinalJoinProjectionSource(source) || IsPlainSchemaScanApplySource(source);
    }

    private static bool IsPlainSchemaScanApplySource(PhysicalNode source)
    {
        return source switch
        {
            PhysicalNestedLoopApplyNode apply =>
                IsPlainSchemaScanApplySource(apply.Left) &&
                IsPlainSchemaScanApplySource(apply.Right),
            PhysicalSchemaScanNode { Arguments.Length: 0 } => true,
            _ => false
        };
    }

    private static FinalJoinProjectionRewrite? RewriteFinalJoinProjection(
        PhysicalProjectNode finalProject,
        PhysicalFilterNode? finalFilter,
        PhysicalProjectNode producerProject,
        PhysicalCteRefNode cteRef)
    {
        var projectedExpressions = CreateProducerProjectionExpressionMap(producerProject.Fields);
        var fields = new ProjectedField[finalProject.Fields.Length];
        for (var index = 0; index < finalProject.Fields.Length; index++)
        {
            var field = finalProject.Fields[index];
            var expression = RewriteFinalJoinExpression(field.Expression, projectedExpressions, cteRef);
            if (expression == null)
                return null;

            fields[index] = field with { Expression = expression };
        }

        if (finalFilter == null)
            return new FinalJoinProjectionRewrite(
                finalProject with { Fields = fields, Input = producerProject.Input },
                null);

        var predicate = RewriteFinalJoinExpression(finalFilter.Predicate, projectedExpressions, cteRef);
        if (predicate == null)
            return null;

        var rewrittenFilter = finalFilter with { Predicate = predicate, Input = producerProject.Input };

        return new FinalJoinProjectionRewrite(
            finalProject with { Fields = fields, Input = rewrittenFilter },
            rewrittenFilter);
    }
}
