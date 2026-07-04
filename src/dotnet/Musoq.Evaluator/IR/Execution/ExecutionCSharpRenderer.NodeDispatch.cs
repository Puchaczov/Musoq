using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderNode(ExecutionNode node)
    {
        return RenderNode(node, new ExecutionRenderContext(_renderOptions, RenderSession));
    }

    private IEnumerable<StatementSyntax> RenderNode(ExecutionNode node, ExecutionRenderContext context)
    {
        var statements = RenderNodeCore(node, context);

        return RenderOperatorProfiledNode(node, statements, context);
    }

    private IEnumerable<StatementSyntax> RenderNodeCore(ExecutionNode node, ExecutionRenderContext context)
    {
        switch (ExecutionNodeRegistry.GetRendererFamily(node))
        {
            case ExecutionRendererNodeFamily.TableControlFlow:
                if (new TableControlFlowRenderer(this, context).TryRender(node, out var tableStatements))
                    return tableStatements;
                break;

            case ExecutionRendererNodeFamily.Aggregate:
                if (new AggregateRenderer(this, context).TryRender(node, out var aggregateStatements))
                    return aggregateStatements;
                break;

            case ExecutionRendererNodeFamily.Join:
                if (new JoinRenderer(this, context).TryRender(node, out var joinStatements))
                    return joinStatements;
                break;

            case ExecutionRendererNodeFamily.Window:
                if (new WindowRenderer(this, context).TryRender(node, out var windowStatements))
                    return windowStatements;
                break;

            case ExecutionRendererNodeFamily.Index:
                if (new JoinRenderer(this, context).TryRender(node, out var indexStatements))
                    return indexStatements;
                break;
        }

        throw UnsupportedShape.Of($"Execution node '{node.GetType().Name}'", "the C# backend");
    }
}
