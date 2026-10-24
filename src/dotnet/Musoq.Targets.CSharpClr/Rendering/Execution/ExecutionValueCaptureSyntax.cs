using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionValueCaptureSyntax
{
    internal static ParenthesizedExpressionSyntax Capture(ExpressionSyntax input, string name, ExpressionSyntax body)
    {
        var receiver = input is ParenthesizedExpressionSyntax ? input : SyntaxFactory.ParenthesizedExpression(input);
        var pattern = SyntaxFactory.VarPattern(SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(name)));
        return SyntaxFactory.ParenthesizedExpression(SyntaxFactory.SwitchExpression(receiver)
            .WithArms(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.SwitchExpressionArm(pattern, body))));
    }
}
