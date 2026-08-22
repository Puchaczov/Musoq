using System.Collections.Generic;
using Musoq.Evaluator.IR.Planning;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution.Lowering.Ctes;

internal sealed record CteSupportedPipeline(
    PhysicalProjectNode Project,
    PhysicalNode Source,
    PhysicalFilterNode? Filter,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record JoinSource(
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
    FusedHashPayload? FusedHashPayload = null)
{
    public IReadOnlyList<ApplyPredicateMovementPlan> ApplyPredicateMovementPlans { get; init; } = [];

    public IReadOnlyList<ExecutionNode> ApplyGuardNodes { get; init; } = [];

    public IReadOnlyList<ApplyPredicateMovementPlan> LoweredApplyPredicateMovementPlans { get; init; } = [];
}

internal sealed record FusedHashPayload(
    HashPayloadShape Shape,
    IReadOnlyList<ExecutionRowValue> Values);

internal sealed record JoinSources(JoinSource Left, JoinSource Right);

internal sealed record HashJoinSides(JoinSource Build, JoinSource Probe);

internal sealed record HashJoinBuildContext(
    PhysicalHashJoinNode Join,
    CteSupportedPipeline Pipeline,
    JoinSources Sources,
    HashJoinSides Sides,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    IReadOnlyDictionary<string, RowShape> ConversionLookup,
    Type KeyType,
    ExecutionVariable Hash,
    ExecutionVariable Matches,
    string ResultTableName,
    string ResultShapeName,
    LoweringScope Scope,
    CteSidecarIndexSpec? CteSidecarIndex = null,
    bool UsesRecursiveInvariantHash = false);
