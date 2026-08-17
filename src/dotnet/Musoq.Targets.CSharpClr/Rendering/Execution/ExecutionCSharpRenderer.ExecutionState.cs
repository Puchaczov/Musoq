using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private const string ExecutionStateVariableName = "__musoqExecutionState";

    private IEnumerable<StatementSyntax> CreateExecutionStateDeclarations(
        ExecutionPlan plan,
        ExecutionRenderContext context)
    {
        if (context.Session.IncludeTableResults)
            yield return CreateTableResultsLocalDeclaration(CountExecutionTableSlots(plan.Body));

        if (context.Session.IncludeCteRowResults)
            yield return CreateObjectLocalDeclaration(CteRowResultsFieldName, CreateCteRowResultsTypeSyntax());

        if (context.Session.IncludeCteIndexResults)
            yield return CreateObjectLocalDeclaration(CteIndexResultsFieldName, CreateCteIndexResultsTypeSyntax());

        yield return CreateExecutionStateLocalDeclaration(context);
    }

    private static LocalDeclarationStatementSyntax CreateTableResultsLocalDeclaration(int slotCount)
    {
        ExpressionSyntax initializer = slotCount == 0
            ? SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(nameof(Array)),
                    SyntaxFactory.GenericName(nameof(Array.Empty))
                        .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                CreateTypeSyntax(typeof(Table)))))))
            : SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(Table)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.NumericLiteralExpression,
                                SyntaxFactory.Literal(slotCount)))))));

        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            "_tableResults",
            initializer);
    }

    private static LocalDeclarationStatementSyntax CreateObjectLocalDeclaration(
        string variableName,
        TypeSyntax type)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            variableName,
            SyntaxFactory.ObjectCreationExpression(type)
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private LocalDeclarationStatementSyntax CreateExecutionStateLocalDeclaration(ExecutionRenderContext context)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            ExecutionStateVariableName,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(ExecutionState)),
                        SyntaxFactory.IdentifierName(nameof(ExecutionState.Capture))))
                .WithArgumentList(CreateArgumentList(CreateExecutionStateParametersSource(context))));
    }

    private static ExpressionSyntax CreateExecutionStateParametersSource(ExecutionRenderContext context)
    {
        return SyntaxFactory.IdentifierName(context.Session.UseQueryRunContext
            ? "__musoqRuntimeParameters"
            : nameof(IParameterizedRunnable.Parameters));
    }

    private static MemberAccessExpressionSyntax CreateExecutionStateParametersRead()
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(ExecutionStateVariableName),
            SyntaxFactory.IdentifierName(nameof(ExecutionState.Parameters)));
    }

    private static int CountExecutionTableSlots(ExecutionBlock block)
    {
        var maxIndex = FlattenNodes(block)
            .OfType<ExecutionStoreTable>()
            .Select(static store => store.TableIndex)
            .DefaultIfEmpty(-1)
            .Max();

        return maxIndex + 1;
    }
}
