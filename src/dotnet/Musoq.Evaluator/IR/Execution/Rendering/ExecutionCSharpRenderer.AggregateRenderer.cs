using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class AggregateRenderer(ExecutionCSharpRenderer renderer)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = node switch
            {
                ExecutionCreateAggregateLibrary library => [ExecutionCSharpRenderer.RenderCreateAggregateLibrary(library)],
                ExecutionCreateAggregateContext context => renderer.RenderCreateAggregateContext(context),
                ExecutionEnsureAggregateGroup ensureGroup => [renderer.RenderEnsureAggregateGroup(ensureGroup)],
                ExecutionCreateSingleKeyAggregateContext context => renderer.RenderCreateSingleKeyAggregateContext(context),
                ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup => renderer.RenderGetOrAddSingleKeyAggregateGroup(getOrAddGroup),
                ExecutionParallelSingleKeyAggregateLoop parallelAggregate => renderer.RenderParallelSingleKeyAggregateLoop(parallelAggregate),
                ExecutionCreateValueTupleAggregateContext context => renderer.RenderCreateValueTupleAggregateContext(context),
                ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup => renderer.RenderGetOrAddValueTupleAggregateGroup(getOrAddGroup),
                ExecutionAggregateSet aggregateSet => [renderer.RenderAggregateSet(aggregateSet)],
                ExecutionAggregateCapturedValueSet capturedValueSet => [renderer.RenderAggregateCapturedValueSet(capturedValueSet)],
                _ => null!
            };

            return statements != null;
        }
    }
}
