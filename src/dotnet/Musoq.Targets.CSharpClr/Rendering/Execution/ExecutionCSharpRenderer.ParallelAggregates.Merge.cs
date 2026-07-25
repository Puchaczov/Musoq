using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Targets.CSharpClr.Rendering.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static List<StatementSyntax> CreateParallelAggregateMergeStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        TypeSyntax groupType,
        string shardsName)
    {
        const string mergedGroupsName = "mergedGroups";
        const string groupsToFinalizeName = "groupsToFinalize";
        const string shardName = "shard";
        const string sourceGroupName = "sourceGroup";

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                mergedGroupsName,
                SyntaxFactory.ObjectCreationExpression(CreateGroupDictionaryTypeSyntax(parallelAggregate.KeyType, groupType))
                    .WithArgumentList(SyntaxFactory.ArgumentList())),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                groupsToFinalizeName,
                SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                    .WithArgumentList(SyntaxFactory.ArgumentList()))
        };

        if (CanBeNull(parallelAggregate.KeyType))
        {
            statements.Add(CreateLocalDeclaration(
                groupType,
                CreateParallelNullGroupName(),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        }

        statements.Add(StatementEmitter.CreateForeach(
            shardName,
            SyntaxFactory.IdentifierName(shardsName),
            StatementEmitter.CreateBlock(StatementEmitter.CreateForeach(
                    sourceGroupName,
                    SyntaxFactory.IdentifierName(shardName),
                    StatementEmitter.CreateBlock(CreateParallelAggregateMergeGroupStatements(
                        parallelAggregate,
                        mergedGroupsName,
                        groupsToFinalizeName,
                        sourceGroupName))))));

        statements.Add(SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName(groupsToFinalizeName)));
        return statements;
    }

    private static List<StatementSyntax> CreateParallelAggregateMergeGroupStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string mergedGroupsName,
        string groupsToFinalizeName,
        string sourceGroupName)
    {
        const string groupKeyName = "groupKey";
        var keyField = GetSingleAggregateGroupKey(parallelAggregate.GroupShape);
        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(
                CreateTypeSyntax(keyField.Type),
                groupKeyName,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(sourceGroupName),
                    SyntaxFactory.IdentifierName(keyField.FieldName)))
,CanBeNull(parallelAggregate.KeyType)
            ? SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    SyntaxFactory.IdentifierName(groupKeyName),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                StatementEmitter.CreateBlock(CreateParallelAggregateMergeNonNullGroupStatements(
                    mergedGroupsName,
                    groupsToFinalizeName,
                    sourceGroupName,
                    groupKeyName)),
                SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(CreateParallelAggregateMergeNullGroupStatements(
                    groupsToFinalizeName,
                    sourceGroupName))))
            : StatementEmitter.CreateBlock(CreateParallelAggregateMergeNonNullGroupStatements(
                mergedGroupsName,
                groupsToFinalizeName,
                sourceGroupName,
                groupKeyName))        };

        return statements;
    }

    private static IReadOnlyList<StatementSyntax> CreateParallelAggregateMergeNonNullGroupStatements(
        string mergedGroupsName,
        string groupsToFinalizeName,
        string sourceGroupName,
        string groupKeyName)
    {
        const string mergeGroupName = "mergedGroup";
        var groupRefName = CreateGroupRefVariableName(mergeGroupName);
        var existsName = $"{mergeGroupName}Exists";

        return
        [
            CreateDictionaryGroupRefDeclaration(
                mergedGroupsName,
                SyntaxFactory.IdentifierName(groupKeyName),
                groupRefName,
                existsName),
            SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(existsName)),
                StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(groupRefName),
                        SyntaxFactory.IdentifierName(sourceGroupName))),
                    CreateCollectionAddStatement(groupsToFinalizeName, SyntaxFactory.IdentifierName(sourceGroupName))),
                SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(CreateMergeAggregateGroupsStatement(groupRefName, sourceGroupName))))
        ];
    }

    private static IReadOnlyList<StatementSyntax> CreateParallelAggregateMergeNullGroupStatements(
        string groupsToFinalizeName,
        string sourceGroupName)
    {
        var nullGroupName = CreateParallelNullGroupName();

        return
        [
            SyntaxFactory.IfStatement(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.IdentifierName(nullGroupName),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(nullGroupName),
                        SyntaxFactory.IdentifierName(sourceGroupName))),
                    CreateCollectionAddStatement(groupsToFinalizeName, SyntaxFactory.IdentifierName(sourceGroupName))),
                SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(CreateMergeAggregateGroupsStatement(nullGroupName, sourceGroupName))))
        ];
    }

    private static ExpressionStatementSyntax CreateMergeAggregateGroupsStatement(
        string targetGroupName,
        string sourceGroupName)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(targetGroupName),
                    SyntaxFactory.IdentifierName("MergeFrom")))
            .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(sourceGroupName))));
    }
}
