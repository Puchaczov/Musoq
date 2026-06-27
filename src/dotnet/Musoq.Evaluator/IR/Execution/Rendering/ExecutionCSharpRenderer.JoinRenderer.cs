using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class JoinRenderer(ExecutionCSharpRenderer renderer)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = node switch
            {
                ExecutionParallelFilterProjectLoop parallelProject => renderer.RenderParallelFilterProjectLoop(parallelProject),
                ExecutionCreateHashPayload createPayload => [renderer.RenderCreateHashPayload(createPayload)],
                ExecutionCreateHash createHash => [renderer.RenderCreateHash(createHash)],
                ExecutionHashAdd hashAdd => renderer.RenderHashAdd(hashAdd),
                ExecutionHashProbe hashProbe => renderer.RenderHashProbe(hashProbe),
                ExecutionCreateKeySet createKeySet => [renderer.RenderCreateKeySet(createKeySet)],
                ExecutionKeySetAdd keySetAdd => renderer.RenderKeySetAdd(keySetAdd),
                ExecutionKeySetProbe keySetProbe => renderer.RenderKeySetProbe(keySetProbe),
                ExecutionCreateAsOfIndex createAsOfIndex => [renderer.RenderCreateAsOfIndex(createAsOfIndex)],
                ExecutionAsOfProbe asOfProbe => [renderer.RenderAsOfProbe(asOfProbe)],
                ExecutionCreateRangeIndex createRangeIndex => [renderer.RenderCreateRangeIndex(createRangeIndex)],
                ExecutionRangeProbe rangeProbe => [renderer.RenderRangeProbe(rangeProbe)],
                _ => null!
            };

            return statements != null;
        }
    }
}
