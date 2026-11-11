namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    private static void AddCostScenarios(List<Scenario> scenarios)
    {
        foreach (var id in new[] { "B01_Scan", "B01_Wide" })
        {
            var original = scenarios.Single(item => item.Id == id);
            scenarios.Add(original with
            {
                Id = id + "_Unordered", Ordered = false,
                Description = "Isolate projection and row consumption without a final sort.",
                Sql = original.Sql.Replace(" order by r.Id", "", StringComparison.Ordinal)
            });
        }
        foreach (var percentage in new[] { 0, 1, 50, 100 })
            scenarios.Add(new($"B02_Filter_Selectivity{percentage}", "Expressions",
                $"Unordered filter retains {percentage}% at standard sizes; isolate predicate and output volume.",
                FormattableString.Invariant($"select r.Id as Id from perf.rows() r where r.Id % 100 < {percentage}"),
                "core/7", Ordered: false, FixtureId: "performance"));
        scenarios.AddRange(Costs());
    }

    private static IEnumerable<Scenario> Costs() =>
    [
        new("B26_ComputedProjection_Accepted", "Contracts", "Provider accepts safe widened arithmetic into typed query-row fields.",
            "select r.Id as Id, r.Id::long * 2 as Value from computed.accepted() r",
            "core/6,18", Ordered: false, FixtureId: "computed"),
        new("B26_ComputedProjection_Residual", "Contracts", "Equivalent widened arithmetic remains in Core over the same typed inputs.",
            "select r.Id as Id, r.Id::long * 2 as Value from computed.residual() r",
            "core/6,18", Ordered: false, FixtureId: "computed"),
        new("B07_StringKeyJoin", "Relational", "Build and probe N unique string keys; shuffled right input; N outputs.",
            "select r.Id as Id, d.Id as Match from perf.rows() r inner join perf.right() d on r.StringKey = d.StringKey",
            "core/8", Ordered: false, FixtureId: "performance"),
        new("B07_NullableCompositeJoin", "Relational", "Unique composite keys with twenty percent NULL subkeys; bounded matching output.",
            "select r.Id as Id, d.Id as Match from perf.rows() r inner join perf.right() d on r.Partition = d.Partition and r.Subkey = d.Subkey and r.Subkey is not null",
            "core/8", Ordered: false, FixtureId: "performance"),
        new("B10_PartitionedRangeExists", "Relational", "Equality-partitioned range existence over N right rows; at most one output per left row.",
            "select r.Id as Id from perf.rows() r where exists (select d.Id from perf.right() d where d.Partition = r.Partition and d.Id > r.Id)",
            "core/7.10", Ordered: false, FixtureId: "performance"),
        new("B03_StableScalarReuse", "Expressions", "Two identical stable bounded computations over one serial source row.",
            "select r.Id as Id, StableWork(r.Text) as First, StableWork(r.Text) as Second from perf.rows() r",
            "core/18", Ordered: false, FixtureId: "performance", DataProfileId: "scalar-serial"),
        new("B03_VolatileScalarControl", "Expressions", "Same bounded computation with metadata forbidding scalar reuse; serial control.",
            "select r.Id as Id, VolatileWork(r.Text) as First, VolatileWork(r.Text) as Second from perf.rows() r",
            "core/18", Ordered: false, FixtureId: "performance", DataProfileId: "scalar-serial"),
        new("B13_ApplyInvariant", "Composition", "Stable scalar independent of each of eight expanded items; serial invariant movement.",
            "select r.Id as Id, i.Value as Item, r.StableWork(r.Text) as Work from perf.rows() r cross apply r.Items i",
            "core/9,18", Ordered: false, FixtureId: "performance", DataProfileId: "scalar-serial"),
        new("B14_CompositeGroups", "Analytics", "String/integer grouping yields four groups per hundred-row block at standard sizes.",
            "select r.GroupKey as Key, r.Bucket as Bucket, Sum(r.Amount) as Total from perf.rows() r group by r.GroupKey, r.Bucket",
            "core/10", Ordered: false, FixtureId: "performance"),
        new("B16_PartitionedRowsFrame", "Analytics", "Sixty-four partitions with a thirty-two-row sliding aggregate frame.",
            "select r.Id as Id, Sum(r.Amount) over (partition by r.Partition order by r.Id rows between 31 preceding and current row) as Total from perf.rows() r",
            "core/11", Ordered: false, FixtureId: "performance"),
        new("B27_Binary4KiB", "Interpretation", "Read a four-byte integer and materialize the 4092-byte tail from each 4096-byte packet.",
            "binary LargePacket { Value: int le, Tail: byte[4092] }; select r.Id as Id, p.Value as Value, Length(p.Tail) as TailLength from perf.rows() r cross apply Interpret<LargePacket>(r.Payload) p",
            "binary-text/4", Ordered: false, FixtureId: "performance", DataProfileId: "binary-4k"),
        new("B30_Text1KiB", "Interpretation", "Parse a 1024-character ASCII record and consume delimiter fields and the remaining label length.",
            Entry + "select r.Id as Id, p.Level as Level, p.Number::int as Number, Length(p.Label) as LabelLength from perf.rows() r cross apply Parse<Entry>(r.Text) p",
            "binary-text/5", Ordered: false, FixtureId: "performance", DataProfileId: "text-1k")
    ];
}
