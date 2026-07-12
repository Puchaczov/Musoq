using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderParallelSingleKeyAggregateLoop(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExecutionRenderContext context)
    {
        if (IsChunkedParallelSingleKeyAggregate(parallelAggregate))
        {
            return
            [
                SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(parallelAggregate.GroupsToFinalize.Name),
                    CreateParallelSingleKeyAggregateInvocation(
                        parallelAggregate,
                        RenderExpression(parallelAggregate.SourceRows, context),
                        context)))
            ];
        }

        var parallelRowsName = $"{parallelAggregate.GroupsToFinalize.Name}ParallelRows";
        var parallelRowsDeclaration = CreateParallelAggregationRowsDeclaration(parallelAggregate, parallelRowsName, context);
        var parallelAssignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(parallelAggregate.GroupsToFinalize.Name),
            CreateParallelSingleKeyAggregateInvocation(
                parallelAggregate,
                SyntaxFactory.IdentifierName(parallelRowsName),
                context)));

        return
        [
            parallelRowsDeclaration,
            parallelAssignment
        ];
    }
    private LocalDeclarationStatementSyntax CreateParallelAggregationRowsDeclaration(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        string parallelRowsName,
        ExecutionRenderContext context)
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
                SyntaxFactory.Argument(RenderExpression(parallelAggregate.SourceRows, context)),
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

    private InvocationExpressionSyntax CreateParallelSingleKeyAggregateInvocation(
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate,
        ExpressionSyntax parallelRows,
        ExecutionRenderContext context)
    {
        var arguments = new List<ExpressionSyntax>
        {
            parallelRows,
            SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(parallelAggregate.MaxDegreeOfParallelism)),
            SyntaxFactory.IdentifierName("token")
        };

        arguments.AddRange(CollectParallelSingleKeyAggregateCaptures(parallelAggregate)
            .Select(CreateCapturedLocalArgument));

        return SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(
                CreateParallelSingleKeyAggregateFunctionName(parallelAggregate, context)))
            .WithArgumentList(CreateArgumentList(arguments));
    }

}
