using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

namespace Musoq.Evaluator.IR.CodeGeneration;

public sealed partial class CSharpRenderer
{
    private enum FinalProjectionInvocationKind
    {
        TypedValuesSerial,
        TypedValuesParallel,
        TypedChunkedValuesParallel,
        TableRowsSerial,
        TableRowsParallel,
        TableChunkedRowsParallel
    }

    private sealed record FinalProjectionInvocationSpec(
        FinalProjectionInvocationKind Kind,
        TypeSyntax SourceType,
        TypeSyntax ResultType,
        string RowsName,
        ExpressionSyntax Predicate,
        ExpressionSyntax Projection,
        int MaxDegreeOfParallelism = 0);

    private static InvocationExpressionSyntax CreateQueryRowsShardInvocation(
        string methodName,
        ExpressionSyntax projectionInvocation)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(QueryRows)),
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(projectionInvocation))));
    }

    private static InvocationExpressionSyntax CreateFinalProjectionInvocation(FinalProjectionInvocationSpec spec)
    {
        var memberAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(GetProjectionHelperName(spec.Kind)),
            SyntaxFactory.GenericName(GetProjectionMethodName(spec.Kind))
                .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                [
                    spec.SourceType,
                    spec.ResultType
                ]))));
        var arguments = new List<ArgumentSyntax>
        {
            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(spec.RowsName))
        };

        if (IsParallelProjection(spec.Kind))
        {
            arguments.Add(SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal(spec.MaxDegreeOfParallelism))));
        }

        arguments.Add(SyntaxFactory.Argument(spec.Predicate));
        arguments.Add(SyntaxFactory.Argument(spec.Projection));
        arguments.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("token")));

        return SyntaxFactory.InvocationExpression(memberAccess)
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)));
    }

    private static string GetProjectionHelperName(FinalProjectionInvocationKind kind)
    {
        return kind switch
        {
            FinalProjectionInvocationKind.TypedValuesSerial or FinalProjectionInvocationKind.TypedValuesParallel
                or FinalProjectionInvocationKind.TypedChunkedValuesParallel
                => nameof(TypedProjectionRows),
            FinalProjectionInvocationKind.TableRowsSerial => nameof(TableProjectionRows),
            FinalProjectionInvocationKind.TableRowsParallel or FinalProjectionInvocationKind.TableChunkedRowsParallel
                => nameof(EvaluationHelper),
            _ => throw new InvalidOperationException($"Unsupported projection invocation kind '{kind}'.")
        };
    }

    private static string GetProjectionMethodName(FinalProjectionInvocationKind kind)
    {
        return kind switch
        {
            FinalProjectionInvocationKind.TypedValuesSerial => nameof(TypedProjectionRows.ProjectValuesSerial),
            FinalProjectionInvocationKind.TypedValuesParallel => nameof(TypedProjectionRows.ProjectValuesParallel),
            FinalProjectionInvocationKind.TypedChunkedValuesParallel => nameof(TypedProjectionRows.ProjectChunkedValuesParallel),
            FinalProjectionInvocationKind.TableRowsSerial => nameof(TableProjectionRows.ProjectRowsSerial),
            FinalProjectionInvocationKind.TableRowsParallel => nameof(EvaluationHelper.ProjectRowsParallel),
            FinalProjectionInvocationKind.TableChunkedRowsParallel => nameof(EvaluationHelper.ProjectChunkedRowsParallel),
            _ => throw new InvalidOperationException($"Unsupported projection invocation kind '{kind}'.")
        };
    }

    private static bool IsParallelProjection(FinalProjectionInvocationKind kind)
    {
        return kind is FinalProjectionInvocationKind.TypedValuesParallel
            or FinalProjectionInvocationKind.TypedChunkedValuesParallel
            or FinalProjectionInvocationKind.TableRowsParallel
            or FinalProjectionInvocationKind.TableChunkedRowsParallel;
    }
}
