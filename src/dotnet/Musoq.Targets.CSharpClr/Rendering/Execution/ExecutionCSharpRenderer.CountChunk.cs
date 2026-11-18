using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private BlockSyntax RenderCountChunk(
        ExecutionEnsureAggregateGroup group,
        ExecutionAggregateSet set,
        string input,
        string chunk,
        StatementSyntax body,
        ExecutionRenderContext context
    )
    {
        var length = GeneratedRowNamingPolicy.CreateRendererIdentifierCandidate($"{chunk}Length", 0);
        var state = CreateAggregateAccumulatorAccess(set.Group, set.Accumulator);
        var update = SyntaxFactory.Block();
        if (context.Session.EmitChunkLoopCancellationChecks)
            update = update.AddStatements(QueryEmitter.GenerateCancellationCheck());
        update = update.AddStatements(
            RenderEnsureAggregateGroup(group, context),
            SyntaxFactory.ParseStatement($"{state}.Count = checked({state}.Count + (long){length});")
        );
        return SyntaxFactory.Block(
            CreateLocalDeclaration(SyntaxFactory.IdentifierName("var"), chunk, SyntaxFactory.IdentifierName(input)),
            StatementEmitter.CreateIf(
                SyntaxFactory.ParseExpression(
                    $"global::Musoq.Evaluator.Helpers.ArrayChunkRows.TryGetCount({chunk}, out var {length})"
                ),
                SyntaxFactory.Block(
                    StatementEmitter.CreateIf(SyntaxFactory.ParseExpression($"{length} > 0"), update),
                    SyntaxFactory.ContinueStatement()
                )
            ),
            body
        );
    }
}
