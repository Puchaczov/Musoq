using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class JoinRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = node switch
            {
                ExecutionParallelFilterProjectLoop parallelProject when renderContext.Session != null => renderer.RenderParallelFilterProjectLoop(parallelProject, renderContext),
                ExecutionCreateHashPayload createPayload => [renderer.RenderCreateHashPayload(createPayload)],
                ExecutionCreateHash createHash => [renderer.RenderCreateHash(createHash, renderContext)],
                ExecutionHashAdd hashAdd => renderer.RenderHashAdd(hashAdd),
                ExecutionHashProbe hashProbe => renderer.RenderHashProbe(hashProbe, renderContext),
                ExecutionCreateKeySet createKeySet => [renderer.RenderCreateKeySet(createKeySet, renderContext)],
                ExecutionKeySetAdd keySetAdd => renderer.RenderKeySetAdd(keySetAdd),
                ExecutionKeySetProbe keySetProbe => renderer.RenderKeySetProbe(keySetProbe, renderContext),
                ExecutionCreateAsOfIndex createAsOfIndex => [renderer.RenderCreateAsOfIndex(createAsOfIndex, renderContext)],
                ExecutionAsOfProbe asOfProbe => [renderer.RenderAsOfProbe(asOfProbe, renderContext)],
                ExecutionCreateRangeIndex createRangeIndex => [renderer.RenderCreateRangeIndex(createRangeIndex, renderContext)],
                ExecutionRangeProbe rangeProbe => [renderer.RenderRangeProbe(rangeProbe, renderContext)],
                _ => null!
            };

            return statements != null;
        }
    }
}
