using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Targets.CSharpClr;

public sealed partial class ExecutionCSharpRenderer
{
    private IEnumerable<StatementSyntax> RenderInterpretSource(ExecutionInterpretSource interpret)
    {
        var interpreterVariableName = CreateInterpreterVariableName(interpret.SchemaName);
        var invocation = CreateInterpretInvocation(interpret, interpreterVariableName);

        if (IsScalarInterpretSourceKind(interpret.Kind))
        {
            return
            [
                CreateInterpreterDeclaration(interpreterVariableName, interpret.InterpreterTypeName),
                CreateLocalDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    interpret.Rows.Name,
                    invocation)
            ];
        }

        var wrappedExpression = CreateScalarToArrayWrapper(
            invocation,
            ResolveInterpretResultTypeName(interpret));

        return
        [
            CreateInterpreterDeclaration(interpreterVariableName, interpret.InterpreterTypeName),
            CreateEnumerableChunksDeclaration(
                interpret.Rows.Name,
                wrappedExpression,
                SyntaxFactory.ParseTypeName(ResolveInterpretResultTypeName(interpret)))
        ];
    }

    private LocalDeclarationStatementSyntax RenderEnumerableSource(ExecutionEnumerableSource enumerable)
    {
        var sourceExpression = RenderEnumerableSourceExpression(enumerable);

        return CreateTypedEnumerableChunksDeclaration(
            enumerable.Rows.Name,
            sourceExpression,
            enumerable.EnumerableType.RequireClrType(),
            enumerable.ChunkMode,
            enumerable.EnumerableTypeName);
    }

    private ExpressionSyntax RenderEnumerableSourceExpression(ExecutionEnumerableSource enumerable)
    {
        var sourceExpression = RenderExpression(enumerable.Source);
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
        string interpreterVariableName)
    {
        var invocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(interpreterVariableName),
                    SyntaxFactory.IdentifierName(ResolveInterpretMethodName(interpret.Kind))))
            .WithArgumentList(CreateArgumentList(interpret.Arguments.Select(RenderExpression)));

        return interpret.Kind is InterpretSourceKind.TryInterpret or InterpretSourceKind.TryParse
            ? WrapInTryCatchReturningNull(invocation, interpret.InterpreterTypeName)
            : invocation;
    }

    private static string ResolveInterpretResultTypeName(ExecutionInterpretSource interpret)
    {
        return interpret.Kind is InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse
            ? $"Musoq.Schema.Interpreters.PartialInterpretResult<{interpret.InterpreterTypeName}>"
            : interpret.InterpreterTypeName;
    }

    private static bool IsScalarInterpretSourceKind(InterpretSourceKind kind)
    {
        return kind is not (InterpretSourceKind.PartialInterpret or InterpretSourceKind.PartialParse);
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
