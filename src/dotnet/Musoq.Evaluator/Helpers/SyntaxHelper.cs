using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Helpers;

public static class SyntaxHelper
{
    private static readonly ConcurrentDictionary<string, TypeSyntax> TypeSyntaxCache = new();

    public static readonly TypeSyntax ObjectTypeSyntax = SyntaxFactory.ParseTypeName("object");
    public static readonly TypeSyntax RowTypeSyntax = SyntaxFactory.ParseTypeName("Row");
    public static readonly TypeSyntax ObjectsRowTypeSyntax = SyntaxFactory.ParseTypeName("ObjectsRow");

    public static readonly TypeSyntax IndexOutOfRangeExceptionTypeSyntax =
        SyntaxFactory.ParseTypeName("IndexOutOfRangeException");

    public static readonly TypeSyntax IObjectResolverTypeSyntax =
        SyntaxFactory.ParseTypeName("Musoq.Schema.DataSources.IObjectResolver");

    public static readonly TypeSyntax RowConcreteTypeSyntax = SyntaxFactory.ParseTypeName("Musoq.Evaluator.Tables.Row");

    public static readonly TypeSyntax ListOfIObjectResolverTypeSyntax =
        SyntaxFactory.ParseTypeName("System.Collections.Generic.List<Musoq.Schema.DataSources.IObjectResolver>");

