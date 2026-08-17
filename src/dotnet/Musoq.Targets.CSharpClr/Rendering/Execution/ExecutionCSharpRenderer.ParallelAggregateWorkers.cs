using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private sealed record ParallelAggregateWorkerField(string ParameterName, string FieldName, TypeSyntax Type);

    private MethodDeclarationSyntax CreateParallelSingleKeyAggregateFunction(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        ValidateParallelSingleKeyAggregateShape(parallelAggregate);
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);

        return SyntaxFactory.MethodDeclaration(
                CreateListTypeSyntax(CreateAggregateGroupType(parallelAggregate.GroupShape, context)),
                CreateParallelSingleKeyAggregateFunctionName(parallelAggregate, context))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(CreateParallelSingleKeyAggregateParameterList(parallelAggregate, captures))
            .WithBody(StatementEmitter.CreateBlock(CreateParallelSingleKeyAggregateFunctionBody(parallelAggregate, context)));
    }

    private MethodDeclarationSyntax CreateParallelSingleKeyAggregateShardFunction(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        ValidateParallelSingleKeyAggregateShape(parallelAggregate);
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                CreateParallelSingleKeyAggregateShardFunctionName(parallelAggregate, context))
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
            .WithParameterList(CreateParallelSingleKeyAggregateShardParameterList(parallelAggregate, captures, context))
            .WithBody(StatementEmitter.CreateBlock(CreateParallelAggregateShardStatements(
                parallelAggregate,
                "rows",
                "workerCount",
                "shards",
                "shardIndex",
                "cancellationToken",
                context)));
    }

    private ClassDeclarationSyntax CreateParallelSingleKeyAggregateWorkerClass(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);
        var fields = CreateParallelAggregateWorkerFields(parallelAggregate, captures, context);
        var members = new List<MemberDeclarationSyntax>();

        members.AddRange(fields.Select(static field => SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(field.Type)
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(field.FieldName))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))));
        members.Add(CreateParallelAggregateWorkerConstructor(parallelAggregate, fields, context));
        members.Add(CreateParallelAggregateWorkerRunMethod(parallelAggregate, fields, context));

        return SyntaxFactory.ClassDeclaration(CreateParallelSingleKeyAggregateWorkerTypeName(parallelAggregate, context))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
            .WithMembers(SyntaxFactory.List(members));
    }

    private static ParameterListSyntax CreateParallelSingleKeyAggregateParameterList(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures)
    {
        var rowsType = IsChunkedParallelSingleKeyAggregate(parallelAggregate)
            ? CreateChunkedRowsTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source))
            : CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source));
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter("rows", rowsType),
            CreateParameter("maxDegreeOfParallelism", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))),
            CreateParameter("cancellationToken", CreateTypeSyntax(typeof(CancellationToken)))
        };

        parameters.AddRange(captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private ParameterListSyntax CreateParallelSingleKeyAggregateShardParameterList(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var parameters = new List<ParameterSyntax>
        {
            CreateParameter("rows", CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source))),
            CreateParameter("workerCount", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))),
            CreateParameter("shards", CreateSingleDimensionArrayTypeSyntax(CreateListTypeSyntax(groupType))),
            CreateParameter("cancellationToken", CreateTypeSyntax(typeof(CancellationToken))),
            CreateParameter("shardIndex", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
        };

        parameters.AddRange(captures.Select(CreateCapturedLocalParameter));

        return SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters));
    }

    private List<ParallelAggregateWorkerField> CreateParallelAggregateWorkerFields(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var usedFieldNames = new HashSet<string>(StringComparer.Ordinal);
        var fields = new List<ParallelAggregateWorkerField>();

        void AddField(string parameterName, string fieldName, TypeSyntax type)
        {
            usedFieldNames.Add(fieldName);
            fields.Add(new ParallelAggregateWorkerField(parameterName, fieldName, type));
        }

        AddField("rows", "_rows", CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source)));
        AddField("workerCount", "_workerCount", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        AddField("shards", "_shards", CreateSingleDimensionArrayTypeSyntax(CreateListTypeSyntax(groupType)));
        AddField("cancellationToken", "_cancellationToken", CreateTypeSyntax(typeof(CancellationToken)));

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

    private ConstructorDeclarationSyntax CreateParallelAggregateWorkerConstructor(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<ParallelAggregateWorkerField> fields,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.ConstructorDeclaration(CreateParallelSingleKeyAggregateWorkerTypeName(parallelAggregate, context))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(
                fields.Select(static field => CreateParameter(field.ParameterName, field.Type)))))
            .WithBody(StatementEmitter.CreateBlock(fields.Select(static field =>
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    CreateIdentifierName(field.FieldName),
                    SyntaxFactory.IdentifierName(field.ParameterName))))));
    }

    private MethodDeclarationSyntax CreateParallelAggregateWorkerRunMethod(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        IReadOnlyList<ParallelAggregateWorkerField> fields,
        ExecutionRenderContext context)
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "Run")
            .WithAttributeLists(SyntaxFactory.SingletonList(CreateAggressiveInliningAttribute()))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                CreateParameter("shardIndex", SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))))))
            .WithBody(StatementEmitter.CreateBlock(SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName(CreateParallelSingleKeyAggregateShardFunctionName(parallelAggregate, context)))
                    .WithArgumentList(CreateArgumentList(CreateParallelAggregateShardArguments(fields))))));
    }

    private static List<ExpressionSyntax> CreateParallelAggregateWorkerConstructorArguments(
        string rowsName,
        string workerCountName,
        string shardsName,
        string cancellationTokenName,
        IReadOnlyList<CapturedLocal> captures)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(rowsName),
            SyntaxFactory.IdentifierName(workerCountName),
            SyntaxFactory.IdentifierName(shardsName),
            SyntaxFactory.IdentifierName(cancellationTokenName)
        };

        arguments.AddRange(captures.Select(CreateCapturedLocalArgument));
        return arguments;
    }

    private static List<ExpressionSyntax> CreateParallelAggregateShardArguments(
        IReadOnlyList<ParallelAggregateWorkerField> fields)
    {
        var arguments = fields.Take(4)
            .Select(static field => (ExpressionSyntax)CreateIdentifierName(field.FieldName))
            .ToList();

        arguments.Add(SyntaxFactory.IdentifierName("shardIndex"));
        arguments.AddRange(fields.Skip(4).Select(static field => CreateIdentifierName(field.FieldName)));
        return arguments;
    }

    private static AttributeListSyntax CreateAggressiveInliningAttribute()
    {
        return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
            SyntaxFactory.Attribute(SyntaxFactory.ParseName("System.Runtime.CompilerServices.MethodImpl"))
                .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.AttributeArgument(SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.ParseName("System.Runtime.CompilerServices.MethodImplOptions"),
                        SyntaxFactory.IdentifierName("AggressiveInlining"))))))));
    }
}
