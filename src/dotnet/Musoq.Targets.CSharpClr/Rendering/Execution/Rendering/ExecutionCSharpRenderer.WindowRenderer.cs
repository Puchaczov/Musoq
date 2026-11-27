using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed class WindowRenderer(ExecutionCSharpRenderer renderer, ExecutionRenderContext renderContext)
    {
        public bool TryRender(ExecutionNode node, out IEnumerable<StatementSyntax> statements)
        {
            statements = node switch
            {
                ExecutionWindowKernelPlan plan when renderContext.Session != null => renderer.RenderWindowKernelPlan(
                    plan,
                    renderContext
                ),
                ExecutionComputeRankingWindow ranking => renderer.RenderComputeRankingWindow(renderContext, ranking),
                ExecutionComputeOffsetWindow offset => renderer.RenderComputeOffsetWindow(renderContext, offset),
                ExecutionComputePluginWindow plugin => renderer.RenderComputePluginWindow(renderContext, plugin),
                ExecutionWindowAggregateKernel kernel => renderer.RenderWindowAggregateKernel(renderContext, kernel),
                _ => null!,
            };

            return statements != null;
        }
    }
}
