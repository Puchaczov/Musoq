using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static InvocationExpressionSyntax CreateFusedIntRankingInvocation(
        ExecutionComputeRankingWindow ranking,
        ExecutionVariable builder,
        ExecutionWindowPartitionSet? sortedPartitions)
    {
        var helperName = ranking.QualifyUpperBound.HasValue
            ? GetTopRankingHelperName(ranking.Function)
            : GetRankingHelperName(ranking.Function);
        var arguments = new List<ExpressionSyntax>
        {
            sortedPartitions == null
                ? CreateBooleanLiteral(ranking.OrderKeys[0].Descending)
                : SyntaxFactory.IdentifierName(sortedPartitions.Variable.Name)
        };

        if (ranking.QualifyUpperBound.HasValue)
            arguments.Add(CreateLongLiteral(ranking.QualifyUpperBound.Value));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(builder.Name),
                    SyntaxFactory.IdentifierName(helperName)))
            .WithArgumentList(CreateArgumentList(arguments.ToArray()));
    }

    private static void AddFusedIntOrderPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionComputeRankingWindow ranking,
        ExecutionVariable builder)
    {
        AddFusedIntOrderPartitionDeclarations(
            statements,
            ranking.Partitions,
            ranking.SortedPartitions,
            builder,
            ranking.OrderKeys[0].Descending);
    }

    private static void AddFusedIntOrderPartitionDeclarations(
        List<StatementSyntax> statements,
        ExecutionWindowPartitionSet? partitions,
        ExecutionWindowPartitionSet? sortedPartitions,
        ExecutionVariable builder,
        bool descending)
    {
        string? createdPartitionVariableName = null;

        if (partitions is { ShouldCreate: true })
        {
            createdPartitionVariableName = partitions.Variable.Name;
            statements.Add(CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                createdPartitionVariableName,
                CreateFusedIntSortedPartitionSetInvocation(builder, descending)));
        }

        if (sortedPartitions is not { ShouldCreate: true })
            return;

        var sortedPartitionVariableName = sortedPartitions.Variable.Name;
        if (string.Equals(createdPartitionVariableName, sortedPartitionVariableName, StringComparison.Ordinal))
            return;

        statements.Add(CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            sortedPartitionVariableName,
            createdPartitionVariableName == null
                ? CreateFusedIntSortedPartitionSetInvocation(builder, descending)
                : SyntaxFactory.IdentifierName(createdPartitionVariableName)));
    }

    private static InvocationExpressionSyntax CreateFusedIntSortedPartitionSetInvocation(
        ExecutionVariable builder,
        bool descending)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(builder.Name),
                    SyntaxFactory.IdentifierName(nameof(WindowIntOrderBuilder.ToSortedPartitionSet))))
            .WithArgumentList(CreateArgumentList(CreateBooleanLiteral(descending)));
    }

    private static string GetRankingHelperName(ExecutionRankingWindowFunction function)
    {
        return function switch
        {
            ExecutionRankingWindowFunction.RowNumber => nameof(WindowFunctionHelpers.ComputeRowNumber),
            ExecutionRankingWindowFunction.Rank => nameof(WindowFunctionHelpers.ComputeRank),
            ExecutionRankingWindowFunction.DenseRank => nameof(WindowFunctionHelpers.ComputeDenseRank),
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };
    }

    private static string GetTopRankingHelperName(ExecutionRankingWindowFunction function)
    {
        return function switch
        {
            ExecutionRankingWindowFunction.RowNumber => nameof(WindowFunctionHelpers.ComputeRowNumberTopN),
            ExecutionRankingWindowFunction.Rank => nameof(WindowFunctionHelpers.ComputeRankTopN),
            ExecutionRankingWindowFunction.DenseRank => nameof(WindowFunctionHelpers.ComputeDenseRankTopN),
            _ => throw new ArgumentOutOfRangeException(nameof(function), function, null)
        };
    }

    private static LiteralExpressionSyntax CreateLongLiteral(long value)
    {
        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(value));
    }
}
