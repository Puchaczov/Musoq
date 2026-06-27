using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;
using Musoq.Evaluator.IR.Planning;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record JoinSource(
        PhysicalNode Node,
        RowShape Shape,
        ExecutionVariable Variable,
        List<ExecutionNode> Setup,
        ExecutionExpression Rows,
        IReadOnlyList<RowShape> Shapes,
        int SchemaSourceCount,
        bool CanReuseSetupAcrossApplyRows = false,
        GeneratedRowShape? GeneratedRowShape = null,
        ExecutionVariable? OrdinalityVariable = null,
        FusedCteHashBuildSource? FusedHashBuild = null,
        FusedHashPayload? FusedHashPayload = null);

    private sealed record FusedHashPayload(
        HashPayloadShape Shape,
        IReadOnlyList<ExecutionRowValue> Values);

    private sealed record SingleKeyAggregateExecutionSource(
        IReadOnlyDictionary<string, RowShape> Lookup,
        IReadOnlyList<RowShape> Shapes,
        IReadOnlyList<ExecutionNode> Setup,
        Func<ExecutionBlock, ExecutionSourceLoop> CreateLoop,
        ExecutionVariable? ParallelSource = null,
        ExecutionExpression? ParallelRows = null);

    private sealed record SingleKeyAggregateExecutionSourceBuildResult(
        bool Supported,
        SingleKeyAggregateExecutionSource Source,
        string UnsupportedReason)
    {
        public static SingleKeyAggregateExecutionSourceBuildResult Success(SingleKeyAggregateExecutionSource source)
        {
            return new SingleKeyAggregateExecutionSourceBuildResult(true, source, string.Empty);
        }

        public static SingleKeyAggregateExecutionSourceBuildResult Unsupported(string reason)
        {
            return new SingleKeyAggregateExecutionSourceBuildResult(
                false,
                new SingleKeyAggregateExecutionSource(
                    new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                    [],
                    [],
                    _ => new ExecutionForEach(
                        new ExecutionVariable(string.Empty, typeof(object)),
                        new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                        ExecutionBlock.Empty)),
                reason);
        }
    }

    private sealed record ApplyChainSource(
        IReadOnlyList<JoinSource> Sources,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        IReadOnlyList<RowShape> Shapes);

    private sealed record ApplyChainPhysicalSource(
        PhysicalNode Source,
        bool WithOrdinality);

    private sealed record ApplyChainBuildResult(
        bool Supported,
        ApplyChainSource Chain,
        string UnsupportedReason)
    {
        public static ApplyChainBuildResult Success(ApplyChainSource chain)
        {
            return new ApplyChainBuildResult(true, chain, string.Empty);
        }

        public static ApplyChainBuildResult Unsupported(string reason)
        {
            return new ApplyChainBuildResult(
                false,
                new ApplyChainSource(
                    [],
                    new Dictionary<string, RowShape>(StringComparer.OrdinalIgnoreCase),
                    []),
                reason);
        }
    }

    private sealed record JoinSources(JoinSource Left, JoinSource Right);

    private sealed record OuterNestedLoopSides(JoinSource Outer, JoinSource Inner);

    private sealed record HashJoinSides(JoinSource Build, JoinSource Probe);

    private sealed record HashJoinBuildContext(
        PhysicalHashJoinNode Join,
        SupportedPipeline Pipeline,
        JoinSources Sources,
        HashJoinSides Sides,
        IReadOnlyDictionary<string, RowShape> SourceLookup,
        IReadOnlyDictionary<string, RowShape> ConversionLookup,
        Type KeyType,
        ExecutionVariable Hash,
        ExecutionVariable Matches,
        string ResultTableName,
        string ResultShapeName,
        CteSidecarIndexSpec? CteSidecarIndex = null);

    private readonly record struct JoinKeyExpressions(
        IrExpression Left,
        IrExpression Right);

    private readonly record struct AsOfJoinPredicateParts(
        JoinKeyExpressions[] EqualityKeys,
        IrExpression LeftInequalityKey,
        IrExpression RightInequalityKey,
        BinaryOpKind ComparisonKind);

    private readonly record struct NormalizedAsOfJoinKey(
        IrExpression Left,
        IrExpression Right,
        BinaryOpKind Kind);

    private sealed record AsOfProbeBuildResult(
        bool Supported,
        GeneratedRowShape ResultShape,
        ExecutionAsOfProbe Probe,
        string UnsupportedReason)
    {
        public static AsOfProbeBuildResult Success(
            GeneratedRowShape resultShape,
            ExecutionAsOfProbe probe)
        {
            return new AsOfProbeBuildResult(true, resultShape, probe, string.Empty);
        }

        public static AsOfProbeBuildResult Unsupported(string reason)
        {
            return new AsOfProbeBuildResult(
                false,
                new GeneratedRowShape(string.Empty, []),
                new ExecutionAsOfProbe(
                    new ExecutionVariable(string.Empty, typeof(object)),
                    new ExecutionVariable(string.Empty, typeof(object)),
                    new ExecutionVariableRead(new ExecutionVariable(string.Empty, typeof(object))),
                    [],
                    new ExecutionLiteral(null, typeof(object)),
                    new ExecutionLiteral(null, typeof(object)),
                    BinaryOpKind.Equal,
                    ExecutionBlock.Empty),
                reason);
        }
    }
}
