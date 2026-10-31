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
        var declaration = CreateInterpreterDeclaration(interpreterVariableName, interpret.InterpreterTypeName);
        if (interpret.Kind is InterpretSourceKind.TryInterpret or InterpretSourceKind.TryParse)
            return RenderTolerantInterpretSource(interpret, declaration, invocation);

        return
        [
            declaration,
            CreateLocalDeclaration(
                SyntaxFactory.IdentifierName("var"),
                interpret.Rows.Name,
                invocation)
        ];
    }

    private static IReadOnlyList<StatementSyntax> RenderTolerantInterpretSource(ExecutionInterpretSource interpret,
        StatementSyntax declaration, ExpressionSyntax invocation)
    {
        var rows = SyntaxFactory.IdentifierName(interpret.Rows.Name);
        var nullValue = SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
        var assignment = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression, rows, invocation));
        var reset = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression, rows, nullValue));
        var parseFailure = SyntaxFactory.CatchClause()
            .WithDeclaration(SyntaxFactory.CatchDeclaration(
                SyntaxFactory.ParseTypeName("global::Musoq.Schema.Interpreters.ParseException")))
            .WithBlock(SyntaxFactory.Block(reset));
        return
        [
            declaration,
            CreateLocalDeclaration(SyntaxFactory.NullableType(SyntaxFactory.ParseTypeName(interpret.InterpreterTypeName)),
                interpret.Rows.Name, nullValue),
            SyntaxFactory.TryStatement(SyntaxFactory.Block(assignment), SyntaxFactory.SingletonList(parseFailure), null)
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

        return invocation;
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
