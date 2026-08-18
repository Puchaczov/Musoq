using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal sealed class ChunkedLoopBreakRewriter(string breakTarget) : CSharpSyntaxRewriter
{
    private int _nestedBreakableDepth;

    public static IEnumerable<StatementSyntax> Rewrite(
        IEnumerable<StatementSyntax> statements,
        string breakTarget)
    {
        var rewriter = new ChunkedLoopBreakRewriter(breakTarget);
        return statements.Select(statement => (StatementSyntax)rewriter.Visit(statement)!);
    }

    public override SyntaxNode? VisitBreakStatement(BreakStatementSyntax node)
    {
        if (_nestedBreakableDepth > 0)
            return node;

        return SyntaxFactory.GotoStatement(
                SyntaxKind.GotoStatement,
                SyntaxFactory.IdentifierName(breakTarget))
            .WithTriviaFrom(node);
    }

    public override SyntaxNode? VisitForStatement(ForStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitForStatement(node));

    public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitForEachStatement(node));

    public override SyntaxNode? VisitForEachVariableStatement(ForEachVariableStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitForEachVariableStatement(node));

    public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitWhileStatement(node));

    public override SyntaxNode? VisitDoStatement(DoStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitDoStatement(node));

    public override SyntaxNode? VisitSwitchStatement(SwitchStatementSyntax node) =>
        VisitNestedBreakable(() => base.VisitSwitchStatement(node));

    private SyntaxNode? VisitNestedBreakable(Func<SyntaxNode?> visit)
    {
        _nestedBreakableDepth++;
        try
        {
            return visit();
        }
        finally
        {
            _nestedBreakableDepth--;
        }
    }
}
