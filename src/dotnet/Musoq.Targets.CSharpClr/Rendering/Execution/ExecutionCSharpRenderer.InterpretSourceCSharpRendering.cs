using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderInterpretSource(ExecutionInterpretSource interpret, ExecutionRenderContext context)
    {
        var interpreterVariableName = CreateInterpreterVariableName(interpret.SchemaName);
        var invocation = CreateInterpretInvocation(interpret, interpreterVariableName, context);

        return
        [
            CreateInterpreterDeclaration(interpreterVariableName, interpret.InterpreterTypeName),
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                interpret.Rows.Name,
                invocation)
        ];
    }

    private LocalDeclarationStatementSyntax RenderEnumerableSource(ExecutionEnumerableSource enumerable, ExecutionRenderContext context)
    {
        var sourceExpression = RenderEnumerableSourceExpression(enumerable, context);

        return CreateTypedEnumerableChunksDeclaration(
            enumerable.Rows.Name,
            sourceExpression,
            enumerable.EnumerableType.RequireClrType(),
            enumerable.ChunkMode,
            enumerable.EnumerableTypeName);
    }

    private ExpressionSyntax RenderEnumerableSourceExpression(ExecutionEnumerableSource enumerable, ExecutionRenderContext context)
    {
        var sourceExpression = RenderExpression(enumerable.Source, context);
        var enumerableType = CreateEnumerableTypeSyntax(enumerable);

        if (IsAlreadyRenderedAsEnumerableType(enumerable, sourceExpression, enumerableType))
            return sourceExpression;

        return SyntaxFactory.CastExpression(
            enumerableType,
            sourceExpression);
    }

    private static bool IsAlreadyRenderedAsEnumerableType(
        ExecutionEnumerableSource enumerable,
        ExpressionSyntax sourceExpression,
        TypeSyntax enumerableType)
    {
        if (!string.IsNullOrWhiteSpace(enumerable.EnumerableTypeName))
        {
            return sourceExpression is not CastExpressionSyntax explicitCast ||
                   SyntaxFactory.AreEquivalent(explicitCast.Type, enumerableType);
        }

        if (enumerable.Source.ReturnType.RequireClrType() != enumerable.EnumerableType.RequireClrType())
            return false;

        if (sourceExpression is not CastExpressionSyntax castExpression)
            return true;

        return
               SyntaxFactory.AreEquivalent(castExpression.Type, enumerableType);
    }

    private static TypeSyntax CreateEnumerableTypeSyntax(ExecutionEnumerableSource enumerable)
    {
        return string.IsNullOrWhiteSpace(enumerable.EnumerableTypeName)
            ? CreateTypeSyntax(enumerable.EnumerableType)
            : SyntaxFactory.ParseTypeName(enumerable.EnumerableTypeName);
    }

    private InvocationExpressionSyntax CreateInterpretInvocation(
        ExecutionInterpretSource interpret,
        string interpreterVariableName,
        ExecutionRenderContext context)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVariableName),
                    SyntaxFactory.IdentifierName(ResolveInterpretMethodName(interpret.Kind))))
            .WithArgumentList(CreateArgumentList(interpret.Arguments.Select(argument => RenderExpression(argument, context))));

        return interpret.Kind is InterpretSourceKind.TryInterpret or InterpretSourceKind.TryParse
            ? WrapInTryCatchReturningNull(invocation, interpret.InterpreterTypeName)
            : invocation;
    }

    private static LocalDeclarationStatementSyntax CreateInterpreterDeclaration(
        string variableName,
        string typeName)
    {
        return CreateLocalDeclaration(
            SyntaxFactory.IdentifierName("var"),
            variableName,
            SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName(typeName))
                .WithArgumentList(SyntaxFactory.ArgumentList()));
    }
}
