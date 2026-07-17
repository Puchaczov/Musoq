using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private TableBuildResult CompleteHashJoinTableBuild(
        HashJoinBuildContext context,
        HashJoinTableLowering lowering)
    {
        context = PruneFusedHashPayload(context, lowering.MatchedBody);
        var matchesLoop = new ExecutionForEach(
            context.Sides.Build.Variable,
            new ExecutionVariableRead(context.Matches),
            lowering.MatchedBody);
        var probeLoop = CreateHashJoinProbeLoop(context, matchesLoop, lowering);
        var nodes = CreateJoinPrelude(
            context.Sources,
            lowering.ResultTable,
            lowering.ResultShape,
            CreateJoinResultCapacityCandidate(lowering.ResultTable, context.Sides.Probe));
        if (lowering.PreludeNodes != null)
            nodes.AddRange(lowering.PreludeNodes);

        if (context.CteSidecarIndex is { Kind: CteSidecarIndexKind.Hash } sidecar)
        {
            nodes.Add(new ExecutionCteSidecarIndexLoadCandidate(
                context.Hash,
                sidecar.IndexSlot,
                ExecutionCteSidecarIndexKind.Hash,
                context.KeyType,
                context.Sides.Build.Variable.Type.ClrType,
                context.Sides.Build.Variable.GeneratedRowTypeName));
        }
        else
        {
            var buildLoop = CreateHashBuildLoop(context);
            nodes.Add(new ExecutionCreateHash(
                context.Hash,
                context.KeyType,
                context.Sides.Build.Variable.Type.ClrType,
                CreateHashCapacityCandidate(context.Hash, context.Sides.Build),
                context.Sides.Build.Variable.GeneratedRowTypeName));
            nodes.Add(buildLoop);
        }

        nodes.Add(probeLoop);

        return CompleteTableBuild(
            [..context.Sources.Left.Shapes, ..context.Sources.Right.Shapes, lowering.ResultShape],
            nodes,
            lowering.ResultTable,
            lowering.ResultShape,
            context.Pipeline.PostOperations,
            context.Pipeline.Project.IsDistinct);
    }

    private static ExecutionSourceLoop CreateHashJoinProbeLoop(
        HashJoinBuildContext context,
        ExecutionForEach matchesLoop,
        HashJoinTableLowering lowering)
    {
        return CreateSourceLoop(
            context.Sides.Probe.Shape,
            context.Sides.Probe.Rows,
            context.Sides.Probe.Variable,
            new ExecutionBlock(
            [
                new ExecutionHashProbe(
                    context.Hash,
                    context.Matches,
                    CreateHashJoinKeyExpression(context.Join.ProbeKeys, context.ConversionLookup, context.KeyType),
                    context.KeyType,
                    context.Sides.Build.Variable.Type.ClrType,
                    new ExecutionBlock([matchesLoop]),
                    lowering.NoMatchBody,
                    lowering.HasMatch,
                    context.Sides.Build.Variable.GeneratedRowTypeName)
            ]));
    }

    private static ExecutionBlock CreateConditionalJoinBlock(
        IrExpression? condition,
        IReadOnlyDictionary<string, RowShape> sourceLookup,
        ExecutionBlock body)
    {
        if (condition == null)
            return body;

        return new ExecutionBlock(
        [
            new ExecutionIf(
                ExecutionExpressionConverter.Convert(condition, sourceLookup),
                body)
        ]);
    }
}
