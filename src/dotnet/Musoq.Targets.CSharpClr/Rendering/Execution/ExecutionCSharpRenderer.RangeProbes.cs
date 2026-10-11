using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private StatementSyntax RenderRangeProbe(
        ExecutionRangeProbe rangeProbe,
        ExecutionRenderContext context)
    {
        var loop = StatementEmitter.CreateForeach(
            EscapeIdentifier(rangeProbe.Match.Name),
            CreateRangeIndexFindInvocation(rangeProbe, context),
            RenderBlock(rangeProbe.Body, context));
        if (rangeProbe.MatchFound == null)
            return loop;

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                CreateTypeSyntax(typeof(bool)),
                rangeProbe.MatchFound.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)),
            loop
        };
        if (rangeProbe.NoMatchBody is { Nodes.Count: > 0 } noMatchBody)
        {
            statements.Add(StatementEmitter.CreateIf(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(rangeProbe.MatchFound.Name)),
                RenderBlock(noMatchBody, context)));
        }

        return SyntaxFactory.Block(statements);
    }
}
