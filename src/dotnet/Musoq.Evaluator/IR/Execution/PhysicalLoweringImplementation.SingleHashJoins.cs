using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult BuildSingleHashJoinTable(
        HashJoinBuildContext context,
        LoweringScope scope)
    {
        if (context.Join.Kind != JoinKind.LeftSingle ||
            !ReferenceEquals(context.Sides.Build, context.Sources.Right))
        {
            return TableBuildResult.Unsupported(
                "Execution IR single hash join lowering requires the right side to be the hash build side.");
        }

        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var nullAlias = RowShapeLookup.ResolveSourceAlias(context.Sources.Right.Shape);
        IReadOnlyDictionary<string, ExecutionExpression>? emptyDefaults = null;
        IReadOnlyList<ExecutionNode>? preludeNodes = null;
        if (TryCreateScalarSubqueryEmptyResultLowering(
                scope,
                context.Sides.Build.Node,
                context.Hash.Name,
                out var valueColumnName,
                out var emptyLowering))
        {
            emptyDefaults = new Dictionary<string, ExecutionExpression>(StringComparer.OrdinalIgnoreCase)
            {
                [valueColumnName] = emptyLowering.Value
            };
            preludeNodes = emptyLowering.PreludeNodes;
        }

        var projection = CreateNullExtendedProjection(new NullExtendedProjectionContext(
            context.ResultShapeName,
            resultTable,
            context.Pipeline.Project.Fields,
            context.SourceLookup,
            nullAlias,
            emptyDefaults));
        if (!projection.IsBuilt)
            return TableBuildResult.Unsupported(projection.UnsupportedReason);

        var appendBlocks = CreateOuterApplyAppendBlocks(
            context.Pipeline.Filter,
            projection.MatchedAppendRow,
            projection.UnmatchedAppendRow,
            context.SourceLookup,
            nullAlias);
        if (!appendBlocks.IsBuilt)
            return TableBuildResult.Unsupported(appendBlocks.UnsupportedReason);

        var hasMatch = new ExecutionVariable($"{context.Hash.Name}HasMatch", typeof(bool));
        var matchedBody = CreateOuterHashJoinMatchedBody(
            context.Join.Residual,
            context.Pipeline.Filter,
            hasMatch,
            projection.MatchedAppendRow,
            context.ConversionLookup);
        return CompleteHashJoinTableBuild(
            context,
            new HashJoinTableLowering(
                resultTable,
                projection.ResultShape,
                matchedBody,
                appendBlocks.UnmatchedAppendBlock,
                hasMatch,
                PreludeNodes: preludeNodes));
    }
}
