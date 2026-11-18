using Musoq.Plugins;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Managers;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Scenarios;

public sealed record NumericBatchRow<T>(int Id, T Reading, T Low, T High);

internal static class NumericBatchWorkloads
{
    internal static IEnumerable<Scenario> Cases()
    {
        const string stored =
            "with samples as (select a.Id as Id, b.Reading as Reading, b.Low as Low, b.High as High from num.rows() a inner join num.right() b on a.Id = b.Id) ";
        const string project = "select r.Id as Id, (r.Reading + r.Reading) - r.Reading * r.Reading as Score from ";
        const string filter = " r where (r.Reading between r.Low and r.High or r.Reading is null) and r.Id >= 0";
        foreach (
            var profile in new[]
            {
                "Int32",
                "Int64",
                "Single",
                "Double",
                "NullableInt32",
                "NullableInt64",
                "NullableSingle",
                "NullableDouble",
            }
        )
        {
            yield return new(
                $"V_NumericStored_{profile}",
                "Analytics",
                "Filter stored numeric columns and compute a score.",
                stored + project + "samples" + filter,
                "core/7,10",
                Ordered: false,
                FixtureId: "numeric-batch",
                DataProfileId: profile
            );
            yield return new(
                $"V_NumericStream_{profile}",
                "Analytics",
                "Filter numeric source rows and sort the computed scores.",
                project + "num.rows()" + filter + " order by r.Id",
                "core/7,10",
                FixtureId: "numeric-batch",
                DataProfileId: profile
            );
        }
    }

    internal static PreparedFixture Prepare(Scenario scenario, int count) =>
        scenario.DataProfileId switch
        {
            "Int32" => Create<int>(scenario, count, i => i),
            "Int64" => Create<long>(scenario, count, i => i),
            "Single" => Create<float>(scenario, count, i => i),
            "Double" => Create<double>(scenario, count, i => i),
            "NullableInt32" => Create<int?>(scenario, count, i => i),
            "NullableInt64" => Create<long?>(scenario, count, i => i),
            "NullableSingle" => Create<float?>(scenario, count, i => i),
            "NullableDouble" => Create<double?>(scenario, count, i => i),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };

    private static PreparedFixture Create<T>(Scenario scenario, int count, Func<int, T> convert)
    {
        var rows = new NumericBatchRow<T>[count];
        var nullable = default(T) is null;
        for (var i = 0; i < rows.Length; i++)
            rows[i] = new(i, nullable && i % 5 == 0 ? default! : convert(i % 64), convert(2), convert(15));
        var right = rows.ToArray();
        StandardDataProfiles.Shuffle(right);
        return new(
            scenario,
            count,
            VectorizationScenarios.Requirements(scenario, count),
            () => new NumericBatchSchema<T>(rows, right)
        );
    }
}

internal sealed class NumericBatchSchema<T>(NumericBatchRow<T>[] rows, NumericBatchRow<T>[] right)
    : SchemaBase("num", CreateLibrary()),
        ISchemaProvider
{
    private readonly NumericRows _left = new(rows);
    private readonly NumericRows _right = new(right);
    private readonly FixtureTable _table = new(
        typeof(NumericBatchRow<T>),
        [
            new SchemaColumn(nameof(NumericBatchRow<T>.Id), 0, typeof(int)),
            new SchemaColumn(nameof(NumericBatchRow<T>.Reading), 1, typeof(T)),
            new SchemaColumn(nameof(NumericBatchRow<T>.Low), 2, typeof(T)),
            new SchemaColumn(nameof(NumericBatchRow<T>.High), 3, typeof(T)),
        ]
    );

    public ISchema GetSchema(string schema) =>
        schema.TrimStart('#') == "num" ? this : throw new ArgumentException(schema);

    public override ISchemaTable GetTableByName(
        string name,
        SourceMetadataContext context,
        params object?[] parameters
    ) => _table;

    public override RowSource<TRow> GetRowSource<TRow, TArguments>(
        string name,
        SourceExecutionContext context,
        TArguments arguments
    ) =>
        EnsureSourceType<TRow, NumericBatchRow<T>>(
            name,
            name == "rows" ? _left
                : name == "right" ? _right
                : throw new ArgumentException(name)
        );

    public override SourceDescriptor DescribeSource(
        string name,
        SourceDescribeContext context,
        params object?[] parameters
    ) => base.DescribeSource(name, context, parameters) with { ExecutionPermissions = SourceExecutionPermissions.None };

    private static MethodsAggregator CreateLibrary()
    {
        var manager = new MethodsManager();
        manager.RegisterLibraries(new LibraryBase());
        return new(manager);
    }

    private sealed class NumericRows(NumericBatchRow<T>[] input) : RowSource<NumericBatchRow<T>>
    {
        private readonly NumericBatchRow<T>[][] _chunks = input.Chunk(4096).ToArray();
        public override IEnumerable<IReadOnlyList<NumericBatchRow<T>>> Chunks => _chunks;
    }
}
