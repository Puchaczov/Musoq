using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Benchmarks.Scenarios;
using Musoq.Converter;
using Musoq.Schema.Optimization;

namespace Musoq.Benchmarks.Tests;

[TestClass]
public sealed class FixtureEdgeTests
{
    [TestMethod]
    public void StructuredSource_PreservesProvidedValuesWithoutRegeneratingFixtures()
    {
        using var cancellation = new CancellationTokenSource();
        var context = new SourceExecutionContext("structured", SourceExecutionPlan.Empty(SourceIdentity.Empty),
            cancellation.Token, [], new Dictionary<string, string>(), Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        NumberInput[] inputs = [new(7, 99m)];
        var source = new StructuredRows(inputs, context);
        NumberInput[] expected = [new(7, 99m)];
        CollectionAssert.AreEqual(expected, source.Chunks.Single().ToArray());
    }

    [TestMethod]
    public void RepeatedGroupingKeys_PreserveNullAndFilteredDistinctSemantics()
    {
        var scenario = ScenarioCatalog.Get("B14_Group");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns,
            [[0, 6m, 1L], [1, 8m, 0L], [2, 10m, 1L], [3, 12m, 0L]]);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "repeated-keys", new FixtureProvider(8),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario));
        using var table = query.Run();
        ResultAssertions.Table(expected, table, scenario);
    }

    [TestMethod]
    public void EmptyScan_PreservesOutputSchema()
    {
        var scenario = ScenarioCatalog.Get("B01_Scan");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns, []);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "empty-scan", new FixtureProvider(0),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario));
        using var table = query.Run();
        ResultAssertions.Table(expected, table, scenario);
    }

    [TestMethod]
    public void NonEqualityJoin_RepeatedKeysHaveAtMostTwoOrderedMatches()
    {
        var scenario = ScenarioCatalog.Get("B08_ThetaJoin");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns,
            [[0, 1], [0, 2], [1, 2], [4, 1], [4, 2], [5, 2]]);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "bounded-theta", new FixtureProvider(8),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, 8));
        ResultAssertions.Execution(expected, query, scenario);
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void NonEqualityJoin_EmptyInputPreservesColumnsAndReturnsNoRows()
    {
        var scenario = ScenarioCatalog.Get("B08_ThetaJoin");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns, []);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "empty-theta", new FixtureProvider(0),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario, 0));
        ResultAssertions.Execution(expected, query, scenario);
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void PacketAnalytics_AccumulatesBeyondIntRangeWithFourInputRows()
    {
        var scenario = ScenarioCatalog.Get("X08_PacketAnalytics");
        var expected = new ExpectedResult(Expectations.Get(scenario.Id).Columns, [["INFO", 6_000_000_012L, 6L]]);
        using var query = InstanceCreator.CompileForExecution(scenario.Sql, "wide-packet-total",
            new FixtureProvider(FixtureData.TestSize, payloadValueOffset: 1_000_000_000),
            BenchmarkLogger.Instance, ExecutionSettings.Create(scenario));
        ResultAssertions.Execution(expected, query, scenario);
        ResultAssertions.Execution(expected, query, scenario);
    }

    [TestMethod]
    public void ImmutableFixtures_GrantConcurrentAccessAndRowProcessing()
    {
        var schema = new FixtureSchema(4);
        using var cancellation = new CancellationTokenSource();
        var metadata = new SourceMetadataContext("permissions", cancellation.Token, [], new Dictionary<string, string>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        var descriptor = schema.DescribeSource("rows", new SourceDescribeContext(SourceIdentity.Empty, metadata));
        Assert.AreEqual(SourceExecutionPermissions.ConcurrentAccess | SourceExecutionPermissions.ConcurrentRowProcessing,
            descriptor.ExecutionPermissions);
    }
}
