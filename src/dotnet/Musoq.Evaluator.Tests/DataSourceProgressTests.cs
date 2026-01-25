using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Evaluator.Tests.Schema.DataSourceProgress;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public class DataSourceProgressTests : BasicEntityTestBase
{
    [TestMethod]
    public void RuntimeContext_WithCallback_InvokesCallback()
    {
        var events = new List<DataSourceEventArgs>();

        var runtimeContext = new RuntimeContext(
            "test_query",
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            (null, [], null, false),
            null,
            Callback);

        runtimeContext.ReportDataSourceBegin("TestSource");
        runtimeContext.ReportDataSourceRowsKnown("TestSource", 100);
        runtimeContext.ReportDataSourceRowsRead("TestSource", 50, 100);
        runtimeContext.ReportDataSourceEnd("TestSource", 100);

        Assert.HasCount(4, events);
        Assert.AreEqual(DataSourcePhase.Begin, events[0].Phase);
        Assert.AreEqual(DataSourcePhase.RowsKnown, events[1].Phase);
        Assert.AreEqual(DataSourcePhase.RowsRead, events[2].Phase);
        Assert.AreEqual(DataSourcePhase.End, events[3].Phase);

        Assert.AreEqual("TestSource", events[0].DataSourceName);
        Assert.AreEqual(100, events[1].TotalRows);
        Assert.AreEqual(50, events[2].RowsProcessed);
        Assert.AreEqual(100, events[3].TotalRows);
        return;

        void Callback(object sender, DataSourceEventArgs args)
        {
            events.Add(args);
        }
    }

    [TestMethod]
    public void Integration_DataSourceReportsAllPhases()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002"), new BasicEntity("003")] }
        };

        var events =
            new List<(DataSourcePhase Phase, string DataSourceName, string QueryId, long? TotalRows, long? RowsProcessed
                )>();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ReportingSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        vm.DataSourceProgress += (sender, args) =>
        {
            events.Add((args.Phase, args.DataSourceName, args.QueryId, args.TotalRows, args.RowsProcessed));
        };

        var result = vm.Run();

        Assert.AreEqual(3, result.Count);

        Assert.IsNotEmpty(events, "Should have received data source progress events");

        var beginEvents = events.Where(e => e.Phase == DataSourcePhase.Begin).ToList();
        var rowsKnownEvents = events.Where(e => e.Phase == DataSourcePhase.RowsKnown).ToList();
        var rowsReadEvents = events.Where(e => e.Phase == DataSourcePhase.RowsRead).ToList();
        var endEvents = events.Where(e => e.Phase == DataSourcePhase.End).ToList();

        Assert.HasCount(1, beginEvents, "Should have exactly one Begin event");
        Assert.HasCount(1, rowsKnownEvents, "Should have exactly one RowsKnown event");
        Assert.HasCount(3, rowsReadEvents, "Should have RowsRead event for each row");
        Assert.HasCount(1, endEvents, "Should have exactly one End event");

        Assert.AreEqual("#A.Entities", beginEvents[0].DataSourceName);
        Assert.IsFalse(string.IsNullOrEmpty(beginEvents[0].QueryId), "QueryId should be populated in Begin event");
        Assert.AreEqual(3, rowsKnownEvents[0].TotalRows);
        Assert.IsFalse(string.IsNullOrEmpty(rowsKnownEvents[0].QueryId),
            "QueryId should be populated in RowsKnown event");
        Assert.AreEqual(1, rowsReadEvents[0].RowsProcessed);
        Assert.IsFalse(string.IsNullOrEmpty(rowsReadEvents[0].QueryId),
            "QueryId should be populated in RowsRead event");
        Assert.AreEqual(2, rowsReadEvents[1].RowsProcessed);
        Assert.AreEqual(3, rowsReadEvents[2].RowsProcessed);
        Assert.AreEqual(3, endEvents[0].TotalRows);
        Assert.IsFalse(string.IsNullOrEmpty(endEvents[0].QueryId), "QueryId should be populated in End event");
    }

    [TestMethod]
    public void Integration_DataSourceReportsInCorrectOrder()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] }
        };

        var phases = new List<DataSourcePhase>();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ReportingSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        vm.DataSourceProgress += (sender, args) => phases.Add(args.Phase);

        vm.Run();

        Assert.IsGreaterThanOrEqualTo(4, phases.Count, "Should have at least 4 events");

        var beginIndex = phases.IndexOf(DataSourcePhase.Begin);
        var rowsKnownIndex = phases.IndexOf(DataSourcePhase.RowsKnown);
        var firstRowsReadIndex = phases.IndexOf(DataSourcePhase.RowsRead);
        var endIndex = phases.IndexOf(DataSourcePhase.End);

        Assert.IsGreaterThanOrEqualTo(0, beginIndex, "Should have Begin phase");
        Assert.IsGreaterThan(beginIndex, rowsKnownIndex, "RowsKnown should come after Begin");
        Assert.IsGreaterThan(rowsKnownIndex, firstRowsReadIndex, "RowsRead should come after RowsKnown");
        Assert.IsGreaterThan(firstRowsReadIndex, endIndex, "End should come after RowsRead");
    }

    [TestMethod]
    public void Integration_EmptyDataSource_ReportsCorrectly()
    {
        var query = "select Name from #A.Entities()";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [] }
        };

        var events = new List<DataSourceEventArgs>();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ReportingSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        vm.DataSourceProgress += (sender, args) => events.Add(args);

        var result = vm.Run();

        Assert.AreEqual(0, result.Count);

        var beginEvents = events.Where(e => e.Phase == DataSourcePhase.Begin).ToList();
        var rowsKnownEvents = events.Where(e => e.Phase == DataSourcePhase.RowsKnown).ToList();
        var rowsReadEvents = events.Where(e => e.Phase == DataSourcePhase.RowsRead).ToList();
        var endEvents = events.Where(e => e.Phase == DataSourcePhase.End).ToList();

        Assert.HasCount(1, beginEvents);
        Assert.HasCount(1, rowsKnownEvents);
        Assert.IsEmpty(rowsReadEvents, "Empty source should have no RowsRead events");
        Assert.HasCount(1, endEvents);

        Assert.AreEqual(0, rowsKnownEvents[0].TotalRows);
        Assert.AreEqual(0, endEvents[0].TotalRows);
    }

    [TestMethod]
    public void Integration_MultipleDataSources_ReportSeparately()
    {
        var query = "select a.Name from #A.Entities() a inner join #B.Entities() b on a.Name = b.Name";
        var sources = new Dictionary<string, IEnumerable<BasicEntity>>
        {
            { "#A", [new BasicEntity("001"), new BasicEntity("002")] },
            { "#B", [new BasicEntity("001")] }
        };

        var eventsBySource = new Dictionary<string, List<DataSourceEventArgs>>();

        var vm = InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            new ReportingSchemaProvider<BasicEntity>(sources),
            LoggerResolver);

        vm.DataSourceProgress += (sender, args) =>
        {
            if (!eventsBySource.ContainsKey(args.DataSourceName))
                eventsBySource[args.DataSourceName] = [];
            eventsBySource[args.DataSourceName].Add(args);
        };

        vm.Run();

        Assert.HasCount(2, eventsBySource, "Should have events from exactly two data sources");

        foreach (var kvp in eventsBySource)
        {
            var events = kvp.Value;
            Assert.IsTrue(events.Any(e => e.Phase == DataSourcePhase.Begin),
                $"Data source {kvp.Key} should have Begin event");
            Assert.IsTrue(events.Any(e => e.Phase == DataSourcePhase.End),
                $"Data source {kvp.Key} should have End event");
        }
    }
}
