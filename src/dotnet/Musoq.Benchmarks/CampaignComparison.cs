namespace Musoq.Benchmarks;

public static class CampaignComparison
{
    public static bool SameSource(SourceStamp left, SourceStamp right) =>
        left with { Tags = Array.Empty<string>() } == right with { Tags = Array.Empty<string>() } &&
        left.Tags.SequenceEqual(right.Tags, StringComparer.Ordinal);

    public static bool SameProvenance(RunProvenance left, RunProvenance right) =>
        SameSource(left.Source, right.Source) && left.Build == right.Build &&
        left.Hardware with { Unavailable = Array.Empty<string>() } == right.Hardware with { Unavailable = Array.Empty<string>() } &&
        left.Hardware.Unavailable.SequenceEqual(right.Hardware.Unavailable, StringComparer.Ordinal) &&
        left.Environment with { RuntimeVariables = null } == right.Environment with { RuntimeVariables = null } &&
        SameArguments(left.Environment.RuntimeVariables, right.Environment.RuntimeVariables);

    public static bool SameCases(CaseDefinition[] left, CaseDefinition[] right) =>
        left.Length == right.Length && left.Zip(right).All(pair =>
            pair.First with { Arguments = null } == pair.Second with { Arguments = null } &&
            SameArguments(pair.First.Arguments, pair.Second.Arguments));

    public static bool SameMeasurements(Measurement[] left, Measurement[] right) =>
        left.Length == right.Length && left.Zip(right).All(pair =>
            pair.First with { RawReports = null } == pair.Second with { RawReports = null } &&
            (pair.First.RawReports ?? []).SequenceEqual(pair.Second.RawReports ?? [], StringComparer.Ordinal));

    private static bool SameArguments(SortedDictionary<string, string>? left, SortedDictionary<string, string>? right)
    {
        left ??= new(StringComparer.Ordinal);
        right ??= new(StringComparer.Ordinal);
        return left.Count == right.Count && left.All(item => right.ContainsKey(item.Key) && right[item.Key] == item.Value);
    }
}
