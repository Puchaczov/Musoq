using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Processes AccessObjectKeyNode visitor operations for dictionary/key access syntax generation.
///     Extracted from ToCSharpRewriteTreeVisitor to improve maintainability and testability.
/// </summary>
public static class AccessObjectKeyNodeProcessor
{
    /// <summary>
    ///     Processes an AccessObjectKeyNode to generate safe dictionary/key access syntax.
    /// </summary>
    /// <param name="node">The AccessObjectKeyNode to process</param>
    /// <param name="nodes">Stack of syntax nodes for expression building</param>
    /// <returns>ProcessingResult containing the generated expression and required namespace</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when node or nodes is null</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when nodes stack is empty</exception>
    public static ProcessingResult ProcessAccessObjectKeyNode(AccessObjectKeyNode node, Stack<SyntaxNode> nodes)
    {
        ValidateInputs(node, nodes);

        var exp = SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)nodes.Pop());


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
                SyntaxFactory.SeparatedList([
                    SyntaxFactory.Argument(memberAccess),
                    SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(node.Token.Key))),
                    SyntaxFactory.Argument(SyntaxFactory.TypeOfExpression(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword))))
                ])));

        return new ProcessingResult
        {
            Expression = safeAccessCall,
            RequiredNamespace = typeof(SafeArrayAccess).Namespace!
        };
    }

    /// <summary>
    ///     Validates the input parameters for AccessObjectKeyNode processing.
    /// </summary>
    /// <param name="node">The AccessObjectKeyNode to validate</param>
    /// <param name="nodes">The syntax nodes stack to validate</param>
    /// <exception cref="System.ArgumentNullException">Thrown when node or nodes is null</exception>
    /// <exception cref="System.InvalidOperationException">Thrown when nodes stack is empty</exception>
    private static void ValidateInputs(AccessObjectKeyNode node, Stack<SyntaxNode> nodes)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (nodes.Count == 0)
            throw new InvalidOperationException("Nodes stack cannot be empty for AccessObjectKeyNode processing");
    }

    /// <summary>
    ///     Result of processing an AccessObjectKeyNode containing the generated expression and required namespace.
    /// </summary>
    public class ProcessingResult
    {
        public ExpressionSyntax Expression { get; init; } = null!;
        public string RequiredNamespace { get; init; } = string.Empty;
    }
}