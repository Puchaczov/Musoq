using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private LocalDeclarationStatementSyntax RenderLet(
        ExecutionLet let,
        ExecutionRenderContext context)
    {
        return CreateLocalDeclaration(
            CreateVariableTypeSyntax(let.Variable),
            let.Variable.Name,
            RenderExpression(let.Value, context));
    }

    private ExpressionStatementSyntax RenderAssign(ExecutionAssign assign)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateIdentifierName(assign.Variable.Name),
                RenderExpression(assign.Value)));
    }

    private static LocalDeclarationStatementSyntax RenderCreateBooleanArray(ExecutionCreateBooleanArray createArray)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(bool)))
                .WithRankSpecifiers(SyntaxFactory.SingletonList(
                    SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                        SyntaxFactory.OmittedArraySizeExpression())))),
            createArray.Array.Name,
            SyntaxFactory.ArrayCreationExpression(
                SyntaxFactory.ArrayType(CreateTypeSyntax(typeof(bool)))
                    .WithRankSpecifiers(SyntaxFactory.SingletonList(
                        SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                            CreateBufferCountExpression(createArray.LengthSource)))))));
    }

    private ExpressionStatementSyntax RenderArrayAssign(ExecutionArrayAssign arrayAssign)
    {
        return SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                CreateElementAccess(
                    SyntaxFactory.IdentifierName(arrayAssign.Array.Name),
                    RenderExpression(arrayAssign.Index)),
                SyntaxFactory.CastExpression(
                    CreateTypeSyntax(arrayAssign.ElementType),
                    SyntaxFactory.ParenthesizedExpression(RenderExpression(arrayAssign.Value)))));
    }

    private static LocalDeclarationStatementSyntax RenderCreateObject(ExecutionCreateObject createObject)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            createObject.Target.Name,
            SyntaxFactory.ObjectCreationExpression(CreateTypeSyntax(createObject.Target.Type))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }

    private IfStatementSyntax RenderIf(
        ExecutionIf branch,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateIf(this.RenderBooleanCondition(branch.Condition, context), RenderBlock(branch.Body, context));
    }

    private IfStatementSyntax RenderContinueIf(
        ExecutionContinueIf continueIf,
        ExecutionRenderContext context)
    {
        return StatementEmitter.CreateIf(
            this.RenderBooleanCondition(continueIf.Condition, context),
            StatementEmitter.CreateBlock(SyntaxFactory.ContinueStatement()));
    }
}
