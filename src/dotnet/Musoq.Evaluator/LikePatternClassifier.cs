namespace Musoq.Evaluator;

internal enum LikePatternMatchKind
{
    Exact,
    Prefix,
    Suffix,
    Contains
}

internal enum LikePatternCharacterDomain
{
    Ascii,
    Unicode
}

internal enum LikePatternRejectionReason
{
    None,
    SingleCharacterWildcard,
    InteriorPercentWildcard,
    NonAsciiPattern
}

internal readonly record struct LikePatternClassification(
    LikePatternMatchKind Kind,
    LikePatternCharacterDomain Domain,
    int LiteralStart,
    int LiteralLength);

internal readonly record struct LikePatternClassificationResult
{
    private LikePatternClassificationResult(
        LikePatternClassification? classification,
        LikePatternRejectionReason rejectionReason,
        LikePatternCharacterDomain domain)
    {
        Classification = classification;
        RejectionReason = rejectionReason;
        Domain = domain;
    }

    public LikePatternClassification? Classification { get; }

    public LikePatternRejectionReason RejectionReason { get; }

    public LikePatternCharacterDomain Domain { get; }

    public bool IsClassified => Classification.HasValue;

    public static LikePatternClassificationResult Accepted(
        LikePatternMatchKind kind,
        LikePatternCharacterDomain domain,
        int literalStart,
        int literalLength) =>
        new(
            new LikePatternClassification(kind, domain, literalStart, literalLength),
            LikePatternRejectionReason.None,
            domain);

    public static LikePatternClassificationResult Rejected(
        LikePatternRejectionReason reason,
        LikePatternCharacterDomain domain)
    {
        if (reason == LikePatternRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));

        return new LikePatternClassificationResult(null, reason, domain);
    }
}

internal static class LikePatternClassifier
{
    public static LikePatternClassificationResult Classify(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var domain = LikePatternCharacterDomain.Ascii;
        var hasSingleCharacterWildcard = false;
        var hasPercentWildcard = false;
        var leadingPercentCount = 0;
        var firstNonPercentIndex = -1;
        var lastNonPercentIndex = -1;
        var percentSeenAfterLiteral = false;
        var hasInteriorPercentWildcard = false;
        for (var index = 0; index < pattern.Length; index++)
        {
            var character = pattern[index];
            if (character > 127)
                domain = LikePatternCharacterDomain.Unicode;
            if (character == '_')
                hasSingleCharacterWildcard = true;
            if (character == '%')
            {
                hasPercentWildcard = true;
                if (firstNonPercentIndex < 0)
                    leadingPercentCount++;
                else
                    percentSeenAfterLiteral = true;
                continue;
            }

            if (percentSeenAfterLiteral)
                hasInteriorPercentWildcard = true;
            if (firstNonPercentIndex < 0)
                firstNonPercentIndex = index;
            lastNonPercentIndex = index;
        }

        if (hasSingleCharacterWildcard)
            return LikePatternClassificationResult.Rejected(LikePatternRejectionReason.SingleCharacterWildcard, domain);

        if (!hasPercentWildcard)
        {
            return LikePatternClassificationResult.Accepted(
                LikePatternMatchKind.Exact,
                domain,
                0,
                pattern.Length);
        }

        if (leadingPercentCount == pattern.Length)
        {
            return LikePatternClassificationResult.Accepted(
                LikePatternMatchKind.Contains,
                domain,
                pattern.Length,
                0);
        }

        if (hasInteriorPercentWildcard)
        {
            return LikePatternClassificationResult.Rejected(
                LikePatternRejectionReason.InteriorPercentWildcard,
                domain);
        }

        var trailingPercentCount = pattern.Length - lastNonPercentIndex - 1;
        var literalLength = lastNonPercentIndex - firstNonPercentIndex + 1;

        var kind = (leadingPercentCount > 0, trailingPercentCount > 0) switch
        {
            (true, true) => LikePatternMatchKind.Contains,
            (true, false) => LikePatternMatchKind.Suffix,
            (false, true) => LikePatternMatchKind.Prefix,
            _ => throw new InvalidOperationException("A classified boundary wildcard must have a leading or trailing percent.")
        };

        return LikePatternClassificationResult.Accepted(
            kind,
            domain,
            firstNonPercentIndex,
            literalLength);
    }
}
