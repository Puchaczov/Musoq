using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.Helpers;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private static bool CanRenderCollectionInCheck(ExecutionCollectionInCheck collectionInCheck)
    {
        return CanRenderExpression(collectionInCheck.Expression) &&
               CanRenderExpression(collectionInCheck.Collection) &&
               CanReferenceType(collectionInCheck.ElementType);
    }

    private ExpressionSyntax RenderCollectionInCheck(
        ExecutionCollectionInCheck collectionInCheck,
        ExecutionRenderContext context) =>
        RenderCollectionParameterCheck(
            collectionInCheck,
            context,
            nameof(EvaluationHelper.CollectionParameterContains));

    private ExpressionSyntax RenderCollectionNotInCheck(
        ExecutionCollectionInCheck collectionInCheck,
        ExecutionRenderContext context) =>
        RenderCollectionParameterCheck(
            collectionInCheck,
            context,
            nameof(EvaluationHelper.CollectionParameterNotContains));

    private ExpressionSyntax RenderCollectionParameterCheck(
        ExecutionCollectionInCheck collectionInCheck,
        ExecutionRenderContext context,
        string helperName)
    {
        return SyntaxFactory.InvocationExpression(CreateGenericEvaluationHelperMemberAccess(
                helperName,
                EvaluationHelper.GetCastableType(collectionInCheck.ElementType.RequireClrType())))
            .WithArgumentList(CreateArgumentList(
                RenderExpression(collectionInCheck.Expression, context),
                RenderExpression(collectionInCheck.Collection, context)));
    }
}
