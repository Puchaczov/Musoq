using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static AggregateOnlyPipeline? DecomposeAggregateOnlyPipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        switch (current)
        {
            case PhysicalProjectNode { Input: PhysicalHavingFilterNode { Input: PhysicalAggregateOnlyNode aggregate } having } project:
                var havingAggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (havingAggregateSource == null)
                    return null;

                return new AggregateOnlyPipeline(project, aggregate.Bindings, havingAggregateSource, having.Predicate, CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalAggregateOnlyNode aggregate } project:
                var aggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (aggregateSource == null)
                    return null;

                return new AggregateOnlyPipeline(project, aggregate.Bindings, aggregateSource, null, CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalHavingFilterNode { Input: PhysicalSingleKeyAggregateNode
                {
                    GroupKey: Literal
                } aggregate } having } project:
                var havingLiteralSource = DecomposeSourcePipeline(aggregate.Input);
                if (havingLiteralSource == null)
                    return null;

                return new AggregateOnlyPipeline(project, aggregate.Bindings, havingLiteralSource, having.Predicate, CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalSingleKeyAggregateNode { GroupKey: Literal } aggregate } project:
                var literalSource = DecomposeSourcePipeline(aggregate.Input);
                if (literalSource == null)
                    return null;

                return new AggregateOnlyPipeline(project, aggregate.Bindings, literalSource, null, CreatePostOperations(operations, project.Fields));
            default:
                return null;
        }
    }

    private static SingleKeyAggregatePipeline? DecomposeSingleKeyAggregatePipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        switch (current)
        {
            case PhysicalProjectNode { Input: PhysicalHavingFilterNode { Input: PhysicalSingleKeyAggregateNode
                {
                    GroupKey: not Literal
                } aggregate } having } project:
                var havingAggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (havingAggregateSource == null)
                    return null;

                return new SingleKeyAggregatePipeline(
                    project,
                    aggregate,
                    aggregate.Bindings,
                    havingAggregateSource,
                    having.Predicate,
                    aggregate.GroupKey,
                    aggregate.GroupKeyName,
                    aggregate.GroupKeyType,
                    CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalSingleKeyAggregateNode { GroupKey: not Literal } aggregate } project:
                var aggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (aggregateSource == null)
                    return null;

                return new SingleKeyAggregatePipeline(
                    project,
                    aggregate,
                    aggregate.Bindings,
                    aggregateSource,
                    null,
                    aggregate.GroupKey,
                    aggregate.GroupKeyName,
                    aggregate.GroupKeyType,
                    CreatePostOperations(operations, project.Fields));
            default:
                return null;
        }
    }

    private static ValueTupleAggregatePipeline? DecomposeValueTupleAggregatePipeline(PhysicalNode node)
    {
        var operations = new List<PostOperation>();
        var current = PeelPostOperations(node, operations);

        switch (current)
        {
            case PhysicalProjectNode { Input: PhysicalHavingFilterNode { Input: PhysicalValueTupleAggregateNode aggregate } having } project:
                var havingAggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (havingAggregateSource == null)
                    return null;

                return new ValueTupleAggregatePipeline(
                    project,
                    aggregate.Bindings,
                    havingAggregateSource,
                    having.Predicate,
                    aggregate.GroupKeys,
                    aggregate.GroupKeyNames,
                    aggregate.GroupKeyTypes,
                    CreatePostOperations(operations, project.Fields));
            case PhysicalProjectNode { Input: PhysicalValueTupleAggregateNode aggregate } project:
                var aggregateSource = DecomposeSourcePipeline(aggregate.Input);
                if (aggregateSource == null)
                    return null;

                return new ValueTupleAggregatePipeline(
                    project,
                    aggregate.Bindings,
                    aggregateSource,
                    null,
                    aggregate.GroupKeys,
                    aggregate.GroupKeyNames,
                    aggregate.GroupKeyTypes,
                    CreatePostOperations(operations, project.Fields));
            default:
                return null;
        }
    }

    private static bool IsAggregateSource(PhysicalNode source)
    {
        return source is PhysicalAggregateOnlyNode
            or PhysicalSingleKeyAggregateNode
            or PhysicalValueTupleAggregateNode
            or PhysicalHavingFilterNode
            {
                Input: PhysicalAggregateOnlyNode
                    or PhysicalSingleKeyAggregateNode
                    or PhysicalValueTupleAggregateNode
            };
    }

    private static string? GetRawAggregateIdentifier(MethodCall methodCall)
    {
        return methodCall.Arguments
            .OfType<Literal>()
            .Select(literal => literal.Value as string)
            .FirstOrDefault(identifier => !string.IsNullOrWhiteSpace(identifier));
    }
}
