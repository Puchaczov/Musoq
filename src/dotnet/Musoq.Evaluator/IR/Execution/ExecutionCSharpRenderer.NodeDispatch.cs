using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderNode(ExecutionNode node)
    {
        var statements = RenderNodeCore(node);

        return RenderOperatorProfiledNode(node, statements);
    }

    private IEnumerable<StatementSyntax> RenderNodeCore(ExecutionNode node)
    {
        if (new TableControlFlowRenderer(this).TryRender(node, out var tableStatements))
            return tableStatements;

        if (new AggregateRenderer(this).TryRender(node, out var aggregateStatements))
            return aggregateStatements;

        if (new JoinRenderer(this).TryRender(node, out var joinStatements))
            return joinStatements;

        if (new WindowRenderer(this).TryRender(node, out var windowStatements))
            return windowStatements;

        throw UnsupportedShape.Of($"Execution node '{node.GetType().Name}'", "the C# backend");
    }
}
