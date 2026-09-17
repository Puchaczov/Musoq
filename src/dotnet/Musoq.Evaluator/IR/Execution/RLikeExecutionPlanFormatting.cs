namespace Musoq.Evaluator.IR.Execution;

internal static class RLikeExecutionPlanFormatting
{
    public static string FormatStringMatch(
        ExecutionStringMatch stringMatch,
        string input,
        string originalPattern,
        string needle)
    {
        var metadata = stringMatch.Comparison == ExecutionStringMatchComparison.Ordinal
            ? FormatOrdinalStringMatchMetadata(stringMatch)
            : string.Empty;
        return $"STRING_MATCH({input}, pattern={originalPattern}, needle={needle}, " +
               $"kind={stringMatch.Kind}, comparison={stringMatch.Comparison}{metadata})";
    }

    public static string FormatPrepareMatcher(ExecutionPrepareRLikeMatcher prepareRLike, string patternText)
    {
        if (prepareRLike.Pattern is not ExecutionLiteral literal || literal.Value.ToClrValue() is not string pattern)
        {
            return $"PREPARE_RLIKE({patternText}, strategy=runtime-classified, " +
                   "anchors=runtime, literal-span=runtime, fallback=runtime)";
        }

        var result = RLikeLiteralPatternClassifier.Classify(pattern);
        if (result.Classification is { } match)
        {
            return $"PREPARE_RLIKE({patternText}, strategy=direct-ordinal, anchors={match.Anchors}, " +
                   $"literal-span={match.LiteralStart}:{match.LiteralLength}, fallback=none)";
        }

        return $"PREPARE_RLIKE({patternText}, strategy=regex, anchors=none, " +
               $"literal-span=none, fallback={result.RejectionReason})";
    }

    private static string FormatOrdinalStringMatchMetadata(ExecutionStringMatch stringMatch)
    {
        var result = RLikeLiteralPatternClassifier.Classify(stringMatch.OriginalPattern);
        if (result.Classification is not { } literal ||
            ToExecutionStringMatchKind(literal.Kind) != stringMatch.Kind ||
            !stringMatch.OriginalPattern.AsSpan(literal.LiteralStart, literal.LiteralLength)
                .SequenceEqual(stringMatch.Needle.AsSpan()))
        {
            return ", strategy=direct-ordinal, anchors=explicit-node, literal-span=materialized, fallback=none";
        }

        return $", strategy=direct-ordinal, anchors={literal.Anchors}, " +
               $"literal-span={literal.LiteralStart}:{literal.LiteralLength}, fallback=none";
    }

    private static ExecutionStringMatchKind ToExecutionStringMatchKind(RLikeLiteralMatchKind kind) => kind switch
    {
        RLikeLiteralMatchKind.Exact => ExecutionStringMatchKind.Exact,
        RLikeLiteralMatchKind.Prefix => ExecutionStringMatchKind.Prefix,
        RLikeLiteralMatchKind.Suffix => ExecutionStringMatchKind.Suffix,
        RLikeLiteralMatchKind.Contains => ExecutionStringMatchKind.Contains,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown literal RLIKE match kind.")
    };
}
