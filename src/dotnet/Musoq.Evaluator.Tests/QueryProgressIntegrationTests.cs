using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Components;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.Wildcard;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class QueryProgressIntegrationTests : BasicEntityTestBase
{
    [TestMethod]
    public void CompiledQuery_ReportsConsumedSourceRowsAndFinalSnapshot()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            ["#A"] =
            [
                new BasicEntity("001"),
                new BasicEntity("002"),
                new BasicEntity("003")
            ]
        };
        var snapshots = new List<QueryProgressEventArgs>();
        var telemetryOrder = new List<string>();
        var vm = CreateAndRunVirtualMachine(query, sources);
        vm.QueryProgress += (_, args) =>
        {
            snapshots.Add(args);
            telemetryOrder.Add(args.IsFinal ? "progress-final" : "progress");
        };
        vm.PhaseChanged += (_, args) => telemetryOrder.Add($"phase-{args.Phase}");

        var table = vm.Run(new QueryProgressOptions
        {
            RowsPerUpdate = 1,
            MinimumInterval = System.TimeSpan.FromDays(1)
        });

        Assert.AreEqual(3, table.Count);
        Assert.IsTrue(snapshots.Count >= 1);
        Assert.IsTrue(snapshots[^1].IsFinal);
        Assert.AreEqual(3, snapshots[^1].QueryRowsProcessed);
        Assert.IsNull(snapshots[^1].SourceContextId);
        Assert.IsTrue(snapshots.Exists(static snapshot => snapshot.SourceContextId != null));
        Assert.IsLessThan(
            telemetryOrder.IndexOf("phase-End"),
            telemetryOrder.IndexOf("progress-final"));
    }

    [TestMethod]
    public void CompiledQuery_ReportsConsumedRowsFromQueryScopedSourceChunks()
    {
        var snapshots = new List<QueryProgressEventArgs>();
        var query = InstanceCreator.CompileForExecution(
            "select Id from #wildcard.rows() a",
            "QueryProgress_QueryScoped",
            new ProjectionSensitiveWildcardSchemaProvider(
                new ProjectionSensitiveWildcardRecorder(),
                queryScopedRowsEnabled: true),
            new TestsLoggerResolver());
        query.QueryProgress += (_, args) => snapshots.Add(args);

        using var table = query.Run(new QueryProgressOptions
        {
            RowsPerUpdate = 1,
            MinimumInterval = System.TimeSpan.FromDays(1)
        });

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(snapshots.Any(static snapshot =>
            snapshot.SourceContextId != null && snapshot.SourceRowsProcessed == 1));
        Assert.IsTrue(snapshots[^1].IsFinal);
        Assert.AreEqual(1, snapshots[^1].QueryRowsProcessed);
    }

    [TestMethod]
    public async Task CompiledQueryAsync_ReportsFinalProgressWithPerRunOptions()
    {
        var snapshots = new List<QueryProgressEventArgs>();
        var query = InstanceCreator.CompileForExecution(
            "select Id from #wildcard.rows() a",
            "QueryProgress_Async",
            new ProjectionSensitiveWildcardSchemaProvider(
                new ProjectionSensitiveWildcardRecorder(),
                queryScopedRowsEnabled: true),
            new TestsLoggerResolver());
        query.QueryProgress += (_, args) => snapshots.Add(args);

        using var table = await query.RunAsync(new QueryProgressOptions
        {
            RowsPerUpdate = 1,
            MinimumInterval = System.TimeSpan.FromDays(1)
        });

        Assert.AreEqual(1, table.Count);
        Assert.IsTrue(snapshots.Count > 0);
        Assert.IsTrue(snapshots[^1].IsFinal);
        Assert.AreEqual(1, snapshots[^1].QueryRowsProcessed);
    }
}
