using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax>? RenderAggregateOperation(ExecutionNode node, ExecutionRenderContext context)
    {
        return node switch
        {
            ExecutionCreateAggregateLibrary library when context.Session != null => [RenderCreateAggregateLibrary(library)],
            ExecutionInitializeAggregateState initialize => RenderInitializeAggregateState(initialize),
            ExecutionCreateAggregateContext aggregateContext => RenderCreateAggregateContext(aggregateContext, context),
            ExecutionEnsureAggregateGroup ensureGroup => [RenderEnsureAggregateGroup(ensureGroup, context)],
            ExecutionCreateSingleKeyAggregateContext singleKeyContext => RenderCreateSingleKeyAggregateContext(singleKeyContext, context),
            ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup => RenderGetOrAddSingleKeyAggregateGroup(getOrAddGroup, context),
            ExecutionParallelSingleKeyAggregateLoop parallelAggregate => RenderParallelSingleKeyAggregateLoop(parallelAggregate, context),
            ExecutionCreateValueTupleAggregateContext tupleContext => RenderCreateValueTupleAggregateContext(tupleContext, context),
            ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup => RenderGetOrAddValueTupleAggregateGroup(getOrAddGroup, context),
            ExecutionAggregateSet aggregateSet => [RenderAggregateSet(aggregateSet, context)],
            ExecutionAggregateCapturedValueSet capturedValueSet => [RenderAggregateCapturedValueSet(capturedValueSet)],
            _ => null
        };
    }
}
