using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

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
        AggregateGroupPlan groupPlan,
        ExecutionRenderContext context)
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
                context,
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

    private List<StatementSyntax> RenderCreateAggregateContext(
        ExecutionCreateAggregateContext aggregateContext,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(aggregateContext.GroupShape, context);
        var statements = new List<StatementSyntax>();

        var rootGroupDeclaration = CreateRootAggregateGroupDeclaration(aggregateContext.RootGroup, aggregateContext.GroupPlan, context);
        if (rootGroupDeclaration is not null)
            statements.Add(rootGroupDeclaration);

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            aggregateContext.Groups.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));

        if (aggregateContext.GroupShape.Keys.Count == 0)
        {
            statements.Add(CreateLocalDeclaration(
                groupType,
                aggregateContext.CurrentGroup.Name,
                CreateAggregateGroupCreation(aggregateContext.GroupShape, context, [])));
            statements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(aggregateContext.Groups.Name),
                            SyntaxFactory.IdentifierName("Add")))
                    .WithArgumentList(CreateArgumentList(SyntaxFactory.IdentifierName(aggregateContext.CurrentGroup.Name)))));
        }
        else
        {
            statements.Add(CreateLocalDeclaration(
                groupType,
                aggregateContext.CurrentGroup.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        }

        return statements;
    }

    private IfStatementSyntax RenderEnsureAggregateGroup(
        ExecutionEnsureAggregateGroup ensureGroup,
        ExecutionRenderContext context)
    {
        var groupCreation = CreateAggregateGroupCreation(
            ensureGroup.GroupShape,
            context,
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

    private List<StatementSyntax> RenderCreateSingleKeyAggregateContext(
        ExecutionCreateSingleKeyAggregateContext aggregateContext,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(aggregateContext.GroupShape, context);
        var statements = new List<StatementSyntax>();

        var rootGroupDeclaration = CreateRootAggregateGroupDeclaration(aggregateContext.RootGroup, aggregateContext.GroupPlan, context);
        if (rootGroupDeclaration is not null)
            statements.Add(rootGroupDeclaration);

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            aggregateContext.GroupsToFinalize.Name,
            SyntaxFactory.ObjectCreationExpression(CreateListTypeSyntax(groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));
        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            aggregateContext.Groups.Name,
            SyntaxFactory.ObjectCreationExpression(CreateGroupDictionaryTypeSyntax(aggregateContext.KeyType, groupType))
                .WithArgumentList(SyntaxFactory.ArgumentList())));

        if (aggregateContext.NullGroup is not null)
            statements.Add(CreateLocalDeclaration(
                groupType,
                aggregateContext.NullGroup.Name,
                SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));

        return statements;
    }

    private List<StatementSyntax> RenderGetOrAddSingleKeyAggregateGroup(
        ExecutionGetOrAddSingleKeyAggregateGroup getOrAddGroup,
        ExecutionRenderContext context)
    {
        const string keyVariableName = "groupKey";
        var groupType = CreateAggregateGroupType(getOrAddGroup.GroupShape, context);

        var statements = new List<StatementSyntax>
        {
            CreateLocalDeclaration(CreateTypeSyntax(getOrAddGroup.KeyType), keyVariableName, RenderExpression(getOrAddGroup.Key))
        };

        if (getOrAddGroup.NullGroup is null)
        {
            statements.AddRange(CreateGetOrAddSingleKeyValueGroupStatements(
                getOrAddGroup,
                keyVariableName,
                context,
                declareGroupVariable: true));
            return statements;
        }

        statements.Add(CreateLocalDeclaration(
            groupType,
            getOrAddGroup.Group.Name,
            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)));
        statements.Add(CreateGetOrAddSingleKeyReferenceGroupStatement(getOrAddGroup, keyVariableName, context));
        return statements;
    }
}
