using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Musoq.Targets.CSharpClr;

internal static class NullSafeIndexedPathSyntax
{
    private static readonly Regex PathSegments = new(
        @"(?<member>[A-Za-z_]\w*)|\[(?<index>-?\d+)\]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    internal static ExpressionSyntax Render(ExpressionSyntax receiver, string path)
    {
        var members = path.TrimStart('.').Replace(".", "?.", StringComparison.Ordinal)
            .Replace("[", "?[", StringComparison.Ordinal);
        var separator = members.StartsWith("?[", StringComparison.Ordinal) ? string.Empty : "?.";
        var guarded = GuardReceiver(SyntaxFactory.ParseExpression(receiver.ToString()));
        return SyntaxFactory.ParseExpression($"({guarded}){separator}{members}");
    }

    internal static ExpressionSyntax Render(ExpressionSyntax receiver, string path, bool isNullSafe)
    {
        var current = receiver;
        var position = 0;
        var sawIndex = false;
        foreach (Match segment in PathSegments.Matches(path))
        {
            var separator = path[position..segment.Index];
            if (separator.Length != 0 && separator != ".")
                return RenderFallback(receiver, path, isNullSafe);
            position = segment.Index + segment.Length;

            if (segment.Groups["index"].Success)
            {
                var index = segment.Groups["index"].Value;
                var indexedValue = SyntaxFactory.ParseExpression(
                    $"global::Musoq.Evaluator.Helpers.SafeArrayAccess.GetArrayElement({current}, {index})");
                current = isNullSafe && !sawIndex
                    ? SyntaxFactory.ParseExpression($"({current}) is null ? null : {indexedValue}")
                    : indexedValue;
                sawIndex = true;
                continue;
            }

            var memberName = segment.Groups["member"].Value;
            var access = isNullSafe || sawIndex ? "?." : ".";
            current = SyntaxFactory.ParseExpression($"({current}){access}{memberName}");
        }

        if (!sawIndex || position != path.Length)
            return RenderFallback(receiver, path, isNullSafe);

        return current;
    }

    private static ExpressionSyntax RenderFallback(ExpressionSyntax receiver, string path, bool isNullSafe)
    {
        if (isNullSafe)
            return Render(receiver, path);

        var normalized = path.TrimStart('.');
        var separator = path.StartsWith("[", StringComparison.Ordinal) ? string.Empty : ".";
        return SyntaxFactory.ParseExpression($"({receiver}){separator}{normalized}");
    }

    private static ExpressionSyntax GuardReceiver(ExpressionSyntax receiver) => receiver switch
    {
        ParenthesizedExpressionSyntax parenthesized => parenthesized.WithExpression(GuardReceiver(parenthesized.Expression)),
        CastExpressionSyntax cast => cast.WithExpression(SyntaxFactory.ParenthesizedExpression(GuardReceiver(cast.Expression))),
        MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax } member =>
            SyntaxFactory.ParenthesizedExpression(SyntaxFactory.ConditionalAccessExpression(
                SyntaxFactory.ParenthesizedExpression(GuardReceiver(member.Expression)), SyntaxFactory.MemberBindingExpression(member.Name))),
        _ => receiver
    };
}
