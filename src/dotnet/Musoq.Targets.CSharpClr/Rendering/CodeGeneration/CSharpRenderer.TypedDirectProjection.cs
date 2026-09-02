using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private static bool TryCreateTypedDirectProjectionMethod(
        ExecutionPlan plan,
        ExecutionCSharpRenderer executionRenderer,
        string rowsMethodName,
        TypedOutputBinding binding,
        TableViaRowsResultInfo resultInfo,
        FinalProjectionSinkPlan sinkPlan,
        bool useQueryRunContext,
        out MethodDeclarationSyntax method,
        out QueryMethodRenderMetadata metadata)
    {
        return TryCreateFinalSinkMethod(
            plan,
            executionRenderer,
            sinkPlan,
            [],
            setup => CreateTypedDirectProjectionMethod(
                rowsMethodName,
                binding,
                executionRenderer,
                setup.ProjectionLoop,
                setup.SourceSetupStatements,
                setup.RenderContext),
            useQueryRunContext,
            out method,
            out metadata);
    }

    private static MethodDeclarationSyntax CreateTypedDirectProjectionMethod(
        string rowsMethodName,
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        IReadOnlyList<StatementSyntax> sourceSetupStatements,
        ExecutionRenderContext renderContext)
    {
        const string sourceRowsName = "__musoqTypedSourceRows";
        var statements = new List<StatementSyntax>(sourceSetupStatements)
        {
            CreateSourceRowsLocalDeclaration(executionRenderer, projectionLoop, sourceRowsName, renderContext)
        };

        if (projectionLoop.CanUseParallel)
        {
            if (CanUseChunkedParallelProjection(projectionLoop))
                statements.AddRange(CreateChunkedParallelReturnStatements(binding, executionRenderer, projectionLoop, sourceRowsName, renderContext));

            statements.AddRange(CreateParallelReturnStatements(binding, executionRenderer, projectionLoop, sourceRowsName, renderContext));
        }
        else
        {
            statements.Add(CreateSerialReturnStatement(binding, executionRenderer, projectionLoop, sourceRowsName, renderContext));
        }

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            CreateTypeSyntax(binding.OutputType)))),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateTypedRunContextParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }

    private static IEnumerable<StatementSyntax> CreateChunkedParallelReturnStatements(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        yield return SyntaxFactory.IfStatement(
            CreateStreamingChunkedRowsCondition(projectionLoop, sourceRowsName),
            SyntaxFactory.Block(SyntaxFactory.ReturnStatement(
                CreateChunkedParallelReturnExpression(binding, executionRenderer, projectionLoop, sourceRowsName, renderContext))));
    }

    private static IEnumerable<StatementSyntax> CreateParallelReturnStatements(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        const string parallelRowsName = "__musoqTypedParallelRows";
        yield return CreateParallelRowsProbeDeclaration(projectionLoop, sourceRowsName, parallelRowsName);
        yield return SyntaxFactory.ReturnStatement(
            CreateShardedReturnExpression(binding, executionRenderer, projectionLoop, parallelRowsName, renderContext));
    }

    private static ReturnStatementSyntax CreateSerialReturnStatement(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return SyntaxFactory.ReturnStatement(
            CreateProjectValuesSerialInvocation(binding, executionRenderer, projectionLoop, sourceRowsName, renderContext));
    }

    private static InvocationExpressionSyntax CreateGetParallelRowsInvocation(
        TypedProjectionLoop projectionLoop,
        string sourceRowsName)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(EvaluationHelper)),
                    SyntaxFactory.GenericName(nameof(EvaluationHelper.GetParallelProjectionRowsOrEmpty))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(CreateSourceTypeSyntax(projectionLoop.Source))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(sourceRowsName)),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(projectionLoop.Threshold)))
            ])));
    }

    private static InvocationExpressionSyntax CreateShardedReturnExpression(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateQueryRowsShardInvocation(
            nameof(QueryRows.FromShards),
            CreateProjectValuesParallelInvocation(
                binding,
                executionRenderer,
                projectionLoop,
                parallelRowsName,
                renderContext));
    }

    private static InvocationExpressionSyntax CreateChunkedParallelReturnExpression(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateProjectValuesChunkedParallelInvocation(
            binding,
            executionRenderer,
            projectionLoop,
            sourceRowsName,
            renderContext);
    }

    private static InvocationExpressionSyntax CreateProjectValuesParallelInvocation(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string parallelRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            CreateTypeSyntax(binding.OutputType),
            parallelRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateProjectionLambda(binding, executionRenderer, projectionLoop, renderContext),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectValuesChunkedParallelInvocation(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedChunkedValuesParallel,
            CreateSourceTypeSyntax(projectionLoop.Source),
            CreateTypeSyntax(binding.OutputType),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateProjectionLambda(binding, executionRenderer, projectionLoop, renderContext),
            projectionLoop.MaxDegreeOfParallelism));
    }

    private static InvocationExpressionSyntax CreateProjectValuesSerialInvocation(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        string sourceRowsName,
        ExecutionRenderContext renderContext)
    {
        return CreateFinalProjectionInvocation(new FinalProjectionInvocationSpec(
            FinalProjectionInvocationKind.TypedValuesSerial,
            CreateSourceTypeSyntax(projectionLoop.Source),
            CreateTypeSyntax(binding.OutputType),
            sourceRowsName,
            CreatePredicateLambda(executionRenderer, projectionLoop, renderContext),
            CreateProjectionLambda(binding, executionRenderer, projectionLoop, renderContext)));
    }

    private static ParenthesizedLambdaExpressionSyntax CreatePredicateLambda(
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        ExecutionRenderContext? renderContext = null)
    {
        var body = projectionLoop.Predicate == null
            ? SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)
            : executionRenderer.RenderPredicateExpression(projectionLoop.Predicate, renderContext);

        return CreateSourceLambda(projectionLoop.Source, body);
    }

    private static ParenthesizedLambdaExpressionSyntax CreateProjectionLambda(
        TypedOutputBinding binding,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        ExecutionRenderContext? renderContext = null)
    {
        var values = projectionLoop.AppendRow.Values
            .Select(value => RenderFinalSinkExpression(executionRenderer, value.Value, renderContext))
            .ToArray();

        return CreateSourceLambda(projectionLoop.Source, binding.CreateOutputExpression(values));
    }

    private static ParenthesizedLambdaExpressionSyntax CreateSourceLambda(
        ExecutionVariable source,
        ExpressionSyntax expression)
    {
        return SyntaxFactory.ParenthesizedLambdaExpression(expression)
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(EscapeIdentifier(source.Name))))));
    }

    private static TypeSyntax CreateSourceTypeSyntax(ExecutionVariable source)
    {
        return string.IsNullOrWhiteSpace(source.GeneratedRowTypeName)
            ? CreateTypeSyntax(source.Type)
            : SyntaxFactory.ParseTypeName(source.GeneratedRowTypeName);
    }

    private static LocalDeclarationStatementSyntax CreateLocalDeclaration(
        TypeSyntax type,
        string name,
        ExpressionSyntax initializer)
    {
        return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(type)
                .WithVariables(SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(name))
                        .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))));
    }

    private static string EscapeIdentifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None
            ? name
            : $"@{name}";
    }
}
