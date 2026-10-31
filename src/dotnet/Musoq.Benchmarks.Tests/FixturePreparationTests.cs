using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class FixturePreparationTests
{
    [TestMethod]
    public void PreparedDefault_PreservesEveryInputField()
    {
        var fixture = FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), 4);
        string[] expected =
        [
            "0;0;1;item-0;NULL;0,1;0001000000020A0B;INFO|0|item-0;0;Queued;000100",
            "1;1;2;item-1;1;1,2;0102000000020A0B;INFO|1|item-1;1;Running;010200",
            "2;2;3;item-2;2;2,3;0203000000020A0B;INFO|2|item-2;2;Queued;020300",
            "3;3;4;item-3;NULL;3,4;0304000000020A0B;INFO|3|item-3;3;Running;030400"
        ];
        CollectionAssert.AreEqual(expected, ReadRows(fixture.CreateSchemaProvider()).Select(Snapshot).ToArray());
    }

    [TestMethod]
    public void PreparedProviders_ReuseTheirPreparedInputs()
    {
        var fixture = FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), 4);
        var first = ReadRows(fixture.CreateSchemaProvider());
        var second = ReadRows(fixture.CreateSchemaProvider());
        Assert.AreSame(first[0], second[0]);
    }

    [TestMethod]
    public void SeparatePreparations_DoNotShareMutablePayloads()
    {
        var scenario = ScenarioCatalog.Get("B01_Scan");
        var first = ReadRows(FixtureCatalog.Prepare(scenario, 4).CreateSchemaProvider());
        var second = ReadRows(FixtureCatalog.Prepare(scenario, 4).CreateSchemaProvider());
        first[0].Items[0] = -1;
        first[0].Payload[1] = 99;
        CollectionAssert.AreEqual(new[] { 0, 1 }, new[] { second[0].Items[0], (int)second[0].Payload[1] });
    }

    [TestMethod]
    public void PreparedDefault_PreservesChunksAndCustomInputCount()
    {
        var provider = FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), 4_097).CreateSchemaProvider();
        CollectionAssert.AreEqual(new[] { 4_096, 1 }, ReadChunkSizes(provider));
        Assert.AreEqual(4_096, ReadRows(provider)[^1].Id);
    }

    [TestMethod]
    public void PreparedDefault_PreservesHelperSourceSizes()
    {
        var provider = FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), 0).CreateSchemaProvider();
        var actual = new[] { "rows", "skewed", "planned", "lookup", "reconcile", "twice", "empty", "stream" }
            .Select(name => ReadRows(provider, name).Length).ToArray();
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 3, 4, 6, 0, 1 }, actual);
    }

    [TestMethod]
    public void PreparedStream_PreservesPacketBytesAndMalformedTail()
    {
        var provider = FixtureCatalog.Prepare(ScenarioCatalog.Get("X09_RecursiveChunks"), 2).CreateSchemaProvider();
        CollectionAssert.AreEqual(Convert.FromHexString("0001000000020A0B0102000000020A0B000100"),
            ReadRows(provider, "stream").Single().Payload);
    }

    [TestMethod]
    public void ExplicitColumns_PreserveOrderAndTypes()
    {
        (string, Type)[] expected =
        [
            ("Id", typeof(int)), ("Key", typeof(int)), ("Amount", typeof(decimal)), ("Name", typeof(string)),
            ("Optional", typeof(int?)), ("Items", typeof(int[])), ("Payload", typeof(byte[])),
            ("Text", typeof(string)), ("Time", typeof(DateTime)), ("State", typeof(JobState)), ("Truncated", typeof(byte[]))
        ];
        CollectionAssert.AreEqual(expected, FixtureTable.Rows().Columns.Select(column => (column.ColumnName, column.SourceReadType)).ToArray());
    }

    [TestMethod]
    public void ExplicitEnumColumn_PreservesItsIntegralCarrierAndLogicalType()
    {
        var column = FixtureTable.Rows().Columns[9];
        Assert.AreEqual(typeof(int), column.ColumnType);
        Assert.AreEqual(typeof(JobState), column.SourceReadType);
        Assert.IsNotNull(column.EnumType);
    }

    [TestMethod]
    public void ExplicitStructuredColumns_PreserveOrderAndTypes()
    {
        (string, Type)[] expected = [("Id", typeof(int)), ("Amount", typeof(decimal))];
        CollectionAssert.AreEqual(expected, FixtureTable.NumberInput().Columns.Select(column => (column.ColumnName, column.ColumnType)).ToArray());
    }

    [TestMethod]
    public void SharedStandardSizes_PreserveTheExecutionMatrix() =>
        CollectionAssert.AreEqual(new[] { 1_000, 10_000, 100_000 }, BenchmarkInputSizes.Standard.ToArray());

    [TestMethod]
    [DataRow("missing", "default")]
    [DataRow("standard", "missing")]
    public void UnknownFixtureOrProfile_IsRejected(string fixtureId, string profileId)
    {
        var scenario = ScenarioCatalog.Get("B01_Scan") with { FixtureId = fixtureId, DataProfileId = profileId };
        Assert.Throws<ArgumentException>(() => FixtureCatalog.Prepare(scenario, 4));
        Assert.Throws<ArgumentException>(() => FixtureCatalog.GetRequirements(scenario, 4));
    }

    [TestMethod]
    public void NegativeInputCount_IsRejectedBeforePreparation() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), -1));

    [TestMethod]
    public void StructuralBudgetOverflow_IsRejectedBeforeAllocatingInputs() =>
        Assert.Throws<OverflowException>(() => FixtureCatalog.Prepare(ScenarioCatalog.Get("B01_Scan"), int.MaxValue));

    [TestMethod]
    public void LegacyProviderBudgetOverflow_IsRejectedBeforeAllocatingInputs() =>
        Assert.Throws<OverflowException>(() => new FixtureProvider(int.MaxValue));

    [TestMethod]
    public void PreparedRequirements_ExposeActualRecursionAndStructuralBudgets()
    {
        var requirements = FixtureCatalog.GetRequirements(ScenarioCatalog.Get("X09_RecursiveChunks"), 100_000);
        Assert.AreEqual(100_002, requirements.RecursiveMaxIterations);
        Assert.AreEqual(300_001, requirements.StructuralLimits.MaxNodes);
        Assert.IsFalse(requirements.ValidatePrimitiveTypes);
    }

    internal static FixtureRow[] ReadRows(ISchemaProvider provider, string name = "rows")
    {
        using var cancellation = new CancellationTokenSource();
        var context = Context(cancellation.Token);
        return provider.GetSchema("bench").GetRowSource<FixtureRow, EmptySourceArguments>(name, context, SourceArguments.Empty)
            .Chunks.SelectMany(chunk => chunk).ToArray();
    }

    private static int[] ReadChunkSizes(ISchemaProvider provider)
    {
        using var cancellation = new CancellationTokenSource();
        return provider.GetSchema("bench").GetRowSource<FixtureRow, EmptySourceArguments>("rows", Context(cancellation.Token), SourceArguments.Empty)
            .Chunks.Select(chunk => chunk.Count).ToArray();
    }

    private static SourceExecutionContext Context(CancellationToken token) => new("fixture-test",
        SourceExecutionPlan.Empty(SourceIdentity.Empty), token, [], new Dictionary<string, string>(), NullLogger.Instance);

    internal static string Snapshot(FixtureRow row) => FormattableString.Invariant(
        $"{row.Id};{row.Key};{row.Amount};{row.Name};{row.Optional?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "NULL"};{string.Join(',', row.Items)};{Convert.ToHexString(row.Payload)};{row.Text};{(row.Time - new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds};{row.State};{Convert.ToHexString(row.Truncated)}");
}