    public static readonly ArrayTypeSyntax ObjectArrayTypeSyntax = SyntaxFactory.ArrayType(
        ObjectTypeSyntax,
        SyntaxFactory.SingletonList(
            SyntaxFactory.ArrayRankSpecifier(
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.OmittedArraySizeExpression()))));

    public static SyntaxTrivia WhiteSpace => SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ");

    public static TypeSyntax GetCachedTypeSyntax(string typeName)
    {
        return TypeSyntaxCache.GetOrAdd(typeName, static t => SyntaxFactory.ParseTypeName(t));
    }

    public static InvocationExpressionSyntax CreateMethodInvocation(string variableName, string methodName,
        IEnumerable<ArgumentSyntax> arguments)
    {
        return CreateMethodInvocation(SyntaxFactory.IdentifierName(variableName), methodName, arguments);
    }

    public static VariableDeclarationSyntax CreateAssignmentByMethodCall(string variableName, string objectName,
        string methodName, ArgumentListSyntax args)
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

    public static ArgumentSyntax TypeLiteralArgument(string typeName)
    {
        return SyntaxFactory.Argument(TypeOf(typeName));
    }

    public static ArrayCreationExpressionSyntax CreateArrayOfObjects(ExpressionSyntax[] expressions)
    {
        return CreateArrayOf(nameof(Object), expressions);
    }

    public static ArrayCreationExpressionSyntax CreateArrayOfObjects(string typeName,
        ExpressionSyntax[] expressions)
    {
        return CreateArrayOf(typeName, expressions);
    }

    public static ObjectCreationExpressionSyntax CreateObjectOf(string typeName, ArgumentListSyntax args,
        InitializerExpressionSyntax initializer = null)
    {
        return CreateObjectOf(GetCachedTypeSyntax(typeName), args, initializer);
    }

    private static ObjectCreationExpressionSyntax CreateObjectOf(TypeSyntax type,
        ArgumentListSyntax args, InitializerExpressionSyntax initializer = null)
    {
        return SyntaxFactory.ObjectCreationExpression(
            SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword,
                SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "))),
            type,
            args,
            initializer);
    }

    public static ForEachStatementSyntax Foreach(string variable, string source, BlockSyntax block,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields)
    {
        ExpressionSyntax orderByExpression = SyntaxFactory.IdentifierName(source);

        if (orderByFields.Length == 0) return CreateForEachStatement(variable, block, orderByExpression);

        var sourceTable = source.Replace(".Rows", string.Empty);

        orderByExpression =
            CreateOrderByExpression(
                variable,
                orderByFields,
                sourceTable,
                orderByFields[0].Field.Order == Order.Ascending ? "OrderBy" : "OrderByDescending",
                0);

        for (var index = 1; index < orderByFields.Length; index++)
        {
            var fieldSyntaxTuple = orderByFields[index];
            orderByExpression = CreateThenByExpression(
                variable,
                orderByFields,
                orderByExpression,
                fieldSyntaxTuple.Field.Order == Order.Ascending ? "ThenBy" : "ThenByDescending",
                index
            );
        }

        return CreateForEachStatement(variable, block, orderByExpression);
    }

    public static StatementSyntax ParallelForeach(string variable, string source, BlockSyntax block)
    {
        return CreateParallelForEachStatement(SyntaxFactory.IdentifierName(source), variable, block);
    }

    private static StatementSyntax CreateParallelForEachStatement(
        ExpressionSyntax collection,
        string variable,
        BlockSyntax block)
    {
        var parameterList = SyntaxFactory.ParameterList(
            SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(
                    SyntaxFactory.Identifier(variable)
                )
            )
        );

        var lambda = SyntaxFactory.ParenthesizedLambdaExpression(
            parameterList,
            block
        );

        var parallelForEachInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Parallel"),
                    SyntaxFactory.IdentifierName("ForEach")
                )
            )
            .WithArgumentList(
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        new[]
                        {
                            SyntaxFactory.Argument(collection),
                            SyntaxFactory.Argument(lambda)
                        }
                    )
                )
            );

        return SyntaxFactory.TryStatement(
            SyntaxFactory.Block(
                SyntaxFactory.ExpressionStatement(parallelForEachInvocation)
            ),
            SyntaxFactory.SingletonList(
                SyntaxFactory.CatchClause()
                    .WithDeclaration(
                        SyntaxFactory.CatchDeclaration(
                            SyntaxFactory.IdentifierName("AggregateException"),
                            SyntaxFactory.Identifier("ex")
                        )
                    )
                    .WithBlock(
                        SyntaxFactory.Block(
                            SyntaxFactory.ThrowStatement(
                                SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("ex"),
                                            SyntaxFactory.IdentifierName("InnerExceptions")
                                        ),
                                        SyntaxFactory.IdentifierName("First")
                                    )
                                )
                            )
                        )
                    )
            ),
            null
        );
    }

    public static ArrayCreationExpressionSyntax CreateArrayOf(string typeName, ExpressionSyntax[] expressions,
        int ranksAmount = 1)
    {
        var newKeyword = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword,
            SyntaxTriviaList.Create(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " ")));
        var syntaxList = SyntaxFactory.SeparatedList(expressions);

        var rankSpecifiers = new SyntaxList<ArrayRankSpecifierSyntax>();

        for (var i = 0; i < ranksAmount; i++)
            rankSpecifiers = rankSpecifiers.Add(
                SyntaxFactory.ArrayRankSpecifier(
                    SyntaxFactory.Token(SyntaxKind.OpenBracketToken),
                    [
                        SyntaxFactory.OmittedArraySizeExpression(
                            SyntaxFactory.Token(SyntaxKind.OmittedArraySizeExpressionToken)
                        )
                    ],
                    SyntaxFactory.Token(SyntaxKind.CloseBracketToken)
                )
            );

        return SyntaxFactory.ArrayCreationExpression(
            newKeyword,
            SyntaxFactory.ArrayType(SyntaxFactory.IdentifierName(typeName), rankSpecifiers),
            SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, syntaxList));
    }

    private static InvocationExpressionSyntax CreateOrderByExpression(string variable,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields, string sourceTable, string methodName,
        int index)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(methodName)
        ).WithArgumentList(
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Argument(
                            SyntaxFactory.IdentifierName(sourceTable)),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(
                            SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier(variable)))
                                .WithExpressionBody(
                                    orderByFields[index].Syntax))
                    }))
        );
    }

    private static LiteralExpressionSyntax StringLiteral(string text)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            SyntaxFactory.Token(
                SyntaxFactory.TriviaList(WhiteSpace),
                SyntaxKind.StringLiteralToken,
                $"\"{text}\"",
                text,
                SyntaxFactory.TriviaList(WhiteSpace))
        );
    }

    private static LiteralExpressionSyntax IntLiteral(int value)
    {
        return SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value)
        );
    }

    private static TypeOfExpressionSyntax TypeOf(string typeName)
    {
        return SyntaxFactory.TypeOfExpression(SyntaxFactory.IdentifierName(typeName));
    }

    private static InvocationExpressionSyntax CreateMethodInvocation(ExpressionSyntax exp, string methodName,
        IEnumerable<ArgumentSyntax> arguments)
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

    private static VariableDeclarationSyntax CreateAssignment(params VariableDeclaratorSyntax[] declarations)
    {
        return SyntaxFactory.VariableDeclaration(
            SyntaxFactory.IdentifierName("var").WithTrailingTrivia(WhiteSpace),
            SyntaxFactory.SeparatedList(new List<VariableDeclaratorSyntax>(declarations)));
    }

    private static InvocationExpressionSyntax CreateThenByExpression(string variable,
        (FieldOrderedNode Field, ExpressionSyntax Syntax)[] orderByFields, ExpressionSyntax orderByExpression,
        string methodName, int index)
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.IdentifierName(methodName)
        ).WithArgumentList(
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList<ArgumentSyntax>(
                    new SyntaxNodeOrToken[]
                    {
                        SyntaxFactory.Argument(orderByExpression),
                        SyntaxFactory.Token(SyntaxKind.CommaToken),
                        SyntaxFactory.Argument(
                            SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier(variable)))
                                .WithExpressionBody(
                                    orderByFields[index].Syntax))
                    }))
        );
    }

    private static ForEachStatementSyntax CreateForEachStatement(string variable, BlockSyntax block,
        ExpressionSyntax orderByExpression)
    {
        return SyntaxFactory.ForEachStatement(
            SyntaxFactory.Token(SyntaxKind.ForEachKeyword),
            SyntaxFactory.Token(SyntaxKind.OpenParenToken),
            SyntaxFactory.IdentifierName("var").WithTrailingTrivia(WhiteSpace),
            SyntaxFactory.Identifier(variable).WithTrailingTrivia(WhiteSpace),
            SyntaxFactory.Token(SyntaxKind.InKeyword).WithTrailingTrivia(WhiteSpace),
            orderByExpression,
            SyntaxFactory.Token(SyntaxKind.CloseParenToken),
            block);
    }

    /// <summary>
    ///     Creates an empty ISchemaColumn array expression: Array.Empty&lt;ISchemaColumn&gt;()
    /// </summary>
    public static InvocationExpressionSyntax CreateEmptyColumnArray()
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Array"),
                SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Empty"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName("ISchemaColumn"))))));
    }
}
