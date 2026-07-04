using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

internal sealed record AggregateOnlyPipeline(
    PhysicalProjectNode Project,
    AggregateBinding[] Bindings,
    SourcePipeline Source,
    IrExpression? HavingPredicate,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record SingleKeyAggregatePipeline(
    PhysicalProjectNode Project,
    PhysicalSingleKeyAggregateNode Aggregate,
    AggregateBinding[] Bindings,
    SourcePipeline Source,
    IrExpression? HavingPredicate,
    IrExpression GroupKey,
    string GroupKeyName,
    Type GroupKeyType,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record ValueTupleAggregatePipeline(
    PhysicalProjectNode Project,
    AggregateBinding[] Bindings,
    SourcePipeline Source,
    IrExpression? HavingPredicate,
    IrExpression[] GroupKeys,
    string[] GroupKeyNames,
    Type[] GroupKeyTypes,
    IReadOnlyList<PostOperation> PostOperations);

internal sealed record AggregateFinalizationGroupKeys(
    IReadOnlyList<IrExpression> Expressions,
    IReadOnlyList<string> Names,
    IReadOnlyList<Type> Types);

internal sealed record AggregateFinalizationContext(
    ExecutionVariable Group,
    AggregateFinalizationGroupKeys GroupKeys,
    IReadOnlyList<AggregateBinding> Bindings,
    Dictionary<string, AggregateBinding> BindingsByIdentifier,
    IReadOnlyDictionary<string, AggregateCapturedValue> CapturedValues,
    IReadOnlyDictionary<string, AggregateAccumulatorField> TypedAccumulators,
    AggregateGroupShape GroupShape,
    string AggregateKind);

internal sealed record AggregateGroupLowering(
    AggregateGroupPlan Plan,
    IReadOnlyDictionary<string, AggregateAccumulatorField> AccumulatorsByIdentifier,
    IReadOnlyDictionary<string, AggregateCapturedField> CapturedFieldsByName)
{
    public AggregateGroupShape Shape => Plan.LeafShape;
}

internal sealed record AggregateLoweringResourceRequest(
    string ResultTableName,
    string AggregateScopeName,
    IReadOnlyList<AggregateBinding> Bindings,
    ProjectedField[] OutputFields,
    IrExpression? HavingPredicate,
    IReadOnlyList<PostOperation> PostOperations,
    AggregateFinalizationGroupKeys FinalizationGroupKeys,
    ExecutionVariable CurrentGroup,
    ExecutionVariable FinalGroup,
    IReadOnlyDictionary<string, RowShape> SourceLookup,
    string AggregateKind);

internal sealed record AggregateLoweringResources(
    AggregateGroupLowering Group,
    IReadOnlyList<ExecutionNode> LibraryNodes,
    AggregateSetBuildResult SetNodes,
    AggregateGroupValueCaptureBuildResult ValueCapture,
    AggregateFinalizationContext FinalizationContext);

internal sealed record AggregateTableCompletion(
    IReadOnlyList<RowShape> SourceShapes,
    IReadOnlyList<ExecutionNode> SourceSetup,
    AggregateLoweringResources Aggregate,
    ExecutionVariable ResultTable,
    GeneratedRowShape ResultShape,
    ExecutionNode ContextCreation,
    ExecutionNode Accumulation,
    ExecutionVariable GroupsToFinalize,
    ExecutionVariable FinalGroup,
    ExecutionBlock FinalBlock,
    IReadOnlyList<PostOperation> PostOperations,
    bool IsDistinct);

internal sealed record AggregateSetBuildResult(
    bool Supported,
    IReadOnlyList<ExecutionNode> Nodes,
    IReadOnlyDictionary<string, AggregateAccumulatorField> TypedAccumulators,
    string UnsupportedReason)
{
    public static AggregateSetBuildResult Success(
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlyDictionary<string, AggregateAccumulatorField> typedAccumulators)
    {
        return new AggregateSetBuildResult(true, nodes, typedAccumulators, string.Empty);
    }

    public static AggregateSetBuildResult Unsupported(string reason)
    {
        return new AggregateSetBuildResult(
            false,
            [],
            new Dictionary<string, AggregateAccumulatorField>(StringComparer.OrdinalIgnoreCase),
            reason);
    }
}

internal sealed record AggregateCapturedValue(
    string ValueName,
    Type ValueType);

internal sealed record AggregateGroupValueCaptureBuildResult(
    bool Supported,
    IReadOnlyList<ExecutionNode> Nodes,
    IReadOnlyDictionary<string, AggregateCapturedValue> CapturedValues,
    string UnsupportedReason)
{
    public static AggregateGroupValueCaptureBuildResult Success(
        IReadOnlyList<ExecutionNode> nodes,
        IReadOnlyDictionary<string, AggregateCapturedValue> capturedValues)
    {
        return new AggregateGroupValueCaptureBuildResult(true, nodes, capturedValues, string.Empty);
    }

    public static AggregateGroupValueCaptureBuildResult Unsupported(string reason)
    {
        return new AggregateGroupValueCaptureBuildResult(
            false,
            [],
            new Dictionary<string, AggregateCapturedValue>(StringComparer.OrdinalIgnoreCase),
            reason);
    }
}
