using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Helpers
{
    public static class SyntaxHelper
    {
        public static SyntaxTrivia WhiteSpace => SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");

        public static SyntaxTrivia DoubleQuoteTrivia => SyntaxFactory.SyntaxTrivia(SyntaxKind.DoubleQuoteToken, "\"");

        public static InvocationExpressionSyntax CreateMethodInvocation(string variableName, string methodName)
            => CreateMethodInvocation(variableName, methodName, new List<SyntaxNode>());
        public static InvocationExpressionSyntax CreateMethodInvocation(ExpressionSyntax variableName, string methodName)
            => CreateMethodInvocation(variableName, methodName, new List<SyntaxNode>());

        public static InvocationExpressionSyntax CreateMethodInvocation(string variableName, string methodName,
            IEnumerable<SyntaxNode> arguments)
            => CreateMethodInvocation(SyntaxFactory.IdentifierName(variableName), methodName, arguments);

        public static InvocationExpressionSyntax CreateMethodInvocation(ExpressionSyntax exp, string methodName, IEnumerable<SyntaxNode> arguments)
        {
            return SyntaxFactory
                .InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, 
                        exp,
                        SyntaxFactory.Token(SyntaxKind.DotToken), 
                        SyntaxFactory.IdentifierName(methodName)
                    ),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(arguments)));
        }

        public static ElementAccessExpressionSyntax CreateElementAccess(string objectName, IEnumerable<ArgumentSyntax> arguments)
        {
            return SyntaxFactory.ElementAccessExpression(
                SyntaxFactory.IdentifierName(objectName),
                SyntaxFactory.BracketedArgumentList(
                    new SeparatedSyntaxList<ArgumentSyntax>().AddRange(arguments)));
        }

        public static VariableDeclarationSyntax CreateAssignment(params VariableDeclaratorSyntax[] declarations)
        {
            return SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var").WithTrailingTrivia(WhiteSpace),
                SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>(declarations)));
        }

        public static VariableDeclarationSyntax CreateAssignmentByMethodCall(string variableName, string objectName, string methodName, ArgumentListSyntax args)
        {
            return CreateAssignment(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName), 
                    null,
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(objectName),
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName(methodName)),
                            args)
                        )
                    )
                );
        }

        public static VariableDeclarationSyntax CreateAssignmentByPropertyCall(string variableName, string objectName,
            string methodName)
        {
            return CreateAssignment(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName),
                    null,
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(objectName),
                            SyntaxFactory.Token(SyntaxKind.DotToken),
                            SyntaxFactory.IdentifierName(methodName))
                    )
                )
            );
        }

        public static VariableDeclarationSyntax CreateAssignment(string variableName, ExpressionSyntax expression)
        {
            return CreateAssignment(
                SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName),
                    null,
                    SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.Token(SyntaxKind.EqualsToken),
                        expression
                    )
                )
            );
        }

        public static ArgumentSyntax StringLiteralArgument(string text)
        {
            return
                SyntaxFactory.Argument(
                    StringLiteral(text)
                );
        }

        public static ArgumentSyntax IntLiteralArgument(int value)
        {
            return
                SyntaxFactory.Argument(
                    IntLiteral(value)
                );
        }

        public static LiteralExpressionSyntax StringLiteral(string text)
        {
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.StringLiteralExpression,
                SyntaxFactory.Token(
                    SyntaxFactory.TriviaList(WhiteSpace),
                    SyntaxKind.StringLiteralToken,
                    $"\"{text}\"",
                    "",
                    SyntaxFactory.TriviaList(WhiteSpace))
            );
        }
        public static ArgumentSyntax TypeLiteralArgument(string typeName)
        {
            return SyntaxFactory.Argument(TypeOf(typeName));
        }

        public static LiteralExpressionSyntax IntLiteral(int value)
        {
            return SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(value)
            );
        }

        public static TypeOfExpressionSyntax TypeOf(string typeName)
        {
            return SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName(typeName));
        }

        public static ArrayCreationExpressionSyntax CreateArrayOfObjects(ExpressionSyntax[] expressions)
        {
            return CreateArrayOf(nameof(Object), expressions);
        }

        public static ArrayCreationExpressionSyntax CreateArrayOfObjects(string typeName, ExpressionSyntax[] expressions)
        {
            return CreateArrayOf(typeName, expressions);
        }

        public static ArgumentSyntax CreateArrayOfArgument(string typeName, ExpressionSyntax[] expressions)
        {
            return SyntaxFactory.Argument(CreateArrayOf(typeName, expressions));
        }

        public static ObjectCreationExpressionSyntax CreaateObjectOf(string typeName, ArgumentListSyntax args, InitializerExpressionSyntax initializer = null)
        {
            return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword,
                    SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "))),
                SyntaxFactory.ParseTypeName(typeName),
                args,
                initializer);
        }

        public static ForEachStatementSyntax Foreach(string variable, string source)
        {
            return SyntaxFactory.ForEachStatement(
                SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.IdentifierName("var").WithTrailingTrivia(WhiteSpace),
                SyntaxFactory.Identifier(variable).WithTrailingTrivia(WhiteSpace),
                SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(WhiteSpace),
                SyntaxFactory.IdentifierName(source),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                SyntaxFactory.Block());
        }

        public static ArrayCreationExpressionSyntax CreateArrayOf(string typeName, ExpressionSyntax[] expressions)
        {
            var newKeyword = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword, SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")));
            var syntaxList = new SeparatedSyntaxList<ExpressionSyntax>();

            for (int i = 0; i < expressions.Length; i++)
            {
                syntaxList = syntaxList.Add(expressions[i]);
            }

            var rankSpecifiers = new SyntaxList<ArrayRankSpecifierSyntax>();

            rankSpecifiers = rankSpecifiers.Add(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                    new SeparatedSyntaxList<ExpressionSyntax>
                    {
                        SyntaxFactory.OmittedArraySizeExpression(
                            SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken)
                        )
                    },
                    SyntaxFactory.Token(SyntaxKind.CloseBracketToken)
                )
            );

            return SyntaxFactory.ArrayCreationExpression(
                newKeyword,
                SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(typeName), rankSpecifiers),
                SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, syntaxList));
        }
    }
}
