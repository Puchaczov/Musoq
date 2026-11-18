namespace Musoq.Benchmarks.Scenarios;

public static class BasicQueryScenarios
{
    public const string Prefix = "BQ_";
    private const string Select = "select r.Id as Id, r.Name as Name from bench.rows() r";

    public static IReadOnlyList<Scenario> Projections { get; } =
        Array.AsReadOnly(
            new[]
            {
                Case("Project", Select, "Read two fields from every row."),
                Case("Filter0", Select + " where r.Id % 100 < 0", "Scan with no matching rows."),
                Case("Filter1", Select + " where r.Id % 100 < 1", "Project one percent of the rows."),
                Case("Filter50", Select + " where r.Id % 100 < 50", "Project half the rows."),
                Case("Filter100", Select + " where r.Id % 100 < 100", "Project every row after filtering."),
            }
        );

    public static IReadOnlyList<Scenario> All { get; } =
        Array.AsReadOnly<Scenario>([
            .. Projections,
            Case("ProjectSmallChunks", Select, "Project every row from 32-row chunks.", "small-chunks"),
            Case("Top1", Select + " order by r.Id desc take 1", "Select the latest observation.", ordered: true),
            Case(
                "Top10Ascending",
                Select + " order by r.Id take 10",
                "Keep the first ten observations.",
                ordered: true
            ),
            Case(
                "Top10Descending",
                Select + " order by r.Id desc take 10",
                "Keep replacing the ten latest observations.",
                ordered: true
            ),
            Case(
                "Top10Shuffled",
                Select + " order by r.Id desc take 10",
                "Select the latest ten unordered observations.",
                "shuffled",
                true
            ),
            Case(
                "TopOffset",
                Select + " order by r.Id desc skip 10 take 10",
                "Select the second page by recency.",
                ordered: true
            ),
            Case("Take10", Select + " take 10", "Read the first ten observations."),
            Case("SkipDeep", Select + " skip 90000 take 10", "Read a deep page or exhaust a shorter source."),
            Case("Count", "select Count(*) as Total from bench.rows() r", "Count all observations."),
            Case(
                "CountSmallChunks",
                "select Count(*) as Total from bench.rows() r",
                "Count observations in 32-row chunks.",
                "small-chunks"
            ),
        ]);

    private static Scenario Case(
        string id,
        string sql,
        string description,
        string profile = "default",
        bool ordered = false
    ) =>
        new(Prefix + id, "Basic queries", description, sql, "core/5,7,10,12", Ordered: ordered, DataProfileId: profile);
}
