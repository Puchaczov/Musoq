using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.Visitors.CodeGeneration;

/// <summary>
///     Removes redundant parentheses from generated Roslyn syntax trees.
///     Targets common patterns produced by <see cref="Microsoft.CodeAnalysis.Editing.SyntaxGenerator"/>
///     which defensively wraps operands in parentheses.
/// </summary>
internal sealed class RedundantParenthesisRewriter : CSharpSyntaxRewriter
{
    public override SyntaxNode VisitParenthesizedExpression(ParenthesizedExpressionSyntax node)
    {
        var visited = (ParenthesizedExpressionSyntax)base.VisitParenthesizedExpression(node)!;
        var inner = visited.Expression;
        var parent = visited.Parent;

        if (parent is not null && CanRemoveParentheses(inner, parent))
            return inner.WithTriviaFrom(visited);

        return visited;
    }

    private static bool CanRemoveParentheses(ExpressionSyntax inner, SyntaxNode parent)
    {
        // Never remove parens that are the entire statement (e.g., return (expr))
        if (parent is ReturnStatementSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax)
            return IsSimpleExpression(inner);

        // Simple expressions never need parentheses
        if (IsSimpleExpression(inner))
            return true;

        // Double parentheses: ((expr)) → (expr)
        if (inner is ParenthesizedExpressionSyntax)
            return true;

        // Cast expressions don't need outer parentheses in most contexts,
        // but MUST keep them when the parent applies postfix operators (member access, indexing, invocation)
        // because the dot/bracket binds tighter than a cast: ((T)x).Prop ≠ (T)x.Prop
        if (inner is CastExpressionSyntax &&
            parent is not BinaryExpressionSyntax
                and not PrefixUnaryExpressionSyntax
                and not MemberAccessExpressionSyntax
                and not ElementAccessExpressionSyntax
                and not InvocationExpressionSyntax
                and not ConditionalAccessExpressionSyntax)
            return true;

        return false;
    }

    private static bool IsSimpleExpression(ExpressionSyntax expression)
    {
        return expression is
            LiteralExpressionSyntax or
            IdentifierNameSyntax or
            MemberAccessExpressionSyntax or
            InvocationExpressionSyntax or
            ElementAccessExpressionSyntax or
            ThisExpressionSyntax or
            BaseExpressionSyntax or
            TypeOfExpressionSyntax or
            DefaultExpressionSyntax or
            ObjectCreationExpressionSyntax;
    }
}
