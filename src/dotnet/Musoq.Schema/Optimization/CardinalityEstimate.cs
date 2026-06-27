namespace Musoq.Schema.Optimization;

public sealed record CardinalityEstimate(
    CardinalityKind Kind,
    long? ExactRows,
    long? LowerBound,
    long? UpperBound,
    double Confidence,
    string? Reason)
{
    public static CardinalityEstimate Unknown(string? reason = null)
    {
        return new CardinalityEstimate(CardinalityKind.Unknown, null, null, null, 0d, reason);
    }

    public static CardinalityEstimate Exact(long rows, string? reason = null)
    {
        return new CardinalityEstimate(CardinalityKind.Exact, rows, rows, rows, 1d, reason);
    }

    public static CardinalityEstimate Bounded(long? lowerBound, long? upperBound, double confidence, string? reason = null)
    {
        return new CardinalityEstimate(CardinalityKind.Bounded, null, lowerBound, upperBound, confidence, reason);
    }

    public static CardinalityEstimate Estimate(long rows, double confidence, string? reason = null)
    {
        return new CardinalityEstimate(CardinalityKind.Estimate, null, rows, rows, confidence, reason);
    }
}
