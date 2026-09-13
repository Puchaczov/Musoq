using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Musoq.Targets.CSharpClr;

internal static class ExecutionStringMatchSyntaxFactory
{
    internal static ExpressionSyntax Render(
        ExecutionStringMatch match,
        ExpressionSyntax input,
        ExecutionRenderSession session)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(session);

        if (match.Comparison != ExecutionStringMatchComparison.LikeIgnoreCase)
            throw UnsupportedShape.Of($"String comparison {match.Comparison}");

        if (match.Kind == ExecutionStringMatchKind.Contains && match.Needle.Length == 0)
        {
            return SyntaxFactory.IsPatternExpression(
                Parenthesize(input),
                SyntaxFactory.TypePattern(
                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))));
        }

        var variableName = $"__musoqStringMatch{++session.StringMatchPatternCount}";
        var variable = SyntaxFactory.IdentifierName(variableName);
        var inputIsString = SyntaxFactory.IsPatternExpression(
            Parenthesize(input),
            SyntaxFactory.DeclarationPattern(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
                SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(variableName))));
        var length = Member(variable, nameof(string.Length));
        var needleLength = Number(match.Needle.Length);
        var lengthCheck = SyntaxFactory.BinaryExpression(
            match.Kind == ExecutionStringMatchKind.Exact
                ? SyntaxKind.EqualsExpression
                : SyntaxKind.GreaterThanOrEqualExpression,
            length,
            needleLength);
        var direct = CreateDirectMatch(match, variable);
        ExpressionSyntax selectedMatch = direct;
        if (RequiresLegacyUnicodeFallback(match.Needle))
        {
            ExpressionSyntax asciiStart = match.Kind == ExecutionStringMatchKind.Suffix
                ? SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, length, needleLength)
                : Number(0);
            ExpressionSyntax asciiLength = match.Kind is ExecutionStringMatchKind.Exact or ExecutionStringMatchKind.Contains
                ? length
                : needleLength;
            ExpressionSyntax asciiCheck = match.Needle.Length == 1 && match.Kind != ExecutionStringMatchKind.Contains
                ? SyntaxFactory.BinaryExpression(
                    SyntaxKind.LessThanOrEqualExpression,
                    SyntaxFactory.ElementAccessExpression(variable)
                        .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                            SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(asciiStart)))),
                    Number(0x7f))
                : InvokeStatic(
                    nameof(Operators),
                    nameof(Operators.IsAsciiLikeSpan),
                    variable,
                    asciiStart,
                    asciiLength);
            var fallback = CreateLegacyFallback(match, variable);
            var compatibleCultureMatch = Or(direct, And(Not(asciiCheck), fallback));
            selectedMatch = match.Needle.AsSpan().IndexOfAny('I', 'i') < 0
                ? compatibleCultureMatch
                : SyntaxFactory.ConditionalExpression(
                    InvokeStatic(nameof(Operators), nameof(Operators.IsOrdinalIgnoreCaseLikeCompatible)),
                    compatibleCultureMatch,
                    fallback);
        }

        return Parenthesize(SyntaxFactory.ConditionalExpression(
            inputIsString,
            And(lengthCheck, Parenthesize(selectedMatch)),
            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)));
    }

    private static ExpressionSyntax CreateDirectMatch(
        ExecutionStringMatch match,
        IdentifierNameSyntax variable)
    {
        if (match.Needle.Length == 1 && match.Kind != ExecutionStringMatchKind.Contains)
            return CreateSingleAsciiCharacterMatch(match, variable);

        var needle = ExecutionSyntaxFactory.CreateStringLiteral(match.Needle);
        var comparison = Member(
            SyntaxFactory.IdentifierName(nameof(StringComparison)),
            nameof(StringComparison.OrdinalIgnoreCase));

        return match.Kind switch
        {
            ExecutionStringMatchKind.Exact => InvokeStatic(
                "string",
                nameof(string.Equals),
                variable,
                needle,
                comparison),
            ExecutionStringMatchKind.Prefix => Invoke(
                Member(variable, nameof(string.StartsWith)),
                needle,
                comparison),
            ExecutionStringMatchKind.Suffix => Invoke(
                Member(variable, nameof(string.EndsWith)),
                needle,
                comparison),
            ExecutionStringMatchKind.Contains => Invoke(
                Member(variable, nameof(string.Contains)),
                needle,
                comparison),
            _ => throw UnsupportedShape.Of($"String match kind {match.Kind}")
        };
    }

    private static ExpressionSyntax CreateSingleAsciiCharacterMatch(
        ExecutionStringMatch match,
        IdentifierNameSyntax variable)
    {
        ExpressionSyntax index = match.Kind == ExecutionStringMatchKind.Suffix
            ? SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                Member(variable, nameof(string.Length)),
                Number(1))
            : Number(0);
        var value = SyntaxFactory.ElementAccessExpression(variable)
            .WithArgumentList(SyntaxFactory.BracketedArgumentList(
                SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(index))));
        var expected = SyntaxFactory.LiteralExpression(
            SyntaxKind.CharacterLiteralExpression,
            SyntaxFactory.Literal(match.Needle[0]));
        var foldedValue = SyntaxFactory.BinaryExpression(
            SyntaxKind.BitwiseOrExpression,
            Parenthesize(value),
            Number(0x20));
        var foldedExpected = match.Needle[0] | 0x20;
        var exact = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, value, expected);
        var foldedEqual = SyntaxFactory.BinaryExpression(
            SyntaxKind.EqualsExpression,
            Parenthesize(foldedValue),
            Number(foldedExpected));
        var foldedIsLetter = SyntaxFactory.BinaryExpression(
            SyntaxKind.LessThanOrEqualExpression,
            Parenthesize(SyntaxFactory.BinaryExpression(
                SyntaxKind.SubtractExpression,
                Parenthesize(foldedValue),
                Number('a'))),
            Number('z' - 'a'));
        return Or(exact, And(foldedEqual, foldedIsLetter));
    }

    private static ExpressionSyntax CreateLegacyFallback(
        ExecutionStringMatch match,
        IdentifierNameSyntax variable)
    {
        if (match is { Kind: ExecutionStringMatchKind.Prefix, Needle.Length: 1 })
        {
            return InvokeStatic(
                nameof(Operators),
                nameof(Operators.LikeLegacyRegexSingleAsciiPrefix),
                variable,
                ExecutionSyntaxFactory.CreateStringLiteral(match.OriginalPattern),
                ExecutionSyntaxFactory.CreateStringLiteral(match.Needle));
        }

        return InvokeStatic(
            nameof(Operators),
            nameof(Operators.LikeLegacyRegex),
            variable,
            ExecutionSyntaxFactory.CreateStringLiteral(match.OriginalPattern));
    }

    private static bool RequiresLegacyUnicodeFallback(string needle)
    {
        foreach (var character in needle)
        {
            if (character is 'I' or 'i' or 'K' or 'k' or 'S' or 's')
                return true;
        }

        return false;
    }

    private static BinaryExpressionSyntax And(ExpressionSyntax left, ExpressionSyntax right) =>
        SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalAndExpression,
            Parenthesize(left),
            Parenthesize(right));

    private static BinaryExpressionSyntax Or(ExpressionSyntax left, ExpressionSyntax right) =>
        SyntaxFactory.BinaryExpression(
            SyntaxKind.LogicalOrExpression,
            Parenthesize(left),
            Parenthesize(right));

    private static PrefixUnaryExpressionSyntax Not(ExpressionSyntax expression) =>
        SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, Parenthesize(expression));

    private static InvocationExpressionSyntax InvokeStatic(
        string typeName,
        string methodName,
        params ExpressionSyntax[] arguments) =>
        Invoke(Member(SyntaxFactory.IdentifierName(typeName), methodName), arguments);

    private static InvocationExpressionSyntax Invoke(
        ExpressionSyntax target,
        params ExpressionSyntax[] arguments) =>
        SyntaxFactory.InvocationExpression(target)
            .WithArgumentList(ExecutionSyntaxFactory.CreateArgumentList(arguments));

    private static MemberAccessExpressionSyntax Member(ExpressionSyntax receiver, string name) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            receiver,
            SyntaxFactory.IdentifierName(name));

    private static LiteralExpressionSyntax Number(int value) =>
        SyntaxFactory.LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            SyntaxFactory.Literal(value));

    private static ParenthesizedExpressionSyntax Parenthesize(ExpressionSyntax expression) =>
        expression as ParenthesizedExpressionSyntax ?? SyntaxFactory.ParenthesizedExpression(expression);
}
