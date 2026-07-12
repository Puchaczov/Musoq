using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private MethodDeclarationSyntax CreateParallelSingleKeyAggregateChunkFunction(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        ValidateParallelSingleKeyAggregateShape(parallelAggregate);
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                CreateParallelSingleKeyAggregateChunkFunctionName(parallelAggregate, context))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(CreateParallelSingleKeyAggregateChunkParameterList(parallelAggregate, captures, context))
            .WithBody(StatementEmitter.CreateBlock(CreateParallelAggregateChunkStatements(
                parallelAggregate,
                "chunk",
                "groups",
                "orderedGroups",
                "nullGroup",
                "cancellationToken",
                context)));
    }

    private ClassDeclarationSyntax CreateParallelSingleKeyAggregateChunkWorkerClass(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);
        var fields = CreateParallelAggregateChunkWorkerFields(parallelAggregate, captures, context);
        var members = new List<MemberDeclarationSyntax>();

        members.Add(CreateParallelAggregateChunkWorkerGroupsField(parallelAggregate, context));
        members.Add(CreateParallelAggregateChunkWorkerOrderedGroupsField(parallelAggregate, context));
        members.Add(CreateParallelAggregateChunkWorkerNullGroupField(parallelAggregate, context));
        members.AddRange(fields.Select(CreateParallelAggregateChunkWorkerField));
        members.Add(CreateParallelAggregateChunkWorkerOrderedGroupsProperty(parallelAggregate, context));
        members.Add(CreateParallelAggregateChunkWorkerConstructor(parallelAggregate, fields, context));
        members.Add(CreateParallelAggregateChunkWorkerProcessMethod(parallelAggregate, fields, context));

        return SyntaxFactory.ClassDeclaration(CreateParallelSingleKeyAggregateChunkWorkerTypeName(parallelAggregate, context))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List(members));
    }

    private ParameterListSyntax CreateParallelSingleKeyAggregateChunkParameterList(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter("chunk", CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source))),
            CreateParameter("groups", CreateGroupDictionaryTypeSyntax(parallelAggregate.KeyType, groupType)),
            CreateParameter("orderedGroups", CreateListTypeSyntax(groupType)),
            CreateParameter("nullGroup", groupType)
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword))),
            CreateParameter("cancellationToken", CreateTypeSyntax(typeof(CancellationToken)))
        };

        parameters.AddRange(captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private static FieldDeclarationSyntax CreateParallelAggregateChunkWorkerField(ParallelAggregateWorkerField field)
    {
        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(field.Type)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(field.FieldName))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private FieldDeclarationSyntax CreateParallelAggregateChunkWorkerGroupsField(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var fieldType = CreateGroupDictionaryTypeSyntax(parallelAggregate.KeyType, groupType);

        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(fieldType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("_groups")
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(fieldType)
                                    .WithArgumentList(SyntaxFactory.ArgumentList()))))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private FieldDeclarationSyntax CreateParallelAggregateChunkWorkerOrderedGroupsField(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var fieldType = CreateListTypeSyntax(groupType);

        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(fieldType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("_orderedGroups")
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(fieldType)
                                    .WithArgumentList(SyntaxFactory.ArgumentList()))))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)));
    }

    private FieldDeclarationSyntax CreateParallelAggregateChunkWorkerNullGroupField(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);

        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(groupType)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator("_nullGroup"))))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
    }

    private PropertyDeclarationSyntax CreateParallelAggregateChunkWorkerOrderedGroupsProperty(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.PropertyDeclaration(
                CreateListTypeSyntax(CreateAggregateGroupType(parallelAggregate.GroupShape, context)),
                "OrderedGroups")
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithExpressionBody(SyntaxFactory.ArrowExpressionClause(SyntaxFactory.IdentifierName("_orderedGroups")))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private List<ParallelAggregateWorkerField> CreateParallelAggregateChunkWorkerFields(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal)
        {
            "_groups",
            "_orderedGroups",
            "_nullGroup"
        };
        var fields = new List<ParallelAggregateWorkerField>
        {
            new("cancellationToken", "_cancellationToken", CreateTypeSyntax(typeof(CancellationToken)))
        };
        usedFieldNames.Add("_cancellationToken");

        foreach (var capture in captures)
        {
            var baseFieldName = CreateIdentifierCandidate($"_{capture.Name.TrimStart('@')}", 0);
            var fieldName = CreateUniqueHelperName(baseFieldName, usedFieldNames);
            fields.Add(new ParallelAggregateWorkerField(
                capture.Name,
                fieldName,
                CreateCapturedLocalTypeSyntax(capture)));
        }

        return fields;
    }

    private ConstructorDeclarationSyntax CreateParallelAggregateChunkWorkerConstructor(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<ParallelAggregateWorkerField> fields,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ConstructorDeclaration(CreateParallelSingleKeyAggregateChunkWorkerTypeName(parallelAggregate, context))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                fields.Select(static field => CreateParameter(field.ParameterName, field.Type)))))
            .WithBody(StatementEmitter.CreateBlock(fields.Select(static field =>
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    CreateIdentifierName(field.FieldName),
                    SyntaxFactory.IdentifierName(field.ParameterName))))));
    }

    private MethodDeclarationSyntax CreateParallelAggregateChunkWorkerProcessMethod(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<ParallelAggregateWorkerField> fields,
        ExecutionRenderContext context)
    {
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("chunk")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_groups")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_orderedGroups")),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_nullGroup"))
                .WithRefKindKeyword(SyntaxFactory.Token(SyntaxKind.RefKeyword)),
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName("_cancellationToken"))
        };
        arguments.AddRange(fields.Skip(1)
            .Select(static field => SyntaxFactory.Argument(CreateIdentifierName(field.FieldName))));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "ProcessChunk")
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                CreateParameter("chunk", CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source))))))
            .WithBody(StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(CreateParallelSingleKeyAggregateChunkFunctionName(parallelAggregate, context)))
                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))))));
    }
}
