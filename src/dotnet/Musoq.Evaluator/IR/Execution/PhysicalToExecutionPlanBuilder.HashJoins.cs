using System.Collections.Generic;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildHashJoinTable(
        PhysicalHashJoinNode join,
        SupportedPipeline pipeline,
        string resultTableName,
        string resultShapeName,
        IReadOnlyDictionary<string, int> cteIndexes,
        IReadOnlyDictionary<string, GeneratedRowShape>? cteShapesByName,
        int schemaFromIndex,
        PhysicalToExecutionLoweringSession session)
    {
        if (join.BuildKeys.Length != join.ProbeKeys.Length || join.BuildKeys.Length == 0)
            return TableBuildResult.Unsupported("Execution IR hash join lowering requires matching equality key counts.");

        var sources = BuildJoinSources(
            join.Left,
            join.Right,
            cteIndexes,
            cteShapesByName,
            schemaFromIndex,
            CreateSourceRowsScope(resultTableName),
            session);
        if (!sources.Supported)
            return TableBuildResult.Unsupported(sources.UnsupportedReason);

        var joinSources = sources.Source;
        if (!TryResolveHashJoinSides(join, joinSources, out var hashSides))
            return TableBuildResult.Unsupported("Execution IR hash join lowering cannot map build/probe keys to flat join inputs.");

        if (HasDynamicHashJoinInput(joinSources))
        {
            return TableBuildResult.Unsupported(
                "Execution IR hash join lowering received dynamic join inputs. Physical planning must select nested-loop before Execution IR lowering.");
        }

        var sidecarIndex = join.Kind == JoinKind.FullOuter
            ? null
            : TryResolveCteSidecarIndex(join, hashSides, CteSidecarIndexKind.Hash) ??
              TryResolveCteSidecarIndex(join, hashSides, CteSidecarIndexKind.KeySet);
        if (sidecarIndex is { Kind: CteSidecarIndexKind.Hash } &&
            TryUseCteSidecarHashPayloadJoinSource(hashSides.Build, sidecarIndex, session, out var payloadBuildSource))
        {
            joinSources = ReferenceEquals(hashSides.Build, joinSources.Left)
                ? joinSources with { Left = payloadBuildSource }
                : joinSources with { Right = payloadBuildSource };
            hashSides = hashSides with { Build = payloadBuildSource };
        }

        var sourceLookup = RowShapeLookup.CreateSourceShapeLookup(joinSources.Left.Shape, joinSources.Right.Shape);
        var conversionLookup = RowShapeLookup.CreateTransitionAliasLookup(sourceLookup);
        var keyType = ResolveHashJoinKeyType(join);
        var hash = new ExecutionVariable(CreateScopedHashName(resultTableName, $"{hashSides.Build.Variable.Name}Hash"), typeof(object));
        var matches = new ExecutionVariable($"{hash.Name}Matches", typeof(object));
        var context = new HashJoinBuildContext(
            join,
            pipeline,
            joinSources,
            hashSides,
            sourceLookup,
            conversionLookup,
            keyType,
            hash,
            matches,
            resultTableName,
            resultShapeName,
            sidecarIndex);

        return join.Kind switch
        {
            JoinKind.Inner => BuildInnerHashJoinTable(context),
            JoinKind.LeftOuter or JoinKind.RightOuter => BuildOuterHashJoinTable(context),
            JoinKind.LeftMark => BuildMarkHashJoinTable(context),
            JoinKind.LeftSingle => BuildSingleHashJoinTable(context, session),
            JoinKind.FullOuter => BuildFullOuterHashJoinTable(context),
            JoinKind.LeftSemi => BuildSemiHashJoinTable(context, isAntiSemiJoin: false),
            JoinKind.LeftAntiSemi => BuildSemiHashJoinTable(context, isAntiSemiJoin: true),
            _ => UnsupportedJoinKind(join.Kind)
        };
    }

    private static bool HasDynamicHashJoinInput(JoinSources joinSources)
    {
        return joinSources.Left.Shape is ExpandoAdapterShape ||
               joinSources.Right.Shape is ExpandoAdapterShape;
    }

    private CteSidecarIndexSpec? TryResolveCteSidecarIndex(
        PhysicalHashJoinNode join,
        HashJoinSides sides,
        CteSidecarIndexKind expectedKind)
    {
        if (!ExecutionStrategies.TryGetCteSidecarIndexConsumer(join, out var spec) ||
            spec.Kind != expectedKind ||
            sides.Build.Node is not PhysicalCteRefNode cteRef ||
            !string.Equals(cteRef.CteName, spec.CteName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return spec;
    }

    private ExecutionCapacityHint? CreateHashCapacityCandidate(ExecutionVariable hash, JoinSource buildSource)
    {
        if (ExecutionStrategies.TryResolveCardinalityCapacity(buildSource.Node, out var cardinalityCapacity))
            return ExecutionCapacityHintCandidates.CreateConstantCandidate(hash, cardinalityCapacity);

        if (buildSource.Rows is ExecutionRowStream { Kind: ExecutionRowStreamKind.Rows } &&
            buildSource.Node is not (PhysicalHashJoinNode or PhysicalNestedLoopJoinNode or PhysicalSortMergeJoinNode))
        {
            return null;
        }

        return CreateRowsCapacityCandidate(hash, buildSource.Rows);
    }

    private ExecutionCapacityHint? CreateJoinResultCapacityCandidate(ExecutionVariable resultTable, JoinSource probeSource)
    {
        if (ExecutionStrategies.TryResolveCardinalityCapacity(probeSource.Node, out var cardinalityCapacity))
            return ExecutionCapacityHintCandidates.CreateConstantCandidate(resultTable, cardinalityCapacity);

        return CreateRowsCapacityCandidate(resultTable, probeSource.Rows);
    }

}
