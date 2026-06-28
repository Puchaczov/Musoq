using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static bool TryApplySidecarJoinProjectionStage(
        SidecarJoinPipelineStage stage,
        IReadOnlyDictionary<string, IrExpression>? currentProjectionMap,
        IReadOnlyDictionary<string, RowShape> activeLookup,
        List<SidecarJoinRuntimeOperation> runtimeOperations,
        ref ProjectedField[]? finalFields,
        ref IReadOnlyDictionary<string, RowShape>? finalOutputLookup,
        out Dictionary<string, IrExpression>? projectedMap)
    {
        projectedMap = null;

        if (stage.Pipeline.Source is not PhysicalCteRefNode cteRef)
            return false;

        if (stage.Pipeline.Project.IsDistinct ||
            stage.Pipeline.PostOperations.Count != 0 ||
            string.IsNullOrWhiteSpace(stage.ExpectedInputCteName) ||
            !string.Equals(cteRef.CteName, stage.ExpectedInputCteName, StringComparison.OrdinalIgnoreCase) ||
            currentProjectionMap == null)
        {
            return false;
        }

        var rewrittenFields = RewriteSidecarJoinProjectedFields(
            stage.Pipeline.Project.Fields,
            currentProjectionMap,
            cteRef);
        if (rewrittenFields == null)
            return false;

        var rewrittenFilter = RewriteSidecarJoinFilter(stage.Pipeline.Filter, currentProjectionMap, cteRef);
        if (stage.Pipeline.Filter != null && rewrittenFilter == null)
            return false;

        if (rewrittenFilter != null)
        {
            if (!TryCreateSidecarJoinRuntimeGuard(
                    rewrittenFilter.Predicate,
                    activeLookup,
                    runtimeOperations.Count,
                    out var guard))
            {
                return false;
            }

            runtimeOperations.Add(guard);
        }

        if (stage.OutputCteName == null)
        {
            finalFields = rewrittenFields;
            finalOutputLookup = activeLookup;
            projectedMap = currentProjectionMap is Dictionary<string, IrExpression> dictionary
                ? dictionary
                : new Dictionary<string, IrExpression>(currentProjectionMap, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        projectedMap = CreateProducerProjectionExpressionMap(rewrittenFields);
        return true;
    }

    private bool TryResolveSidecarBuildCteRef(
        PhysicalHashJoinNode join,
        out PhysicalCteRefNode buildCteRef,
        out CteSidecarIndexSpec sidecar)
    {
        buildCteRef = null!;
        sidecar = null!;

        if (!ExecutionStrategies.TryGetCteSidecarIndexConsumer(join, out var resolvedSidecar))
            return false;

        sidecar = resolvedSidecar;

        if (join.Left is PhysicalCteRefNode left &&
            string.Equals(left.CteName, sidecar.CteName, StringComparison.OrdinalIgnoreCase) &&
            HashBuildAliasUsage.BuildKeysReferenceAlias(join, left.Alias))
        {
            buildCteRef = left;
            return true;
        }

        if (join.Right is PhysicalCteRefNode right &&
            string.Equals(right.CteName, sidecar.CteName, StringComparison.OrdinalIgnoreCase) &&
            HashBuildAliasUsage.BuildKeysReferenceAlias(join, right.Alias))
        {
            buildCteRef = right;
            return true;
        }

        return false;
    }

    private static bool IsSupportedSidecarPipelineJoin(
        PhysicalHashJoinNode join,
        CteSidecarIndexSpec sidecar)
    {
        return join.Kind switch
        {
            JoinKind.Inner => sidecar.Kind == CteSidecarIndexKind.Hash,
            JoinKind.LeftSemi => sidecar.Kind == CteSidecarIndexKind.KeySet && join.Residual == null,
            _ => false
        };
    }

    private static ExecutionNode CreateSidecarJoinIndexLoad(
        ExecutionVariable indexVariable,
        JoinSource buildSource,
        CteSidecarIndexSpec sidecar)
    {
        return sidecar.Kind switch
        {
            CteSidecarIndexKind.Hash => new ExecutionCteSidecarIndexLoadCandidate(
                indexVariable,
                sidecar.IndexSlot,
                ExecutionCteSidecarIndexKind.Hash,
                sidecar.KeyType,
                buildSource.Variable.Type,
                buildSource.Variable.GeneratedRowTypeName),
            CteSidecarIndexKind.KeySet => new ExecutionCteSidecarIndexLoadCandidate(
                indexVariable,
                sidecar.IndexSlot,
                ExecutionCteSidecarIndexKind.KeySet,
                sidecar.KeyType),
            _ => throw new NotSupportedException($"Unsupported CTE sidecar index kind '{sidecar.Kind}'.")
        };
    }

    private static ExecutionVariable CreateSidecarJoinIndexVariable(
        string resultTableName,
        string? stageOutputName,
        string buildAlias,
        CteSidecarIndexKind kind,
        int stageIndex)
    {
        var scope = string.IsNullOrWhiteSpace(stageOutputName)
            ? resultTableName
            : stageOutputName;
        var suffix = kind == CteSidecarIndexKind.KeySet ? "Keys" : "Hash";

        return new ExecutionVariable(
            CreateIdentifierCandidate(
                CreateScopedHashName(scope, $"{buildAlias}{suffix}"),
                stageIndex),
            typeof(object));
    }

    private static ExecutionBlock CreateSidecarJoinStepBlock(
        SidecarJoinRuntimeStep step,
        ExecutionBlock continuation)
    {
        var conditioned = CreateSidecarJoinConditionBlock(
            step.Residual,
            step.Filter,
            step.SourceLookup,
            continuation);
        var probeKey = CreateHashJoinKeyExpression(
            step.ProbeKeys,
            step.SourceLookup,
            step.Sidecar.KeyType);

        if (step.Sidecar.Kind == CteSidecarIndexKind.KeySet)
        {
            return new ExecutionBlock(
            [
                new ExecutionKeySetProbe(
                    step.Index,
                    probeKey,
                    step.Sidecar.KeyType,
                    conditioned,
                    KeyVariableName: $"{step.Index.Name}Key")
            ]);
        }

        var matchesLoop = new ExecutionForEach(
            step.Build.Variable,
            new ExecutionVariableRead(step.Matches!),
            conditioned);

        return new ExecutionBlock(
        [
            new ExecutionHashProbe(
                step.Index,
                step.Matches!,
                probeKey,
                step.Sidecar.KeyType,
                step.Build.Variable.Type,
                new ExecutionBlock([matchesLoop]),
                GeneratedRowTypeName: step.Build.Variable.GeneratedRowTypeName,
                KeyVariableName: $"{step.Index.Name}Key")
        ]);
    }

    private static ExecutionBlock CreateSidecarJoinConditionBlock(
        IrExpression? residual,
        PhysicalFilterNode? filter,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        ExecutionBlock body)
    {
        var condition = AppendPredicate(residual, filter?.Predicate);
        return condition == null
            ? body
            : CreateConditionalJoinBlock(condition, sourceLookup, body);
    }

}
