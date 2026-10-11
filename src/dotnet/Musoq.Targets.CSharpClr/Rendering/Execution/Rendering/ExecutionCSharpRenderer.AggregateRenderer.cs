using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class AggregateRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = renderer.RenderAggregateOperation(node, renderContext)!;

            return statements != null;
        }
    }
}
