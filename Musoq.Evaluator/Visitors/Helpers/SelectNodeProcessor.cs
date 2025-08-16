using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Resources;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;
using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
/// Processor for SelectNode that generates variable creation, context handling, and expression building.
/// </summary>
public static class SelectNodeProcessor
{
    /// <summary>
    /// Result of processing a SelectNode.
    /// </summary>
    public sealed class ProcessResult
    {
        /// <summary>
        /// The generated block syntax for the select statement.
        /// </summary>
        public required BlockSyntax SelectBlock { get; init; }
    }

    /// <summary>
    /// Processes a SelectNode to generate select block syntax with variable creation and context handling.
    /// </summary>
    /// <param name="node">The SelectNode to process.</param>
    /// <param name="nodes">Stack of syntax nodes for popping expressions.</param>
    /// <param name="scope">The scope containing metadata and variable information.</param>
    /// <param name="methodAccessType">The current method access type for determining row variable names.</param>
    /// <returns>ProcessResult containing the generated select block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when nodes stack doesn't have enough expressions.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when methodAccessType is not recognized.</exception>
    public static ProcessResult ProcessSelectNode(SelectNode node, Stack<SyntaxNode> nodes, Scope scope, MethodAccessType methodAccessType)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        if (nodes.Count < node.Fields.Length)
            throw new InvalidOperationException($"Nodes stack must contain at least {node.Fields.Length} expressions for SelectNode processing.");

        var scoreTable = scope[MetaAttributes.SelectIntoVariableName];

        // Create variable declaration for select
        var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select",
            SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));
        
        var syntaxList = new ExpressionSyntax[node.Fields.Length];
        for (var i = 0; i < node.Fields.Length; i++)
            syntaxList[node.Fields.Length - 1 - i] = (ExpressionSyntax) nodes.Pop();

        var array = SyntaxHelper.CreateArrayOfObjects(syntaxList.ToArray());
        var equalsClause = SyntaxFactory.EqualsValueClause(
            SyntaxFactory.Token(SyntaxKind.EqualsToken).WithTrailingTrivia(SyntaxHelper.WhiteSpace), array);

        var variableDecl = SyntaxFactory.VariableDeclarator(variableNameKeyword, null, equalsClause);
        var list = SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>
        {
            variableDecl
        });

        var variableDeclaration =
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var").WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                list);

        // Handle contexts and create expressions
        var contexts = scope[MetaAttributes.Contexts].Split(',');
        var contextsExpressions = CreateContextExpressions(variableNameKeyword, contexts, methodAccessType);

        // Create invocation for Table.Add with ObjectsRow
        var invocation = SyntaxHelper.CreateMethodInvocation(
            scoreTable,
            nameof(Table.Add),
            [
                SyntaxFactory.Argument(
                    SyntaxFactory.ObjectCreationExpression(
                        SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                        SyntaxFactory.ParseTypeName(nameof(ObjectsRow)),
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SeparatedList(
                                contextsExpressions.ToArray())
                        ),
                        SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                )
            ]);

        // Create block statements
        var a1 = SyntaxFactory.LocalDeclarationStatement(variableDeclaration)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
        var a2 = SyntaxFactory.ExpressionStatement(invocation)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

        var selectBlock = SyntaxFactory.Block(a1, a2);

        return new ProcessResult
        {
            SelectBlock = selectBlock
        };
    }

    /// <summary>
    /// Creates context expressions for the given contexts and method access type.
    /// </summary>
    private static List<ArgumentSyntax> CreateContextExpressions(SyntaxToken variableNameKeyword, string[] contexts, MethodAccessType methodAccessType)
    {
        var contextsExpressions = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(
                SyntaxFactory.IdentifierName(variableNameKeyword.Text))
        };

        foreach (var context in contexts)
        {
            var rowVariableName = GetRowVariableName(context, methodAccessType);

            contextsExpressions.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(rowVariableName),
                        SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}"))));
        }

        return contextsExpressions;
    }

    /// <summary>
    /// Determines the row variable name based on context and method access type.
    /// </summary>
    private static string GetRowVariableName(string context, MethodAccessType methodAccessType)
    {
        return methodAccessType switch
        {
            MethodAccessType.TransformingQuery => $"{context}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => throw new ArgumentOutOfRangeException(nameof(methodAccessType), methodAccessType, "Unknown method access type")
        };
    }
}