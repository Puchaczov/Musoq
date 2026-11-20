using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    internal StatementSyntax RenderSerialProjectionLoop(
        TypedProjectionLoop loop,
        ExecutionBlock body,
        ExpressionSyntax sourceRows,
        Func<ExecutionAppendRow, ExpressionSyntax> createOutput,
        ExecutionRenderContext context
    )
    {
        var statements = RenderProjectionBody(
                body,
                row => SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, createOutput(row)),
                context
            )
            .ToList();
        if (!ExecutionRowStreams.IsChunked(loop.SourceRows))
        {
            if (context.Session.EmitChunkLoopCancellationChecks && !context.Session.SkipInitialLoopCancellationCheck)
                statements.Insert(0, QueryEmitter.GenerateCancellationCheck());
            return StatementEmitter.CreateForeach(
                EscapeIdentifier(loop.Source.Name),
                sourceRows,
                StatementEmitter.CreateBlock(statements)
            );
        }

        return ChunkedLoopSyntaxFactory.Create(
            loop.Source,
            sourceRows,
            CreateChunkedLoopBreakTarget(loop.Source, context.Session),
            (access, index) =>
            {
                var items = CreateChunkedLoopBodyPrefix(loop.Source, access, index, context);
                items.AddRange(statements);
                return items;
            }
        );
    }
}
