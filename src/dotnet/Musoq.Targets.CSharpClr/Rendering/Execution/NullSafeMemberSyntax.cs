using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class NullSafeMemberSyntax
{
    internal static ExpressionSyntax Render(ExecutionMemberRead member, ExpressionSyntax receiver)
    {
        var read = SyntaxFactory.ConditionalAccessExpression(SyntaxFactory.ParenthesizedExpression(receiver),
            SyntaxFactory.MemberBindingExpression(CreateIdentifierName(member.MemberName)));
        var type = member.ReturnType.RequireClrType();
        return type.IsValueType && Nullable.GetUnderlyingType(type) is null
            ? SyntaxFactory.ParenthesizedExpression(SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression,
                read, SyntaxFactory.DefaultExpression(CreateTypeSyntax(member.ReturnType))))
            : read;
    }
}
