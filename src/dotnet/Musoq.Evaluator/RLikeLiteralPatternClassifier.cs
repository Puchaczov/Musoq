namespace Musoq.Evaluator;

internal enum RLikeLiteralMatchKind
{
    Exact,
    Prefix,
    Suffix,
    Contains
}

internal enum RLikeLiteralAnchorKind
{
    None,
    Start,
    End,
    StartAndEnd
}

internal enum RLikeLiteralRejectionReason
{
    None,
    RegexMetacharacter,
    UnsupportedEscape
}

internal readonly record struct RLikeLiteralClassification(
    RLikeLiteralMatchKind Kind,
    RLikeLiteralAnchorKind Anchors,
    int LiteralStart,
    int LiteralLength);

internal readonly record struct RLikeLiteralClassificationResult
{
    private RLikeLiteralClassificationResult(
        RLikeLiteralClassification? classification,
        RLikeLiteralRejectionReason rejectionReason)
    {
        Classification = classification;
        RejectionReason = rejectionReason;
    }

    public RLikeLiteralClassification? Classification { get; }

    public RLikeLiteralRejectionReason RejectionReason { get; }

    public static RLikeLiteralClassificationResult Accepted(
        RLikeLiteralMatchKind kind,
        RLikeLiteralAnchorKind anchors,
        int literalStart,
        int literalLength) =>
        new(new RLikeLiteralClassification(kind, anchors, literalStart, literalLength), RLikeLiteralRejectionReason.None);

    public static RLikeLiteralClassificationResult Rejected(RLikeLiteralRejectionReason reason)
    {
        if (reason == RLikeLiteralRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));

        return new RLikeLiteralClassificationResult(null, reason);
    }
}

internal static class RLikeLiteralPatternClassifier
{
    public static RLikeLiteralClassificationResult Classify(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var hasStartAnchor = pattern.Length >= 2 && pattern[0] == '\\' && pattern[1] == 'A';
        var hasEndAnchor = pattern.Length >= 2 && pattern[^2] == '\\' && pattern[^1] == 'z';
        var literalStart = hasStartAnchor ? 2 : 0;
        var literalEnd = hasEndAnchor ? pattern.Length - 2 : pattern.Length;

        if (literalEnd < literalStart)
            return RLikeLiteralClassificationResult.Rejected(RLikeLiteralRejectionReason.UnsupportedEscape);

        for (var index = literalStart; index < literalEnd; index++)
        {
            var character = pattern[index];
            if (character == '\\')
                return RLikeLiteralClassificationResult.Rejected(RLikeLiteralRejectionReason.UnsupportedEscape);
            if (IsRegexMetacharacter(character))
                return RLikeLiteralClassificationResult.Rejected(RLikeLiteralRejectionReason.RegexMetacharacter);
        }

        var anchors = (hasStartAnchor, hasEndAnchor) switch
        {
            (true, true) => RLikeLiteralAnchorKind.StartAndEnd,
            (true, false) => RLikeLiteralAnchorKind.Start,
            (false, true) => RLikeLiteralAnchorKind.End,
            _ => RLikeLiteralAnchorKind.None
        };
        var kind = anchors switch
        {
            RLikeLiteralAnchorKind.StartAndEnd => RLikeLiteralMatchKind.Exact,
            RLikeLiteralAnchorKind.Start => RLikeLiteralMatchKind.Prefix,
            RLikeLiteralAnchorKind.End => RLikeLiteralMatchKind.Suffix,
            _ => RLikeLiteralMatchKind.Contains
        };

        return RLikeLiteralClassificationResult.Accepted(
            kind,
            anchors,
            literalStart,
            literalEnd - literalStart);
    }

    private static bool IsRegexMetacharacter(char character) => character is
        '^' or '$' or '.' or '|' or '?' or '*' or '+' or '(' or ')' or '[' or ']' or '{' or '}';
}
