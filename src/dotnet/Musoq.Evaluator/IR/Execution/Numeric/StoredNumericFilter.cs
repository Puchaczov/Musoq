using System.Linq;

namespace Musoq.Evaluator.IR.Execution;

internal abstract record StoredNumericConsumer
{
    internal static StoredNumericConsumer? TryPlan(ExecutionPlan plan, int tableIndex, GeneratedRowShape shape)
    {
        var loop = plan
            .Body.Nodes.OfType<ExecutionForEach>()
            .FirstOrDefault(node => node.Source is ExecutionStoredTableRows rows && rows.TableIndex == tableIndex);
        if (
            loop?.Body.Nodes.LastOrDefault() is not (ExecutionIf or ExecutionAggregateSet)
            || shape.Fields.Count > 8
            || !shape.Fields.All(field => StoredNumericBindings.Supported(field.Type))
            || ExecutionIrAnalysis
                .CollectExpressions<ExecutionStoredTableRows>(plan.Body)
                .Count(read => read.TableIndex == tableIndex) != 1
            || ExecutionIrAnalysis
                .CollectExpressions<ExecutionStoredTable>(plan.Body)
                .Any(read => read.TableIndex == tableIndex)
        )
            return null;
        var bindings = new StoredNumericBindings(shape, loop.Item.Name);
        var result = loop.Body.Nodes[^1] switch
        {
            ExecutionIf { Body.Nodes: [ExecutionAppendRow { Contexts.Count: 0 } append] } conditional =>
                (StoredNumericConsumer?)StoredNumericFilter.FromLoop(loop.Body, conditional, append, bindings),
            ExecutionAggregateSet => StoredNumericAggregates.FromLoop(loop.Body, bindings),
            _ => null,
        };
        return
            result != null && StoredTableColumnarBufferEligibility.HasColumnarProducer(plan, tableIndex, shape, out _)
            ? result
            : null;
    }
}

internal sealed record StoredNumericFilter(NumericPredicate Predicate, ExecutionBlock MatchedBody)
    : StoredNumericConsumer
{
    internal static StoredNumericFilter? FromLoop(
        ExecutionBlock body,
        ExecutionIf conditional,
        ExecutionAppendRow append,
        StoredNumericBindings bindings
    )
    {
        var prefix = body.Nodes.SkipLast(1).ToArray();
        if (!prefix.All(node => node is ExecutionLet let && bindings.Bind(let)))
            return null;
        return
            bindings.Predicate(conditional.Condition) is { } predicate
            && append.Values.All(value => bindings.SafeProjection(value.Value))
            ? new StoredNumericFilter(predicate, new ExecutionBlock([.. prefix, append]))
            : null;
    }
}
