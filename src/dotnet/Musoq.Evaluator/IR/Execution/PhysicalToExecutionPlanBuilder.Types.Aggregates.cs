using System.Collections.Generic;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private sealed record AggregateOnlyPipeline(
        PhysicalProjectNode Project,
        AggregateBinding[] Bindings,
        SourcePipeline Source,
        IrExpression? HavingPredicate,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record SingleKeyAggregatePipeline(
        PhysicalProjectNode Project,
        PhysicalSingleKeyAggregateNode Aggregate,
        AggregateBinding[] Bindings,
        SourcePipeline Source,
        IrExpression? HavingPredicate,
        IrExpression GroupKey,
        string GroupKeyName,
        Type GroupKeyType,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record ValueTupleAggregatePipeline(
        PhysicalProjectNode Project,
        AggregateBinding[] Bindings,
        SourcePipeline Source,
        IrExpression? HavingPredicate,
        IrExpression[] GroupKeys,
        string[] GroupKeyNames,
        Type[] GroupKeyTypes,
        IReadOnlyList<PostOperation> PostOperations);

    private sealed record AggregateFinalizationGroupKeys(
        IReadOnlyList<IrExpression> Expressions,
        IReadOnlyList<string> Names,
        IReadOnlyList<Type> Types);

    private sealed record AggregateFinalizationContext(
        ExecutionVariable Group,
        AggregateFinalizationGroupKeys GroupKeys,
        IReadOnlyList<AggregateBinding> Bindings,
        Dictionary<string, AggregateBinding> BindingsByIdentifier,
        IReadOnlyDictionary<string, AggregateCapturedValue> CapturedValues,
        IReadOnlyDictionary<string, AggregateAccumulatorField> TypedAccumulators,
        AggregateGroupShape GroupShape,
        string AggregateKind);

    private sealed record AggregateGroupLowering(
        AggregateGroupPlan Plan,
        IReadOnlyDictionary<string, AggregateAccumulatorField> AccumulatorsByIdentifier,
        IReadOnlyDictionary<string, AggregateCapturedField> CapturedFieldsByName)
    {
        public AggregateGroupShape Shape => Plan.LeafShape;
    }
}
