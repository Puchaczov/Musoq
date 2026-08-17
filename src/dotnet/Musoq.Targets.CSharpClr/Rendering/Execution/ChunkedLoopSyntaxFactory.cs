using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ChunkedLoopSyntaxFactory
{
    public static StatementSyntax Create(
        ExecutionVariable item,
        ExpressionSyntax sourceExpression,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        var chunkVariableName = CreateIdentifierCandidate($"{item.Name}Chunk", 0);
        var indexVariableName = CreateIdentifierCandidate($"{item.Name}Index", 0);

        return StatementEmitter.CreateForeach(
            chunkVariableName,
            sourceExpression,
            TryCreateFastPathItemType(item, out var itemType)
                ? CreateChunkFastPathStatement(
                    itemType,
                    chunkVariableName,
                    indexVariableName,
                    createBodyStatements)
                : CreateGenericChunkLoop(
                    chunkVariableName,
                    indexVariableName,
                    createBodyStatements));
    }

    private static bool TryCreateFastPathItemType(ExecutionVariable item, out TypeSyntax itemType)
    {
        if (item.Type.RequireClrType() != typeof(object) || !string.IsNullOrWhiteSpace(item.GeneratedRowTypeName))
        {
            itemType = ExecutionSyntaxFactory.CreateVariableTypeSyntax(item);
            return true;
        }

        itemType = SyntaxFactory.IdentifierName("object");
        return false;
    }

    private static StatementSyntax CreateChunkFastPathStatement(
        TypeSyntax itemType,
        string chunkVariableName,
        string indexVariableName,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        var rowChunkVariableName = CreateIdentifierCandidate($"{chunkVariableName}View", 0);

        return StatementEmitter.CreateBlock(
            StatementEmitter.CreateIf(
                CreateIsPatternExpression(
                    chunkVariableName,
                    CreateRowChunkTypeSyntax(itemType),
                    rowChunkVariableName),
                CreateRowChunkFastPathStatement(
                    itemType,
                    rowChunkVariableName,
                    indexVariableName,
                    createBodyStatements)),
            CreateGenericChunkLoop(
                chunkVariableName,
                indexVariableName,
                createBodyStatements));
    }

    private static StatementSyntax CreateRowChunkFastPathStatement(
        TypeSyntax itemType,
        string rowChunkVariableName,
        string indexVariableName,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        var sourceExpression = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(rowChunkVariableName),
            SyntaxFactory.IdentifierName("Source"));
        var arrayVariableName = CreateIdentifierCandidate($"{rowChunkVariableName}Array", 0);
        var listVariableName = CreateIdentifierCandidate($"{rowChunkVariableName}List", 0);

        return StatementEmitter.CreateBlock(
            StatementEmitter.CreateIf(
                SyntaxFactory.IsPatternExpression(
                    sourceExpression,
                    SyntaxFactory.DeclarationPattern(
                        CreateArrayTypeSyntax(itemType),
                        SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(arrayVariableName)))),
                CreateRowChunkIndexedSourceLoop(
                    rowChunkVariableName,
                    arrayVariableName,
                    indexVariableName,
                    createBodyStatements)),
            StatementEmitter.CreateIf(
                SyntaxFactory.IsPatternExpression(
                    sourceExpression,
                    SyntaxFactory.DeclarationPattern(
                        CreateListTypeSyntax(itemType),
                        SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(listVariableName)))),
                CreateRowChunkIndexedSourceLoop(
                    rowChunkVariableName,
                    listVariableName,
                    indexVariableName,
                    createBodyStatements)));
    }

    private static StatementSyntax CreateRowChunkIndexedSourceLoop(
        string rowChunkVariableName,
        string sourceVariableName,
        string indexVariableName,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        var offsetVariableName = CreateIdentifierCandidate($"{rowChunkVariableName}Offset", 0);
        var offsetDeclaration = ExecutionSyntaxFactory.CreateLocalDeclaration(
            ExecutionSyntaxFactory.CreateTypeSyntax(typeof(int)),
            offsetVariableName,
            CreateMemberAccess(rowChunkVariableName, "Offset"));

        var itemAccess = ExecutionSyntaxFactory.CreateElementAccess(
            SyntaxFactory.IdentifierName(sourceVariableName),
            SyntaxFactory.BinaryExpression(
                SyntaxKind.AddExpression,
                SyntaxFactory.IdentifierName(offsetVariableName),
                SyntaxFactory.IdentifierName(indexVariableName)));

        return StatementEmitter.CreateBlock(
            offsetDeclaration,
            CreateChunkIndexLoop(
                indexVariableName,
                CreateMemberAccess(rowChunkVariableName, "Count"),
                StatementEmitter.CreateBlock(createBodyStatements(itemAccess, indexVariableName))),
            SyntaxFactory.ContinueStatement());
    }

    private static StatementSyntax CreateGenericChunkLoop(
        string chunkVariableName,
        string indexVariableName,
        Func<ExpressionSyntax, string, List<StatementSyntax>> createBodyStatements)
    {
        return CreateChunkIndexLoop(
            indexVariableName,
            CreateMemberAccess(chunkVariableName, "Count"),
            StatementEmitter.CreateBlock(createBodyStatements(
                ExecutionSyntaxFactory.CreateElementAccess(
                    SyntaxFactory.IdentifierName(chunkVariableName),
                    SyntaxFactory.IdentifierName(indexVariableName)),
                indexVariableName)));
    }

    private static TypeSyntax CreateArrayTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.ParseTypeName($"{itemType}[]");
    }

    private static TypeSyntax CreateRowChunkTypeSyntax(TypeSyntax itemType)
    {
        return SyntaxFactory.ParseTypeName($"global::Musoq.Schema.DataSources.RowChunk<{itemType}>");
    }

    private static ExpressionSyntax CreateMemberAccess(string variableName, string memberName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.IdentifierName(memberName));
    }

    private static ExpressionSyntax CreateIsPatternExpression(
        string variableName,
        TypeSyntax type,
        string designationName)
    {
        return SyntaxFactory.IsPatternExpression(
            SyntaxFactory.IdentifierName(variableName),
            SyntaxFactory.DeclarationPattern(
                type,
                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(designationName))));
    }

    private static ForStatementSyntax CreateChunkIndexLoop(
        string indexVariableName,
        ExpressionSyntax countExpression,
        StatementSyntax body)
    {
        var countVariableName = CreateIdentifierCandidate($"{indexVariableName}Count", 0);

        return SyntaxFactory.ForStatement(body)
            .WithDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)),
                    SyntaxFactory.SeparatedList(
                    [
                        SyntaxFactory.VariableDeclarator(indexVariableName)
                            .WithInitializer(
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(0)))),
                        SyntaxFactory.VariableDeclarator(countVariableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(countExpression))
                    ])))
            .WithCondition(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.LessThanExpression,
                    SyntaxFactory.IdentifierName(indexVariableName),
                    SyntaxFactory.IdentifierName(countVariableName)))
            .WithIncrementors(
                SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                    SyntaxFactory.PrefixUnaryExpression(
                        SyntaxKind.PreIncrementExpression,
                        SyntaxFactory.IdentifierName(indexVariableName))));
    }

    private static string CreateIdentifierCandidate(string value, int disambiguator)
    {
        return GeneratedRowNamingPolicy.CreateRendererIdentifierCandidate(value, disambiguator);
    }
}
