using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Bindings;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static AggregateOnlyPipeline? DecomposeRawAggregateOnlyPipeline(PhysicalNode node)
    {
        return node switch
        {
            PhysicalHavingFilterNode { Input: PhysicalAggregateOnlyNode aggregate } having =>
                CreateRawAggregateOnlyPipeline(aggregate, having.Predicate),
            PhysicalAggregateOnlyNode aggregate => CreateRawAggregateOnlyPipeline(aggregate, null),
            _ => null
        };
    }

    private static AggregateOnlyPipeline? CreateRawAggregateOnlyPipeline(
        PhysicalAggregateOnlyNode aggregate,
        IrExpression? havingPredicate)
    {
        var source = DecomposeSourcePipeline(aggregate.Input);
        return source == null
            ? null
            : new AggregateOnlyPipeline(
                CreateAggregateProjection(aggregate),
                aggregate.Bindings,
                source,
                havingPredicate,
                []);
    }

    private static PhysicalProjectNode CreateAggregateProjection(PhysicalAggregateOnlyNode aggregate)
    {
        return new PhysicalProjectNode(CreateAggregateBindingFields(aggregate.Bindings, 0).ToArray(), aggregate);
    }

    private static SingleKeyAggregatePipeline? DecomposeRawSingleKeyAggregatePipeline(PhysicalNode node)
    {
        return node switch
        {
            PhysicalHavingFilterNode { Input: PhysicalSingleKeyAggregateNode aggregate } having =>
                CreateRawSingleKeyAggregatePipeline(aggregate, having.Predicate),
            PhysicalSingleKeyAggregateNode aggregate => CreateRawSingleKeyAggregatePipeline(aggregate, null),
            _ => null
        };
    }

    private static SingleKeyAggregatePipeline? CreateRawSingleKeyAggregatePipeline(
        PhysicalSingleKeyAggregateNode aggregate,
        IrExpression? havingPredicate)
    {
        var source = DecomposeSourcePipeline(aggregate.Input);
        return source == null
            ? null
            : new SingleKeyAggregatePipeline(
                CreateAggregateProjection(aggregate),
                aggregate,
                aggregate.Bindings,
                source,
                havingPredicate,
                aggregate.GroupKey,
                aggregate.GroupKeyName,
                aggregate.GroupKeyType,
                []);
    }

    private static PhysicalProjectNode CreateAggregateProjection(PhysicalSingleKeyAggregateNode aggregate)
    {
        var fields = new List<ProjectedField>(aggregate.Bindings.Length + 1)
        {
            new(aggregate.GroupKeyName, aggregate.GroupKey, 0)
        };

        fields.AddRange(CreateAggregateBindingFields(aggregate.Bindings, 1));

        return new PhysicalProjectNode(fields.ToArray(), aggregate);
    }

    private static ValueTupleAggregatePipeline? DecomposeRawValueTupleAggregatePipeline(PhysicalNode node)
    {
        return node switch
        {
            PhysicalHavingFilterNode { Input: PhysicalValueTupleAggregateNode aggregate } having =>
                CreateRawValueTupleAggregatePipeline(aggregate, having.Predicate),
            PhysicalValueTupleAggregateNode aggregate => CreateRawValueTupleAggregatePipeline(aggregate, null),
            _ => null
        };
    }

    private static ValueTupleAggregatePipeline? CreateRawValueTupleAggregatePipeline(
        PhysicalValueTupleAggregateNode aggregate,
        IrExpression? havingPredicate)
    {
        var source = DecomposeSourcePipeline(aggregate.Input);
        return source == null
            ? null
            : new ValueTupleAggregatePipeline(
                CreateAggregateProjection(aggregate),
                aggregate.Bindings,
                source,
                havingPredicate,
                aggregate.GroupKeys,
                aggregate.GroupKeyNames,
                aggregate.GroupKeyTypes,
                []);
    }

    private static PhysicalProjectNode CreateAggregateProjection(PhysicalValueTupleAggregateNode aggregate)
    {
        return new PhysicalProjectNode(
            CreateGroupedAggregateProjectionFields(aggregate.GroupKeys, aggregate.GroupKeyNames, aggregate.Bindings),
            aggregate);
    }

    private static ProjectedField[] CreateGroupedAggregateProjectionFields(
        IrExpression[] groupKeys,
        string[] groupKeyNames,
        AggregateBinding[] bindings)
    {
        var fields = new List<ProjectedField>(groupKeys.Length + bindings.Length);

        for (var index = 0; index < groupKeys.Length; index++)
            fields.Add(new ProjectedField(groupKeyNames[index], groupKeys[index], index));

        fields.AddRange(CreateAggregateBindingFields(bindings, groupKeys.Length));

        return fields.ToArray();
    }

    private static IEnumerable<ProjectedField> CreateAggregateBindingFields(
        AggregateBinding[] bindings,
        int startIndex)
    {
        for (var index = 0; index < bindings.Length; index++)
        {
            var binding = bindings[index];

            yield return new ProjectedField(
                binding.Identifier,
                new AggregateRef(binding.Identifier, binding.ReturnType),
                startIndex + index);
        }
    }
}
