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
using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Visitors.Helpers;

/// <summary>
///     Helper class for processing SelectNode visitor operations.
///     Handles variable creation, context processing, and block syntax generation.
/// </summary>
public static class SelectNodeProcessor
{
    /// <summary>
    ///     Processes a SelectNode and generates the corresponding block syntax with variable declarations
    ///     and table operations.
    /// </summary>
    /// <param name="node">The SelectNode to process</param>
    /// <param name="nodes">Stack of syntax nodes for expression processing</param>
    /// <param name="scope">The current scope containing metadata</param>
    /// <param name="type">The current method access type</param>
    /// <returns>A BlockSyntax containing the generated select statements</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when method access type is unsupported</exception>
    public static BlockSyntax ProcessSelectNode(
        SelectNode node,
        Stack<SyntaxNode> nodes,
        Scope scope,
        MethodAccessType type,
        string rowClassName = null)
    {
        if (node == null)
            throw new ArgumentNullException(nameof(node));
        if (nodes == null)
            throw new ArgumentNullException(nameof(nodes));
        if (scope == null)
            throw new ArgumentNullException(nameof(scope));

        var scoreTable = scope[MetaAttributes.SelectIntoVariableName];

        var syntaxList = new ExpressionSyntax[node.Fields.Length];

        for (var i = 0; i < node.Fields.Length; i++)
            syntaxList[node.Fields.Length - 1 - i] = (ExpressionSyntax)nodes.Pop();

        var contexts = scope[MetaAttributes.Contexts].Split(',');
        var contextsExpressions = new List<ArgumentSyntax>();

        foreach (var context in contexts)
        {
            var rowVariableName = GetRowVariableName(type, context);

            contextsExpressions.Add(
                SyntaxFactory.Argument(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(rowVariableName),
                        SyntaxFactory.IdentifierName($"{nameof(IObjectResolver.Contexts)}"))));
        }

        if (rowClassName != null)
        {
            var args = new List<ArgumentSyntax>();
            foreach (var expr in syntaxList)
                args.Add(SyntaxFactory.Argument(expr));

            args.AddRange(contextsExpressions);

            var invocation = SyntaxHelper.CreateMethodInvocation(
                scoreTable,
                nameof(Table.Add),
                [
                    SyntaxFactory.Argument(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.ParseTypeName(rowClassName),
                            SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(args)),
                            null)
                    )
                ]);

            return SyntaxFactory.Block(SyntaxFactory.ExpressionStatement(invocation));
        }
        else
        {
            var variableNameKeyword = SyntaxFactory.Identifier(SyntaxTriviaList.Empty, "select",
                SyntaxTriviaList.Create(SyntaxHelper.WhiteSpace));

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

            var args = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(
                    SyntaxFactory.IdentifierName(variableNameKeyword.Text))
            };
            args.AddRange(contextsExpressions);

            var invocation = SyntaxHelper.CreateMethodInvocation(
                scoreTable,
                nameof(Table.Add),
                [
                    SyntaxFactory.Argument(
                        SyntaxFactory.ObjectCreationExpression(
                            SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxHelper.WhiteSpace),
                            SyntaxHelper.ObjectsRowTypeSyntax,
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SeparatedList(args.ToArray())
                            ),
                            SyntaxFactory.InitializerExpression(SyntaxKind.ComplexElementInitializerExpression))
                    )
                ]);

            var a1 = SyntaxFactory.LocalDeclarationStatement(variableDeclaration)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
            var a2 = SyntaxFactory.ExpressionStatement(invocation)
                .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

            return SyntaxFactory.Block(a1, a2);
        }
    }

    private static string GetRowVariableName(MethodAccessType type, string context)
    {
        return type switch
        {
            MethodAccessType.TransformingQuery => $"{context}Row",
            MethodAccessType.ResultQuery or MethodAccessType.CaseWhen => "score",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported method access type")
        };
    }
}
