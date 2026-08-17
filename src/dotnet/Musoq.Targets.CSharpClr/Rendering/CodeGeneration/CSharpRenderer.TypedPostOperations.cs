using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class CSharpRenderer
{
    private static bool TryCreateTypedPostOperationRowsMethod(
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
            setup => CreateTypedPostOperationRowsMethod(
                rowsMethodName,
                binding,
                resultInfo,
                executionRenderer,
                setup.ProjectionLoop,
                setup.SinkPlan.PostOperations,
                setup.SourceSetupStatements,
                setup.RenderContext),
            useQueryRunContext,
            out method,
            out metadata);
    }

    private static MethodDeclarationSyntax CreateTypedPostOperationRowsMethod(
        string rowsMethodName,
        TypedOutputBinding binding,
        TableViaRowsResultInfo resultInfo,
        ExecutionCSharpRenderer executionRenderer,
        TypedProjectionLoop projectionLoop,
        IReadOnlyList<TypedPostOperation> postOperations,
        IReadOnlyList<StatementSyntax> sourceSetupStatements,
        ExecutionRenderContext renderContext)
    {
        const string sourceRowsName = "__musoqTypedPostSourceRows";
        const string projectedRowsName = "__musoqTypedPostRows";
        var rowType = SyntaxFactory.ParseTypeName(resultInfo.RowTypeName);
        var statements = new List<StatementSyntax>(sourceSetupStatements)
        {
            CreateSourceRowsLocalDeclaration(executionRenderer, projectionLoop, sourceRowsName, renderContext),
            SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(rowType))))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(projectedRowsName)))))
        };

        if (projectionLoop.CanUseParallel)
        {
            const string parallelRowsName = "__musoqTypedPostParallelRows";
            statements.Add(CreateParallelRowsProbeDeclaration(projectionLoop, sourceRowsName, parallelRowsName));
            statements.Add(CreateTypedPostRowsAssignment(
                projectedRowsName,
                CreateRowShardedReturnExpression(resultInfo, executionRenderer, projectionLoop, parallelRowsName, renderContext)));
        }
        else
        {
            statements.Add(CreateTypedPostRowsAssignment(
                projectedRowsName,
                CreateProjectRowsSerialInvocation(resultInfo, executionRenderer, projectionLoop, sourceRowsName, renderContext)));
        }

        foreach (var operation in postOperations)
            statements.AddRange(CreateTypedPostOperationStatements(resultInfo.RowTypeName, projectedRowsName, operation));

        statements.Add(SyntaxFactory.ReturnStatement(CreateTypedPostProjectInvocation(binding, resultInfo.RowTypeName, projectedRowsName)));

        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.GenericName(nameof(IEnumerable<object>))
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            ExecutionSyntaxFactory.CreateTypeSyntax(binding.OutputType)))),
                SyntaxFactory.Identifier(rowsMethodName))
            .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
            .WithParameterList(MethodDeclarationHelper.CreateTypedRunContextParameterList())
            .WithBody(SyntaxFactory.Block(statements));
    }

    private static ExpressionStatementSyntax CreateTypedPostRowsAssignment(
        string projectedRowsName,
        ExpressionSyntax rowsExpression)
    {
        return SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.IdentifierName(projectedRowsName),
            rowsExpression));
    }

    private static IEnumerable<StatementSyntax> CreateTypedPostOperationStatements(
        string rowTypeName,
        string rowsName,
        TypedPostOperation operation)
    {
        switch (operation)
        {
            case TypedPostOperation.Distinct:
                yield return CreateTypedPostRowsAssignment(
                    rowsName,
                    SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(nameof(TypedPostOperationRows)),
                                SyntaxFactory.GenericName(nameof(TypedPostOperationRows.Distinct))
                                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(rowTypeName))))))
                        .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rowsName))))));
                yield break;

            case TypedPostOperation.Order order:
                yield return CreateTypedPostRowsAssignment(rowsName, CreateTypedOrderInvocation(rowTypeName, rowsName, order.Keys));
                yield break;

            case TypedPostOperation.Skip skip:
                yield return CreateTypedPostRowsAssignment(rowsName, CreateTypedRowsMethodExpression(SyntaxFactory.IdentifierName(rowsName), "Skip", skip.Count));
                yield break;

            case TypedPostOperation.Take take:
                yield return CreateTypedPostRowsAssignment(rowsName, CreateTypedRowsMethodExpression(SyntaxFactory.IdentifierName(rowsName), "Take", take.Count));
                yield break;

            default:
                throw new InvalidOperationException($"Typed post operation '{operation.GetType().Name}' was not rendered.");
        }
    }

    private static InvocationExpressionSyntax CreateTypedOrderInvocation(
        string rowTypeName,
        string rowsName,
        IReadOnlyList<ExecutionOrderField> keys)
    {
        var orderKeyType = SyntaxFactory.GenericName("TypedRowOrderKey")
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(rowTypeName))));
        var orderKeys = SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(orderKeyType)
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.OmittedArraySizeExpression())))))
            .WithInitializer(SyntaxFactory.InitializerExpression(
                SyntaxKind.ArrayInitializerExpression,
                SyntaxFactory.SeparatedList<ExpressionSyntax>(keys.Select(key => CreateTypedRowOrderKey(rowTypeName, key)))));

        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(TypedPostOperationRows)),
                    SyntaxFactory.GenericName(nameof(TypedPostOperationRows.Order))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(rowTypeName))))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rowsName)),
                SyntaxFactory.Argument(orderKeys)
            ])));
    }

    private static ObjectCreationExpressionSyntax CreateTypedRowOrderKey(string rowTypeName, ExecutionOrderField key)
    {
        const string rowName = "__musoqOrderRow";
        return SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName("TypedRowOrderKey")
                    .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList(SyntaxFactory.ParseTypeName(rowTypeName)))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(
                        SyntaxFactory.ElementAccessExpression(SyntaxFactory.IdentifierName(rowName))
                            .WithArgumentList(SyntaxFactory.BracketedArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                                    SyntaxKind.NumericLiteralExpression,
                                    SyntaxFactory.Literal(key.OutputIndex)))))))
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(rowName)))))),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(key.Descending ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)),
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal((int)key.NullOrdering)))
            ])));
    }

    private static InvocationExpressionSyntax CreateTypedPostProjectInvocation(
        TypedOutputBinding binding,
        string rowTypeName,
        string rowsName)
    {
        const string rowName = "__musoqTypedPostRow";
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(TypedPostOperationRows)),
                    SyntaxFactory.GenericName(nameof(TypedPostOperationRows.Project))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(
                        [
                            SyntaxFactory.ParseTypeName(rowTypeName),
                            ExecutionSyntaxFactory.CreateTypeSyntax(binding.OutputType)
                        ])))))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(
            [
                SyntaxFactory.Argument(SyntaxFactory.IdentifierName(rowsName)),
                SyntaxFactory.Argument(SyntaxFactory.ParenthesizedLambdaExpression(binding.CreateOutputExpression(rowName))
                    .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(rowName))))))
            ])));
    }

    private static InvocationExpressionSyntax CreateTypedRowsMethodExpression(
        ExpressionSyntax sourceRows,
        string methodName,
        int count)
    {
        return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    sourceRows,
                    SyntaxFactory.IdentifierName(methodName)))
            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(
                    SyntaxKind.NumericLiteralExpression,
                    SyntaxFactory.Literal(count))))));
    }

}
