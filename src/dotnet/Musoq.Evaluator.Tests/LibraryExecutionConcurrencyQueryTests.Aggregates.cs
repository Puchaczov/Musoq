using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;

namespace Musoq.Evaluator.Tests;

public sealed partial class LibraryExecutionConcurrencyQueryTests
{
    [TestMethod]
    [DataRow(false, false)]
    [DataRow(false, true)]
    [DataRow(true, false)]
    [DataRow(true, true)]
    public void CustomCountQuery_WhenSourceAndKernelPermissionsIntersect_ShouldOwnIndependentState(bool sourceAllowed, bool functionAllowed)
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, sourceAllowed);
        var function = functionAllowed ? "PermittedCount" : "SerialCount";
        var query = $"select Position % 8 as Partition, {function}(Position) as Total from #schema.first() group by Position % 8";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.AreEqual(sourceAllowed && functionAllowed,
            inspection.GeneratedCSharpCode.Contains("ParallelSingleKeyAggregateChunkWorker_", StringComparison.Ordinal), inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(int)), ("Total", typeof(long)));
        Assert.HasCount(8, rows);
        var counts = rows.ToDictionary(static row => row.GetValue<int>(0), static row => row.GetValue<long>(1));
        for (var partition = 0; partition < 8; partition++)
            Assert.AreEqual(512L, counts[partition]);
    }

    [TestMethod]
    public void CountQuery_WhenSourcePermitsWorkers_ShouldMergeIndependentTypedStates()
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, true);
        const string query = "select Position % 8 as Partition, Count(Position) as Total from #schema.first() group by Position % 8";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.Contains("ParallelSingleKeyAggregateChunkWorker_", inspection.GeneratedCSharpCode);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        for (var execution = 0; execution < 3; execution++)
        {
            using var result = compiled.Run(TestContext.CancellationToken);
            var rows = result.ToArray();
            TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(int)), ("Total", typeof(long)));
            Assert.HasCount(8, rows);
            var counts = rows.ToDictionary(static row => row.GetValue<int>(0), static row => row.GetValue<long>(1));
            for (var partition = 0; partition < 8; partition++)
                Assert.AreEqual(512L, counts[partition]);
        }
    }

    [TestMethod]
    public void CheckedSumQuery_WhenMergeExistsWithoutEquivalencePermission_ShouldRemainSerial()
    {
        using var probe = new ConcurrencyProbe(false);
        var provider = CreateProvider(probe, true);
        const string query = "select Position % 8 as Partition, Sum(Position) as Total from #schema.first() group by Position % 8";
        var inspection = InstanceCreator.CompileForInspection(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        Assert.DoesNotContain("ParallelSingleKeyAggregateChunkWorker_", inspection.GeneratedCSharpCode);
        Assert.Contains("equivalent partial merging", inspection.PlanningText);
        using var compiled = InstanceCreator.CompileForExecution(query, Guid.NewGuid().ToString("N"), provider, _logger, ParallelOptions);
        using var result = compiled.Run(TestContext.CancellationToken);
        var rows = result.ToArray();
        TableMaterializationTestHelper.AssertColumns(result, ("Partition", typeof(int)), ("Total", typeof(int?)));
        Assert.HasCount(8, rows);
        var totals = rows.ToDictionary(static row => row.GetValue<int>(0), static row => row.GetValue<int?>(1));
        for (var partition = 0; partition < 8; partition++)
            Assert.AreEqual((int?)(partition * 512 + 8 * 511 * 512 / 2), totals[partition]);
    }
}
