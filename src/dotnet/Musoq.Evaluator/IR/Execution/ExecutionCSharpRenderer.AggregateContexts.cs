using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private static LocalDeclarationStatementSyntax RenderCreateAggregateLibrary(ExecutionCreateAggregateLibrary library)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            library.Library.Name,
            SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(library.LibraryType))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private LocalDeclarationStatementSyntax? CreateRootAggregateGroupDeclaration(
        ExecutionVariable rootGroup,
        AggregateGroupPlan groupPlan)
    {
        if (groupPlan.LeafShape.Keys.Count == 0)
            return null;

        var rootLevel = groupPlan.Levels.FirstOrDefault(static level => level.IsRoot);
        if (rootLevel is null)
            return null;

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            rootGroup.Name,
            CreateAggregateGroupCreation(
                rootLevel.Shape,
                [],
                CreateAggregateGroupDefaultKeyArguments(rootLevel.Shape)));
    }

    private static ExpressionSyntax[] CreateNoOwnerArguments(AggregateGroupShape shape)
    {
        if (shape.OwnerFields.Count > 0)
            throw new InvalidOperationException($"Aggregate group '{shape.TypeName}' requires owner arguments.");

        return [];
    }

    private static ExpressionSyntax[] CreateLeafOwnerArguments(
        AggregateGroupShape shape,
        Func<AggregateGroupOwnerField, ExpressionSyntax> resolveOwner)
    {
        return shape.OwnerFields
            .Select(resolveOwner)
            .ToArray();
    }

    private static IdentifierNameSyntax ResolveSingleKeyOwnerArgument(
        AggregateGroupOwnerField owner,
        ExecutionVariable rootGroup)
    {
        if (owner.PrefixLength == 0)
            return SyntaxFactory.IdentifierName(rootGroup.Name);

        throw new InvalidOperationException(
            $"Single-key aggregate group '{owner.Shape.TypeName}' cannot use prefix owner {owner.PrefixLength.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static IdentifierNameSyntax ResolveValueTupleOwnerArgument(
        AggregateGroupOwnerField owner,
        ExecutionVariable rootGroup,
        IReadOnlyDictionary<int, string> ownerGroupNames)
    {
        if (owner.PrefixLength == 0)
            return SyntaxFactory.IdentifierName(rootGroup.Name);

        if (ownerGroupNames.TryGetValue(owner.PrefixLength, out var ownerGroupName))
            return SyntaxFactory.IdentifierName(ownerGroupName);

        throw new InvalidOperationException(
            $"Value-tuple aggregate group '{owner.Shape.TypeName}' cannot resolve prefix owner {owner.PrefixLength.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static AggregateGroupLevelPlan GetAggregateGroupLevel(
        AggregateGroupPlan groupPlan,
        int prefixLength)
    {
        return groupPlan.Levels.FirstOrDefault(level => level.PrefixLength == prefixLength)
               ?? throw new InvalidOperationException(
                   $"Aggregate group plan does not contain prefix level {prefixLength.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static AggregateGroupLookup GetAggregateGroupDictionary(
        IReadOnlyList<AggregateGroupLookup> dictionaries,
        int prefixLength)
    {
        return dictionaries.FirstOrDefault(dictionary => dictionary.PrefixLength == prefixLength)
               ?? throw new InvalidOperationException(
                   $"Aggregate group plan does not contain dictionary level {prefixLength.ToString(CultureInfo.InvariantCulture)}.");
    }

    private static string CreateValueTuplePrefixGroupVariableName(int prefixLength)
    {
        return $"levelGroup_{(prefixLength - 1).ToString(CultureInfo.InvariantCulture)}";
    }

    private List<StatementSyntax> RenderCreateAggregateContext(ExecutionCreateAggregateContext context)
    {
        var groupType = CreateAggregateGroupType(context.GroupShape);
        var statements = new List<StatementSyntax>();

        var rootGroupDeclaration = CreateRootAggregateGroupDeclaration(context.RootGroup, context.GroupPlan);
        if (rootGroupDeclaration is not null)
            statements.Add(rootGroupDeclaration);

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            context.Groups.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));
        statements.Add(CreateLocalDeclaration(
            groupType,
            context.CurrentGroup.Name,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        return statements;
    }

    private IfStatementSyntax RenderEnsureAggregateGroup(ExecutionEnsureAggregateGroup ensureGroup)
    {
        var groupCreation = CreateAggregateGroupCreation(
            ensureGroup.GroupShape,
            CreateNoOwnerArguments(ensureGroup.GroupShape));
        var groupAssignment = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName(ensureGroup.CurrentGroup.Name),
                groupCreation));
        var addInvocation = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(ensureGroup.Groups.Name),
                        SyntaxFactory.IdentifierName("Add")))
                .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(ensureGroup.CurrentGroup.Name))));

        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                SyntaxFactory.IdentifierName(ensureGroup.CurrentGroup.Name),
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
            StatementEmitter.CreateBlock(groupAssignment, addInvocation));
    }

    private List<StatementSyntax> RenderCreateSingleKeyAggregateContext(ExecutionCreateSingleKeyAggregateContext context)
    {
        var groupType = CreateAggregateGroupType(context.GroupShape);
        var statements = new List<StatementSyntax>();

        var rootGroupDeclaration = CreateRootAggregateGroupDeclaration(context.RootGroup, context.GroupPlan);
        if (rootGroupDeclaration is not null)
            statements.Add(rootGroupDeclaration);

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            context.GroupsToFinalize.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            context.Groups.Name,
            SyntaxFactory.ObjectCreationExpression(CreateGroupDictionaryTypeSyntax(context.KeyType, groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));

        if (context.NullGroup is not null)
            statements.Add(CreateLocalDeclaration(
                groupType,
                context.NullGroup.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        return statements;
    }

    private List<StatementSyntax> RenderGetOrAddSingleKeyAggregateGroup(ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup)
    {
        const string keyVariableName = "groupKey";
        var groupType = CreateAggregateGroupType(getOrAddGroup.GroupShape);

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(CreateTypeSyntax(getOrAddGroup.KeyType), keyVariableName, RenderExpression(getOrAddGroup.Key))
        };

        if (getOrAddGroup.NullGroup is null)
        {
            statements.AddRange(CreateGetOrAddSingleKeyValueGroupStatements(
                getOrAddGroup,
                keyVariableName,
                declareGroupVariable: true));
            return statements;
        }

        statements.Add(CreateLocalDeclaration(
            groupType,
            getOrAddGroup.Group.Name,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        statements.Add(CreateGetOrAddSingleKeyReferenceGroupStatement(getOrAddGroup, keyVariableName));
        return statements;
    }
}
