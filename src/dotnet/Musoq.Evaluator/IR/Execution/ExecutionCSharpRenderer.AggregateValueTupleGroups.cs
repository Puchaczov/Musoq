using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> RenderCreateValueTupleAggregateContext(
        ExecutionCreateValueTupleAggregateContext valueTupleContext,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(valueTupleContext.GroupShape, context);
        var statements = new List<StatementSyntax>();

        var rootGroupDeclaration = CreateRootAggregateGroupDeclaration(valueTupleContext.RootGroup, valueTupleContext.GroupPlan, context);
        if (rootGroupDeclaration is not null)
            statements.Add(rootGroupDeclaration);

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            valueTupleContext.GroupsToFinalize.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));

        foreach (var dictionary in valueTupleContext.GroupDictionaries)
        {
            var level = GetAggregateGroupLevel(valueTupleContext.GroupPlan, dictionary.PrefixLength);
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                dictionary.Variable.Name,
                SyntaxFactory.ObjectCreationExpression(CreateValueTupleGroupDictionaryTypeSyntax(
                    valueTupleContext.KeyTypes,
                    dictionary.PrefixLength,
                    CreateAggregateGroupType(level.Shape, context)))
                    .WithArgumentList(SyntaxFactory.ArgumentList())));
        }

        return statements;
    }

    private List<StatementSyntax> RenderGetOrAddValueTupleAggregateGroup(
        ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup,
        ExecutionRenderContext context)
    {
        var statements = new List<StatementSyntax>();

        for (var index = 0; index < getOrAddGroup.Keys.Count; index++)
            statements.Add(CreateLocalDeclaration(
                CreateTypeSyntax(getOrAddGroup.KeyTypes[index]),
                CreateGroupKeyVariableName(index),
                RenderExpression(getOrAddGroup.Keys[index])));

        var ownerGroupNames = new Dictionary<int, string>();

        foreach (var level in getOrAddGroup.GroupPlan.Levels.Where(level =>
                     level.PrefixLength > 0 &&
                     level.PrefixLength < getOrAddGroup.Keys.Count))
        {
            var groupName = CreateValueTuplePrefixGroupVariableName(level.PrefixLength);
            ownerGroupNames[level.PrefixLength] = groupName;
            statements.AddRange(CreateValueTuplePrefixGroupStatements(getOrAddGroup, level, groupName, context));
        }

        statements.AddRange(CreateLeafValueTupleGroupStatements(getOrAddGroup, ownerGroupNames, context));
        return statements;
    }

    private StatementSyntax[] CreateValueTuplePrefixGroupStatements(
        ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup,
        AggregateGroupLevelPlan level,
        string groupName,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(level.Shape, context);
        var dictionary = GetAggregateGroupDictionary(getOrAddGroup.GroupDictionaries, level.PrefixLength);
        var keyExpression = CreateValueTupleKeyExpression(level.PrefixLength);
        var groupCreation = CreateAggregateGroupCreation(
            level.Shape,
            context,
            [],
            CreateAggregateGroupKeyArguments(level.Shape, level.PrefixLength));

        return CreateDictionaryGroupAcquisitionStatements(
            groupType,
            groupName,
            dictionary.Variable.Name,
            keyExpression,
            groupCreation,
            [],
            declareGroupVariable: true);
    }

    private StatementSyntax[] CreateLeafValueTupleGroupStatements(
        ExecutionGetOrAddValueTupleAggregateGroup getOrAddGroup,
        IReadOnlyDictionary<int, string> ownerGroupNames,
        ExecutionRenderContext context)
    {
        var keyCount = getOrAddGroup.Keys.Count;
        var groupType = CreateAggregateGroupType(getOrAddGroup.GroupShape, context);
        var dictionary = GetAggregateGroupDictionary(getOrAddGroup.GroupDictionaries, keyCount);
        var keyExpression = CreateValueTupleKeyExpression(keyCount);
        var groupCreation = CreateAggregateGroupCreation(
            getOrAddGroup.GroupShape,
            context,
            CreateLeafOwnerArguments(
                getOrAddGroup.GroupShape,
                owner => ResolveValueTupleOwnerArgument(owner, getOrAddGroup.RootGroup, ownerGroupNames)),
            CreateAggregateGroupKeyArguments(getOrAddGroup.GroupShape, keyCount));
        var addToFinalize = CreateCollectionAddStatement(
            getOrAddGroup.GroupsToFinalize.Name,
            SyntaxFactory.IdentifierName(CreateGroupRefVariableName(getOrAddGroup.Group.Name)));

        return CreateDictionaryGroupAcquisitionStatements(
            groupType,
            getOrAddGroup.Group.Name,
            dictionary.Variable.Name,
            keyExpression,
            groupCreation,
            [addToFinalize],
            declareGroupVariable: true);
    }

    private StatementSyntax[] CreateGetOrAddSingleKeyValueGroupStatements(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        string keyVariableName,
        ExecutionRenderContext context,
        bool declareGroupVariable)
    {
        var groupCreation = CreateSingleKeyGroupCreation(getOrAddGroup, SyntaxFactory.IdentifierName(keyVariableName), context);
        var addToFinalize = CreateCollectionAddStatement(
            getOrAddGroup.GroupsToFinalize.Name,
            SyntaxFactory.IdentifierName(CreateGroupRefVariableName(getOrAddGroup.Group.Name)));

        return CreateDictionaryGroupAcquisitionStatements(
            CreateAggregateGroupType(getOrAddGroup.GroupShape, context),
            getOrAddGroup.Group.Name,
            getOrAddGroup.Groups.Name,
            SyntaxFactory.IdentifierName(keyVariableName),
            groupCreation,
            [addToFinalize],
            declareGroupVariable);
    }

    private IfStatementSyntax CreateGetOrAddSingleKeyReferenceGroupStatement(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        string keyVariableName,
        ExecutionRenderContext context)
    {
        var nonNullBlock = StatementEmitter.CreateBlock([
            ..CreateGetOrAddSingleKeyValueGroupStatements(
                getOrAddGroup,
                keyVariableName,
                context,
                declareGroupVariable: false)
        ]);

        var nullGroup = getOrAddGroup.NullGroup
            ?? throw new InvalidOperationException("Reference aggregate grouping requires a null-group variable.");
        var assignNullGroup = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(getOrAddGroup.Group.Name),
            SyntaxFactory.IdentifierName(nullGroup.Name)));
        var elseBlock = StatementEmitter.CreateBlock(StatementEmitter.CreateIf(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    SyntaxFactory.IdentifierName(nullGroup.Name),
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                StatementEmitter.CreateBlock(CreateNewSingleKeyNullGroupStatements(
                    getOrAddGroup,
                    nullGroup.Name,
                    SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression),
                    context))), assignNullGroup);

        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.NotEqualsExpression,
                SyntaxFactory.IdentifierName(keyVariableName),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            nonNullBlock,
            SyntaxFactory.ElseClause(elseBlock));
    }

    private StatementSyntax[] CreateNewSingleKeyNullGroupStatements(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        string groupName,
        ExpressionSyntax keyValue,
        ExecutionRenderContext context)
    {
        var groupCreation = CreateSingleKeyGroupCreation(getOrAddGroup, keyValue, context);
        var assignGroup = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(groupName),
            groupCreation));
        var addToFinalize = CreateCollectionAddStatement(
            getOrAddGroup.GroupsToFinalize.Name,
            SyntaxFactory.IdentifierName(groupName));

        return [assignGroup, addToFinalize];
    }

    private ObjectCreationExpressionSyntax CreateSingleKeyGroupCreation(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        ExpressionSyntax keyValue,
        ExecutionRenderContext context)
    {
        return CreateAggregateGroupCreation(
            getOrAddGroup.GroupShape,
            context,
            CreateLeafOwnerArguments(
                getOrAddGroup.GroupShape,
                owner => ResolveSingleKeyOwnerArgument(owner, getOrAddGroup.RootGroup)),
            keyValue);
    }

    private static StatementSyntax[] CreateDictionaryGroupAcquisitionStatements(
        TypeSyntax groupType,
        string groupName,
        string dictionaryName,
        ExpressionSyntax keyExpression,
        ExpressionSyntax groupCreation,
        IReadOnlyList<StatementSyntax> onCreatedStatements,
        bool declareGroupVariable)
    {
        var groupRefName = CreateGroupRefVariableName(groupName);
        var existsName = $"{groupName}Exists";
        var createdStatements = new List<StatementSyntax>
        {
            SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(groupRefName),
                groupCreation))
        };
        createdStatements.AddRange(onCreatedStatements);

        StatementSyntax assignGroup = declareGroupVariable
            ? CreateLocalDeclaration(groupType, groupName, SyntaxFactory.IdentifierName(groupRefName))
            : SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(groupName),
                SyntaxFactory.IdentifierName(groupRefName)));

        return
        [
            CreateDictionaryGroupRefDeclaration(dictionaryName, keyExpression, groupRefName, existsName),
            SyntaxFactory.IfStatement(
                SyntaxFactory.PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    SyntaxFactory.IdentifierName(existsName)),
                StatementEmitter.CreateBlock(createdStatements)),
            assignGroup
        ];
    }

    private static LocalDeclarationStatementSyntax CreateDictionaryGroupRefDeclaration(
        string dictionaryName,
        ExpressionSyntax keyExpression,
        string groupRefName,
        string existsName)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.ParseExpression("System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault"))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(dictionaryName)),
                SyntaxFactory.Argument(keyExpression),
                SyntaxFactory.Argument(SyntaxFactory.DeclarationExpression(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(existsName))))
                    .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
            ])));

        return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(groupRefName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.RefExpression(invocation))))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)));
    }

    private static ExpressionStatementSyntax CreateCollectionAddStatement(
        string collectionName,
        ExpressionSyntax item)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(collectionName),
                    SyntaxFactory.IdentifierName("Add")))
            .WithArgumentList(CreateArgumentList(item)));
    }

    private static string CreateGroupRefVariableName(string groupName)
    {
        return $"{groupName}Ref";
    }
}
