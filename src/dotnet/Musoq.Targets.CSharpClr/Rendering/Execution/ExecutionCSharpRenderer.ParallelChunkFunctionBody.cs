using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> CreateParallelSingleKeyAggregateChunkedFunctionBody(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        const string rowsName = "rows";
        const string maxDegreeName = "maxDegreeOfParallelism";
        const string cancellationTokenName = "cancellationToken";
        const string workerCountName = "workerCount";
        const string shardsName = "shards";
        const string optionsName = "options";

        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape, context);
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);
        var body = new List<StatementSyntax>
        {
            CreateChunkedWorkerCountDeclaration(maxDegreeName, workerCountName),
            CreateConcurrentShardQueueDeclaration(groupType, shardsName),
            CreateParallelAggregateOptionsDeclaration(optionsName, cancellationTokenName, workerCountName),
            CreateParallelAggregateChunkedForEachStatement(
                parallelAggregate,
                rowsName,
                optionsName,
                shardsName,
                cancellationTokenName,
                captures,
                context)
        };

        body.AddRange(CreateParallelAggregateMergeStatements(
            parallelAggregate,
            groupType,
            shardsName));

        return body;
    }

    private static LocalDeclarationStatementSyntax CreateChunkedWorkerCountDeclaration(
        string maxDegreeName,
        string workerCountName)
    {
        var maxExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Math)),
                    SyntaxFactory.IdentifierName(nameof(Math.Max))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
                SyntaxFactory.IdentifierName(maxDegreeName)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            workerCountName,
            maxExpression);
    }

    private static LocalDeclarationStatementSyntax CreateConcurrentShardQueueDeclaration(
        TypeSyntax groupType,
        string shardsName)
    {
        var shardType = CreateListTypeSyntax(groupType);
        var queueType = SyntaxFactory.ParseTypeName(
            $"global::System.Collections.Concurrent.ConcurrentQueue<{shardType.ToFullString()}>");

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            shardsName,
            SyntaxFactory.ObjectCreationExpression(queueType)
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private StatementSyntax CreateParallelAggregateChunkedForEachStatement(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string rowsName,
        string optionsName,
        string shardsName,
        string cancellationTokenName,
        IReadOnlyList<CapturedLocal> captures,
        ExecutionRenderContext context)
    {
        var rowType = CreateVariableTypeSyntax(parallelAggregate.Source).ToFullString();
        var chunkType = CreateReadOnlyListTypeSyntax(CreateVariableTypeSyntax(parallelAggregate.Source)).ToFullString();
        var workerType = CreateParallelSingleKeyAggregateChunkWorkerTypeName(parallelAggregate, context);
        var captureArguments = captures.Count == 0
            ? string.Empty
            : ", " + string.Join(", ", captures.Select(static capture => capture.Name));

        return SyntaxFactory.ParseStatement($$"""
            Parallel.ForEach<{{chunkType}}, {{workerType}}>(
                {{rowsName}},
                {{optionsName}},
                () => new {{workerType}}({{cancellationTokenName}}{{captureArguments}}),
                (chunk, _, worker) =>
                {
                    worker.ProcessChunk(chunk ?? Array.Empty<{{rowType}}>());
                    return worker;
                },
                worker =>
                {
                    if (worker.OrderedGroups.Count != 0)
                    {
                        {{shardsName}}.Enqueue(worker.OrderedGroups);
                    }
                });
            """);
    }
}
