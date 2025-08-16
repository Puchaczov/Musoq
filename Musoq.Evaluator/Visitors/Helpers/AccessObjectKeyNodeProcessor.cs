using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processor for AccessObjectKeyNode that generates safe dictionary/key access syntax using SafeArrayAccess.GetIndexedElement.
/// </summary>
public static class AccessObjectKeyNodeProcessor
{
    /// <summary>
    /// Result of processing an AccessObjectKeyNode.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// The generated expression syntax for safe dictionary/key access.
        /// </summary>
        public required ExpressionSyntax Expression { get; init; }
        
        /// <summary>
        /// The namespace that should be added for the SafeArrayAccess helper.
        /// </summary>
        public required string RequiredNamespace { get; init; }
    }

    /// <summary>
    /// Processes an AccessObjectKeyNode to generate safe dictionary/key access syntax.
    /// </summary>
    /// <param name="node">The AccessObjectKeyNode to process.</param>
    /// <param name="nodes">Stack of syntax nodes for popping the expression.</param>
    /// <returns>ProcessResult containing the generated expression and required namespace.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when node or nodes is null.</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when nodes stack is empty.</exception>
    public static ProcessResult ProcessAccessObjectKeyNode(AccessObjectKeyNode node, Stack<Microsoft.CodeAnalysis.SyntaxNode> nodes)
    {
        if (node == null)
            throw new System.ArgumentNullException(nameof(node));
        
        if (nodes == null)
            throw new System.ArgumentNullException(nameof(nodes));
        
        if (nodes.Count == 0)
            throw new System.InvalidOperationException("Nodes stack cannot be empty when processing AccessObjectKeyNode.");

        var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax) nodes.Pop());

        // Generate safe dictionary/key access using SafeArrayAccess.GetIndexedElement
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            exp, 
            SyntaxFactory.IdentifierName(node.Name));

        var safeAccessCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(nameof(SafeArrayAccess)),
                SyntaxFactory.IdentifierName(nameof(SafeArrayAccess.GetIndexedElement))))
            .WithArgumentList(SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(new[]
                {
                    SyntaxFactory.Argument(memberAccess),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(node.Token.Key))),
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
                })));

        return new ProcessResult
        {
            Expression = safeAccessCall,
            RequiredNamespace = typeof(SafeArrayAccess).Namespace ?? "Musoq.Evaluator.Helpers"
        };
    }
}