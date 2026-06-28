using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Visitors.CodeGeneration;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private const string ExecutionStateVariableName = "__musoqExecutionState";

    private IEnumerable<StatementSyntax> CreateExecutionStateDeclarations(ExecutionPlan plan)
    {
        if (_includeTableResults)
            yield return CreateTableResultsLocalDeclaration(CountExecutionTableSlots(plan.Body));

        if (_includeCteRowResults)
            yield return CreateObjectLocalDeclaration(CteRowResultsFieldName, CreateCteRowResultsTypeSyntax());

        if (_includeCteIndexResults)
            yield return CreateObjectLocalDeclaration(CteIndexResultsFieldName, CreateCteIndexResultsTypeSyntax());

        yield return CreateExecutionStateLocalDeclaration();
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

    private LocalDeclarationStatementSyntax CreateExecutionStateLocalDeclaration()
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            ExecutionStateVariableName,
            SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName(nameof(ExecutionState)),
                        SyntaxFactory.IdentifierName(nameof(ExecutionState.Capture))))
                .WithArgumentList(CreateArgumentList(CreateExecutionStateParametersSource())));
    }

    private ExpressionSyntax CreateExecutionStateParametersSource()
    {
        return SyntaxFactory.IdentifierName(_useQueryRunContext
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
