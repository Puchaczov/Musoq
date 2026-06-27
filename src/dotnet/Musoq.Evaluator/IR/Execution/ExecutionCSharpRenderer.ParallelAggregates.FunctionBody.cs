using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> CreateParallelSingleKeyAggregateFunctionBody(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        const string rowsName = "rows";
        const string maxDegreeName = "maxDegreeOfParallelism";
        const string cancellationTokenName = "cancellationToken";
        const string workerCountName = "workerCount";
        const string shardsName = "shards";
        const string optionsName = "options";
        const string workerName = "worker";

        var groupType = CreateAggregateGroupType(parallelAggregate.GroupShape);
        var captures = CollectParallelSingleKeyAggregateCaptures(parallelAggregate);
        var body = new List<StatementSyntax>
        {
            CreateReturnEmptyGroupsIfNoRowsStatement(parallelAggregate),
            CreateWorkerCountDeclaration(rowsName, maxDegreeName, workerCountName),
            CreateShardArrayDeclaration(groupType, shardsName, workerCountName),
            CreateParallelAggregateOptionsDeclaration(optionsName, cancellationTokenName, workerCountName),
            CreateParallelAggregateWorkerDeclaration(
                parallelAggregate,
                rowsName,
                workerCountName,
                shardsName,
                cancellationTokenName,
                workerName,
                captures),
            CreateParallelAggregateForStatement(
                workerCountName,
                optionsName,
                workerName)
        };

        body.AddRange(CreateParallelAggregateMergeStatements(
            parallelAggregate,
            groupType,
            shardsName));

        return body;
    }

    private IfStatementSyntax CreateReturnEmptyGroupsIfNoRowsStatement(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return SyntaxFactory.IfStatement(
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression,
                CreateRowsCountExpression("rows"),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
            StatementEmitter.CreateBlock(SyntaxFactory.ReturnStatement(SyntaxFactory.ObjectCreationExpression(
                        CreateListTypeSyntax(CreateAggregateGroupType(parallelAggregate.GroupShape)))
                    .WithArgumentList(SyntaxFactory.ArgumentList()))));
    }

    private static LocalDeclarationStatementSyntax CreateWorkerCountDeclaration(
        string rowsName,
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

        var minExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Math)),
                    SyntaxFactory.IdentifierName(nameof(Math.Min))))
            .WithArgumentList(CreateArgumentList(
                maxExpression,
                CreateRowsCountExpression(rowsName)));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            workerCountName,
            minExpression);
    }

    private static LocalDeclarationStatementSyntax CreateShardArrayDeclaration(
        TypeSyntax groupType,
        string shardsName,
        string workerCountName)
    {
        var shardType = CreateListTypeSyntax(groupType);
        var arrayType = CreateSingleDimensionArrayTypeSyntax(shardType);
        var arrayCreation = SyntaxFactory.ArrayCreationExpression(
            SyntaxFactory.ArrayType(shardType)
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.IdentifierName(workerCountName))))));

        return CreateLocalDeclaration(arrayType, shardsName, arrayCreation);
    }

    private static LocalDeclarationStatementSyntax CreateParallelAggregateOptionsDeclaration(
        string optionsName,
        string cancellationTokenName,
        string workerCountName)
    {
        var optionsCreation = SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(typeof(ParallelOptions)))
            .WithArgumentList(SyntaxFactory.ArgumentList())
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ObjectInitializerExpression,
                SyntaxFactory.SeparatedList(
                new ExpressionSyntax[]
                {
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(nameof(ParallelOptions.CancellationToken)),
                        SyntaxFactory.IdentifierName(cancellationTokenName)),
                    SyntaxFactory.AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactory.IdentifierName(nameof(ParallelOptions.MaxDegreeOfParallelism)),
                        SyntaxFactory.IdentifierName(workerCountName))
                })));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            optionsName,
            optionsCreation);
    }

    private ExpressionStatementSyntax CreateParallelAggregateForStatement(
        string workerCountName,
        string optionsName,
        string workerName)
    {
        var loopInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Parallel)),
                    SyntaxFactory.IdentifierName(nameof(Parallel.For))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0)),
                SyntaxFactory.IdentifierName(workerCountName),
                SyntaxFactory.IdentifierName(optionsName),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(workerName),
                    SyntaxFactory.IdentifierName("Run"))));

        return SyntaxFactory.ExpressionStatement(loopInvocation);
    }
}
