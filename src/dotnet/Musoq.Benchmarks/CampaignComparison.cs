namespace Musoq.Benchmarks;

public static class CampaignComparison
{
    public static bool SameSource(SourceStamp left, SourceStamp right) =>
        left with { Tags = Array.Empty<string>() } == right with { Tags = Array.Empty<string>() }
        && left.Tags.SequenceEqual(right.Tags, StringComparer.Ordinal);

    public static bool SameProvenance(RunProvenance left, RunProvenance right) =>
        SameSource(left.Source, right.Source) && left.Build == right.Build && SameMeasurementEnvironment(left, right);

    internal static bool SameMeasurementEnvironment(RunProvenance left, RunProvenance right) =>
        left.Hardware with
        {
            Unavailable = Array.Empty<string>(),
        } == right.Hardware with { Unavailable = Array.Empty<string>() }
        && left.Hardware.Unavailable.SequenceEqual(right.Hardware.Unavailable, StringComparer.Ordinal)
        && left.Environment with { RuntimeVariables = null } == right.Environment with { RuntimeVariables = null }
        && SameArguments(left.Environment.RuntimeVariables, right.Environment.RuntimeVariables);

    public static bool SameCases(CaseDefinition[] left, CaseDefinition[] right) =>
        left.Length == right.Length && left.Zip(right).All(pair => SameCase(pair.First, pair.Second));

    internal static bool SameCase(CaseDefinition left, CaseDefinition right) =>
        left with { Arguments = null } == right with { Arguments = null }
        && SameArguments(left.Arguments, right.Arguments);

    internal static string? WorkloadDifference(CaseDefinition left, CaseDefinition right)
    {
        var differences = new List<string>();
        if (left.Id != right.Id || left.ScenarioId != right.ScenarioId)
            differences.Add("case identity");
        if (left.Sql != right.Sql)
            differences.Add("SQL");
        if (left.InputCount != right.InputCount || left.InputUnit != right.InputUnit || left.Size != right.Size)
            differences.Add("input size or unit");
        if (left.Phase != right.Phase || left.ResultApi != right.ResultApi || left.Consumption != right.Consumption)
            differences.Add("measurement phase, result API or consumption");
        if (left.Distribution != right.Distribution || !SameArguments(left.Arguments, right.Arguments))
            differences.Add("fixture distribution or arguments");
        if (left.JobSettings != right.JobSettings)
            differences.Add("case job settings");
        return differences.Count == 0 ? null : $"Benchmark definition changed ({string.Join(", ", differences)}).";
    }

    public static bool SameMeasurements(Measurement[] left, Measurement[] right) =>
        left.Length == right.Length
        && left.Zip(right)
            .All(pair =>
                pair.First with { RawReports = null } == pair.Second with { RawReports = null }
                && (pair.First.RawReports ?? []).SequenceEqual(pair.Second.RawReports ?? [], StringComparer.Ordinal)
            );

    private static bool SameArguments(SortedDictionary<string, string>? left, SortedDictionary<string, string>? right)
    {
        left ??= new(StringComparer.Ordinal);
        right ??= new(StringComparer.Ordinal);
        return left.Count == right.Count
            && left.All(item => right.ContainsKey(item.Key) && right[item.Key] == item.Value);
    }
}
