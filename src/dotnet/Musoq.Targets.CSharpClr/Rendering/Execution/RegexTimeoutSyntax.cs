using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace Musoq.Targets.CSharpClr;

internal static class RegexTimeoutSyntax
{
    private static readonly TimeSpan DefaultTimeout = new CompilationOptions().RegexMatchTimeout;
    private static readonly ExpressionSyntax DefaultExpression = CreateExpression(DefaultTimeout);

    internal static ExpressionSyntax Create(TimeSpan timeout)
    {
        if (timeout == DefaultTimeout)
            return DefaultExpression;
        return CreateExpression(timeout);
    }

    private static ExpressionSyntax CreateExpression(TimeSpan timeout) => SyntaxFactory.InvocationExpression(
        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(nameof(TimeSpan)), SyntaxFactory.IdentifierName(nameof(TimeSpan.FromTicks))),
        SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression,
                SyntaxFactory.Literal($"{timeout.Ticks.ToString(CultureInfo.InvariantCulture)}L", timeout.Ticks))))));
}
