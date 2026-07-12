using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class AggregateRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = node switch
            {
                ExecutionCreateAggregateLibrary library when renderContext.Session != null => [ExecutionCSharpRenderer.RenderCreateAggregateLibrary(library)],
                ExecutionCreateAggregateContext context => renderer.RenderCreateAggregateContext(context, renderContext),
                ExecutionEnsureAggregateGroup ensureGroup => [renderer.RenderEnsureAggregateGroup(ensureGroup, renderContext)],
                ExecutionCreateSingleKeyAggregateContext context => renderer.RenderCreateSingleKeyAggregateContext(context, renderContext),
                ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup => renderer.RenderGetOrAddSingleKeyAggregateGroup(getOrAddGroup, renderContext),
                ExecutionParallelSingleKeyAggregateLoop parallelAggregate => renderer.RenderParallelSingleKeyAggregateLoop(parallelAggregate, renderContext),
                ExecutionCreateValueTupleAggregateContext context => renderer.RenderCreateValueTupleAggregateContext(context, renderContext),
                ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup => renderer.RenderGetOrAddValueTupleAggregateGroup(getOrAddGroup, renderContext),
                ExecutionAggregateSet aggregateSet => [renderer.RenderAggregateSet(aggregateSet)],
                ExecutionAggregateCapturedValueSet capturedValueSet => [renderer.RenderAggregateCapturedValueSet(capturedValueSet)],
                _ => null!
            };

            return statements != null;
        }
    }
}
