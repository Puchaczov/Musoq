using System.Linq;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult BuildSemiKeySetJoinTable(HashJoinBuildContext context, bool isAntiSemiJoin)
    {
        context = DropUnusedFusedPayloadForKeySet(context);
        var outputLookup = RowShapeLookup.CreateSourceShapeLookup(context.Sides.Probe.Shape);
        var resultShape = CreateGeneratedShape(context.ResultShapeName, context.Pipeline.Project.Fields, outputLookup);
        var resultTable = new ExecutionVariable(context.ResultTableName, typeof(object));
        var appendRow = CreateAppendRow(resultTable, resultShape, context.Pipeline.Project.Fields, outputLookup);
        var keySet = new ExecutionVariable(CreateKeySetName(context.Hash.Name), typeof(object));
        var hasMatch = isAntiSemiJoin ? new ExecutionVariable($"{keySet.Name}HasMatch", typeof(bool)) : null;
        var matchedBody = isAntiSemiJoin
            ? new ExecutionBlock([new ExecutionAssign(hasMatch!, new ExecutionLiteral(true, typeof(bool)))])
            : CreateLoopBody(context.Pipeline.Filter, appendRow, outputLookup);
        var noMatchBody = isAntiSemiJoin ? CreateLoopBody(context.Pipeline.Filter, appendRow, outputLookup) : null;
        var nodes = CreateJoinPrelude(
            context.Sources,
            resultTable,
            resultShape,
            CreateJoinResultCapacityCandidate(resultTable, context.Sides.Probe));

        if (context.CteSidecarIndex is { Kind: CteSidecarIndexKind.KeySet } sidecar)
        {
            nodes.Add(new ExecutionCteSidecarIndexLoadCandidate(
                keySet,
                sidecar.IndexSlot,
                ExecutionCteSidecarIndexKind.KeySet,
                context.KeyType));
        }
        else
        {
            nodes.Add(new ExecutionCreateKeySet(
                keySet,
                context.KeyType,
                CreateHashCapacityCandidate(keySet, context.Sides.Build)));
            nodes.Add(CreateKeySetBuildLoop(context, keySet));
        }

        nodes.Add(CreateSourceLoop(
            context.Sides.Probe.Shape,
            context.Sides.Probe.Rows,
            context.Sides.Probe.Variable,
            new ExecutionBlock(
            [
                new ExecutionKeySetProbe(
                    keySet,
                    CreateHashJoinKeyExpression(context.Join.ProbeKeys, context.ConversionLookup, context.KeyType),
                    context.KeyType,
                    matchedBody,
                    noMatchBody,
                    hasMatch)
            ])));

        return CompleteTableBuild(
            [..context.Sources.Left.Shapes, ..context.Sources.Right.Shapes, resultShape],
            nodes,
            resultTable,
            resultShape,
            context.Pipeline.PostOperations,
            context.Pipeline.Project.IsDistinct);
    }

    private static bool CanUsePayloadFreeSemiJoinKeySet(HashJoinBuildContext context)
    {
        return context.Join.Residual == null &&
               context.KeyType != typeof(object) &&
               context.Sides.Build.Shape is not ExpandoAdapterShape &&
               context.Sides.Probe.Shape is not ExpandoAdapterShape &&
               CanBuildPayloadFreeKeySet(context);
    }

    private static HashJoinBuildContext DropUnusedFusedPayloadForKeySet(HashJoinBuildContext context)
    {
        if (context.Sides.Build.FusedHashPayload is not { } payload)
            return context;

        var build = context.Sides.Build with
        {
            FusedHashPayload = null,
            Shapes = context.Sides.Build.Shapes
                .Where(shape => shape is not HashPayloadShape hashPayload ||
                                !string.Equals(hashPayload.TypeName, payload.Shape.TypeName, StringComparison.Ordinal))
                .ToArray()
        };
        var sides = context.Sides with { Build = build };
        return context with
        {
            Sources = ReplaceBuildSource(context.Sources, context.Sides.Build, build),
            Sides = sides
        };
    }

    private static bool CanBuildPayloadFreeKeySet(HashJoinBuildContext context)
    {
        if (context.Sides.Build.FusedHashBuild is not { } fused)
            return true;

        var key = CreateHashJoinKeyExpression(
            context.Join.BuildKeys,
            context.ConversionLookup,
            context.KeyType);
        return TryRewritePayloadKeyToProducerKey(
            key,
            context.Sides.Build.Variable.Name,
            fused.RowShape,
            fused.RowValues,
            out _);
    }

    private static ExecutionSourceLoop CreateKeySetBuildLoop(HashJoinBuildContext context, ExecutionVariable keySet)
    {
        var key = CreateHashJoinKeyExpression(
            context.Join.BuildKeys,
            context.ConversionLookup,
            context.KeyType);

        if (context.Sides.Build.FusedHashBuild is { } fused &&
            TryRewritePayloadKeyToProducerKey(
                key,
                context.Sides.Build.Variable.Name,
                fused.RowShape,
                fused.RowValues,
                out var producerKey))
        {
            return CreateSourceLoop(
                fused.ProducerShape,
                context.Sides.Build.Rows,
                fused.ProducerVariable,
                new ExecutionBlock([new ExecutionKeySetAdd(keySet, producerKey, context.KeyType)]));
        }

        return CreateSourceLoop(
            context.Sides.Build.Shape,
            context.Sides.Build.Rows,
            context.Sides.Build.Variable,
            new ExecutionBlock([new ExecutionKeySetAdd(keySet, key, context.KeyType)]));
    }

    private static string CreateKeySetName(string hashName)
    {
        const string hashSuffix = "Hash";

        return hashName.EndsWith(hashSuffix, StringComparison.Ordinal)
            ? $"{hashName[..^hashSuffix.Length]}Keys"
            : $"{hashName}Keys";
    }
}
