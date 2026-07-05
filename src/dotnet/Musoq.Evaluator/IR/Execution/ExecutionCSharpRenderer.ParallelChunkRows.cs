using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private List<StatementSyntax> CreateParallelAggregateChunkStatements(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string chunkName,
        string groupsName,
        string orderedGroupsName,
        string nullGroupName,
        string cancellationTokenName,
        ExecutionRenderContext context)
    {
        const string indexName = "index";
        const string groupKeyName = "groupKey";

        return
        [
            CreateParallelAggregateChunkRowLoop(
                parallelAggregate,
                chunkName,
                groupsName,
                orderedGroupsName,
                nullGroupName,
                indexName,
                groupKeyName,
                cancellationTokenName,
                context)
        ];
    }

    private ForStatementSyntax CreateParallelAggregateChunkRowLoop(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string chunkName,
        string groupsName,
        string orderedGroupsName,
        string nullGroupName,
        string indexName,
        string groupKeyName,
        string cancellationTokenName,
        ExecutionRenderContext context)
    {
        var aggregateBodyStatements = RenderBlock(parallelAggregate.AggregateBody, context).Statements.ToList();
        var reuseGroupKeyLet = TryGetReusableGroupKeyLet(parallelAggregate, out var groupKeyLet);
        var body = new List<StatementSyntax>
        {
            CreatePeriodicCancellationCheck(indexName, cancellationTokenName),
            CreateLocalDeclaration(
                CreateVariableTypeSyntax(parallelAggregate.Source),
                parallelAggregate.Source.Name,
                CreateElementAccess(SyntaxFactory.IdentifierName(chunkName), SyntaxFactory.IdentifierName(indexName)))
        };

        if (reuseGroupKeyLet)
        {
            body.Add(RenderLet(groupKeyLet!, context));
            aggregateBodyStatements.RemoveAt(0);
        }

        body.Add(CreateLocalDeclaration(
            CreateTypeSyntax(parallelAggregate.KeyType),
            groupKeyName,
            reuseGroupKeyLet
                ? SyntaxFactory.IdentifierName(groupKeyLet!.Variable.Name)
                : RenderExpression(parallelAggregate.Key, context)));

        body.AddRange(CreateParallelAggregateGroupAcquisitionStatements(
            parallelAggregate,
            groupsName,
            orderedGroupsName,
            groupKeyName,
            context,
            nullGroupName));
        body.AddRange(aggregateBodyStatements);

        return SyntaxFactory.ForStatement(StatementEmitter.CreateBlock(body))
            .WithDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(indexName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            SyntaxFactory.Literal(0)))))))
            .WithCondition(SyntaxFactory.BinaryExpression(
                SyntaxKind.LessThanExpression,
                SyntaxFactory.IdentifierName(indexName),
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(chunkName),
                    SyntaxFactory.IdentifierName(nameof(IReadOnlyCollection<>.Count)))))
            .WithIncrementors(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                SyntaxFactory.PostfixUnaryExpression(
                    SyntaxKind.PostIncrementExpression,
                    SyntaxFactory.IdentifierName(indexName))));
    }
}
