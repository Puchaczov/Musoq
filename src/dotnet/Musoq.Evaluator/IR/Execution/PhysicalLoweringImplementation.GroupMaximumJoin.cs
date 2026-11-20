using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed partial class PhysicalLoweringImplementation
{
    private TableBuildResult? TryBuildGroupMaximumJoin(HashJoinBuildContext context, GroupMaximumSummary summary)
    {
        var build = context.Sides.Build;
        var key = CreateHashJoinKeyExpression(context.Join.BuildKeys, context.ConversionLookup, context.KeyType);
        var value = ExecutionExpressionConverter.Convert(summary.Build, context.ConversionLookup);
        var inputShape = build.Shape;
        var inputRow = build.Variable;
        if (build.FusedHashBuild is { } fused)
        {
            if (
                !TryRewritePayloadKeyToProducerKey(
                    key,
                    build.Variable.Name,
                    fused.RowShape,
                    fused.RowValues,
                    out var producerKey
                )
                || !TryRewritePayloadKeyToProducerKey(
                    value,
                    build.Variable.Name,
                    fused.RowShape,
                    fused.RowValues,
                    out var producerValue
                )
                || !ExpressionStabilityAnalyzer.IsStable(producerValue)
            )
                return null;
            key = producerKey;
            value = producerValue;
            inputShape = fused.ProducerShape;
            inputRow = fused.ProducerVariable;
            context = DropUnusedFusedPayloadForKeySet(context);
        }

        var output = RowShapeLookup.CreateSourceShapeLookup(context.Sides.Probe.Shape);
        var resultShape = CreateGeneratedShape(context.ResultShapeName, context.Pipeline.Project.Fields, output);
        var table = new ExecutionVariable(context.ResultTableName, typeof(object));
        var append = CreateAppendRow(table, resultShape, context.Pipeline.Project.Fields, output);
        var nodes = CreateJoinPrelude(
            context.Sources,
            table,
            resultShape,
            context.Scope,
            CreateJoinResultCapacityCandidate(table, context.Sides.Probe)
        );
        var valueType = summary.Build.ReturnType;
        var candidate = new ExecutionVariable(context.Hash.Name + "Value", valueType);
        nodes.Add(new ExecutionCreateHash(context.Hash, context.KeyType, valueType) { TracksMaximum = true });
        var update = new ExecutionHashAdd(context.Hash, key, candidate, context.KeyType, valueType)
        {
            TracksMaximum = true,
        };
        var buildBody = new List<ExecutionNode>();
        // A rejected correlation key must not evaluate the projected value.
        if (key is not ExecutionValueTupleKey tuple)
            return null;
        if (tuple.Parts.Any(part => CanBeNullType(part.ReturnType.ResolveClrType())))
        {
            if (!TryCreatePrePayloadValueTupleKeyNodes(tuple, context.KeyType, buildBody, out var validated))
                return null;
            update = update with { Key = new ExecutionVariableRead(validated), PrecomputedKey = validated };
        }
        buildBody.Add(new ExecutionLet(candidate, value));
        buildBody.Add(update);
        nodes.Add(CreateSourceLoop(inputShape, build.Rows, inputRow, new ExecutionBlock(buildBody)));

        var maximum = new ExecutionVariable(context.Hash.Name + "Maximum", valueType);
        var hasMatch = new ExecutionVariable(context.Hash.Name + "HasMatch", typeof(bool));
        var probe = new ExecutionVariable(context.Hash.Name + "Probe", summary.Probe.ReturnType);
        var probeRead = new ExecutionVariableRead(probe);
        var maximumRead = new ExecutionVariableRead(maximum);
        ExecutionExpression fails = new ExecutionBinary(BinaryOpKind.LessOrEqual, probeRead, maximumRead, typeof(bool));
        if (CanBeNullType(valueType))
            fails = new ExecutionBinary(
                BinaryOpKind.Or,
                new ExecutionIsNullCheck(maximumRead, false, typeof(bool)),
                fails,
                typeof(bool)
            );
        if (CanBeNullType(summary.Probe.ReturnType))
            fails = new ExecutionBinary(
                BinaryOpKind.Or,
                new ExecutionIsNullCheck(probeRead, false, typeof(bool)),
                fails,
                typeof(bool)
            );
        nodes.Add(
            CreateSourceLoop(
                context.Sides.Probe.Shape,
                context.Sides.Probe.Rows,
                context.Sides.Probe.Variable,
                new ExecutionBlock([
                    new ExecutionHashProbe(
                        context.Hash,
                        maximum,
                        CreateHashJoinKeyExpression(context.Join.ProbeKeys, context.ConversionLookup, context.KeyType),
                        context.KeyType,
                        valueType,
                        new ExecutionBlock([
                            new ExecutionLet(probe, ExecutionExpressionConverter.Convert(summary.Probe, output)),
                            new ExecutionAssign(hasMatch, fails),
                        ]),
                        CreateLoopBody(context.Pipeline.Filter, append, output),
                        hasMatch
                    ),
                ])
            )
        );
        return CompleteOutputTableBuild(
            context.Scope,
            [.. context.Sources.Left.Shapes, .. context.Sources.Right.Shapes, resultShape],
            nodes,
            table,
            resultShape,
            context.Pipeline.PostOperations,
            context.Pipeline.Project.IsDistinct
        );
    }
}
