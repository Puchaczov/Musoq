using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class ExecutionCSharpRenderer
{
    private ExpressionSyntax RenderContextFieldRead(
        ExecutionFieldRead fieldRead,
        ContextAccess contextAccess,
        ExecutionRenderContext context)
    {
        if (string.IsNullOrWhiteSpace(fieldRead.Alias))
            throw new InvalidOperationException("Context field reads require a source alias.");

        var value = TryCreateGeneratedRowContextStorageRead(fieldRead.Alias, contextAccess, context, out var contextRead)
            ? contextRead
            : CreateContextArrayElementRead(fieldRead.Alias, contextAccess.Index);

        if (fieldRead.ReturnType == typeof(object))
            return value;

        return SyntaxFactory.CastExpression(CreateTypeSyntax(fieldRead.ReturnType), value);
    }

    private bool TryCreateGeneratedRowContextStorageRead(
        string alias,
        ContextAccess contextAccess,
        ExecutionRenderContext context,
        out ExpressionSyntax value)
    {
        value = null!;
        if (!context.Session.GeneratedRowVariableTypeNamesByName.TryGetValue(alias, out var typeNames))
            return false;

        foreach (var typeName in typeNames)
        {
            if (TryCreateGeneratedRowContextStorageRead(
                    alias,
                    new GeneratedRowContextAccess(typeName, contextAccess.Index),
                    context,
                    out value))
            {
                return true;
            }
        }

        return false;
    }

    private static ElementAccessExpressionSyntax CreateContextArrayElementRead(
        string alias,
        int index)
    {
        var contexts = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            CreateIdentifierName(alias),
            SyntaxFactory.IdentifierName("Contexts"));

        return CreateElementAccess(
            contexts,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(index)));
    }
}
