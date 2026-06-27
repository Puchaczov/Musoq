using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderParallelSingleKeyAggregateLoop(ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        var parallelRowsName = $"{parallelAggregate.GroupsToFinalize.Name}ParallelRows";
        var parallelRowsDeclaration = CreateParallelAggregationRowsDeclaration(parallelAggregate, parallelRowsName);
        var condition = CreateParallelAggregationRowsCondition(parallelAggregate, parallelRowsName);
        var parallelAssignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(parallelAggregate.GroupsToFinalize.Name),
            CreateParallelSingleKeyAggregateInvocation(parallelAggregate, parallelRowsName)));
        var serialStatement = CreateSerialSingleKeyAggregateInvocation(parallelAggregate);

        return
        [
            parallelRowsDeclaration,
            SyntaxFactory.IfStatement(
                condition,
                StatementEmitter.CreateBlock(parallelAssignment),
                SyntaxFactory.ElseClause(StatementEmitter.CreateBlock(serialStatement)))
        ];
    }
    private LocalDeclarationStatementSyntax CreateParallelAggregationRowsDeclaration(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string parallelRowsName)
    {
        var initializer = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.GetParallelAggregationRowsOrEmpty))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            CreateVariableTypeSyntax(parallelAggregate.Source))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(RenderExpression(parallelAggregate.SourceRows)),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelAggregate.Threshold)))
            ])));

        return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.VariableDeclarator(parallelRowsName)
                    .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))));
    }

    private BinaryExpressionSyntax CreateParallelAggregationRowsCondition(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string parallelRowsName)
    {
        return SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            SyntaxFactory.BinaryExpression(
                SyntaxKind.GreaterThanExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(parallelRowsName),
                    SyntaxFactory.IdentifierName(nameof(IReadOnlyCollection<>.Count))),
                SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))),
            CreateParallelAggregationCardinalityCheck(parallelAggregate, parallelRowsName));
    }

    private InvocationExpressionSyntax CreateParallelAggregationCardinalityCheck(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string parallelRowsName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.ShouldUseParallelSingleKeyAggregation))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            CreateVariableTypeSyntax(parallelAggregate.Source),
                            CreateTypeSyntax(parallelAggregate.KeyType)
                        ])))))
            .WithArgumentList(CreateArgumentList(
                SyntaxFactory.IdentifierName(parallelRowsName),
                CreateParallelAggregateKeySelector(parallelAggregate),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelAggregate.CardinalitySampleSize)),
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(parallelAggregate.MaxDistinctSample))));
    }

    private InvocationExpressionSyntax CreateParallelSingleKeyAggregateInvocation(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string parallelRowsName)
    {
        var arguments = new List<ExpressionSyntax>
        {
            SyntaxFactory.IdentifierName(parallelRowsName),
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(parallelAggregate.MaxDegreeOfParallelism)),
            SyntaxFactory.IdentifierName("token")
        };

        arguments.AddRange(CollectParallelSingleKeyAggregateCaptures(parallelAggregate)
            .Select(CreateCapturedLocalArgument));

        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(
                CreateParallelSingleKeyAggregateFunctionName(parallelAggregate)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

    private ParenthesizedLambdaExpressionSyntax CreateParallelAggregateKeySelector(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(RenderExpression(parallelAggregate.Key))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(parallelAggregate.Source.Name))))));
    }
}
