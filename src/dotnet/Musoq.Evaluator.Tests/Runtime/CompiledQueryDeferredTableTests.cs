using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public sealed class CompiledQueryDeferredTableTests
{
    private static readonly Column[] Columns = [new("Value", typeof(string), 0)];

    [TestMethod]
    public void Run_WhenRunnableReturnsDeferredTable_ShouldMaterializeOnlyOnFirstAccess()
    {
        var factoryCalls = 0;
        var enumeratedRows = 0;
        var query = new CompiledQuery(new DeferredTableRunnable((_, token) =>
            QueryRows.DeferredTable("result", Columns, Rows, token)));

        var table = query.Run(CancellationToken.None);

        Assert.AreEqual(0, factoryCalls);
        Assert.AreEqual(0, enumeratedRows);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        Assert.AreEqual("a", table[0][0]);

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        return;

        IEnumerable<TestRow> Rows(CancellationToken token)
        {
            factoryCalls++;
            token.ThrowIfCancellationRequested();
            enumeratedRows++;
            yield return new TestRow("a");
            enumeratedRows++;
            yield return new TestRow("b");
        }
    }

    [TestMethod]
    public void Run_WhenTokenIsCancelledAfterRun_ShouldThrowOnFirstAccess()
    {
        using var cancellation = new CancellationTokenSource();
        var query = new CompiledQuery(new DeferredTableRunnable((_, token) =>
            QueryRows.DeferredTable("result", Columns, Rows, token)));

        var table = query.Run(cancellation.Token);
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => _ = table.Count);
        return;

        static IEnumerable<TestRow> Rows(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            yield return new TestRow("a");
        }
    }

    [TestMethod]
    public void Run_WhenDeferredScriptParameterBindingFails_ShouldWrapOnFirstAccess()
    {
        var query = new CompiledQuery(new DeferredTableRunnable((_, token) =>
            QueryRows.DeferredTable("result", Columns, Rows, token)));

        var table = query.Run(CancellationToken.None);
        var exception = Assert.Throws<QueryExecutionException>(() => _ = table.Count);

        Assert.IsInstanceOfType<ScriptParameterBindingException>(exception.InnerException);
        return;

        static IEnumerable<TestRow> Rows(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            throw ScriptParameterBindingException.MissingRequired("p");
        }
    }

    [TestMethod]
    public void Run_WhenDeferredRowsRaiseEvents_ShouldForwardThemAtMaterializationTime()
    {
        var query = new CompiledQuery(new DeferredTableRunnable((runnable, token) =>
            QueryRows.DeferredTable("result", Columns, rowToken => Rows(runnable, rowToken), token)));
        var phaseEvents = 0;
        var dataSourceEvents = 0;
        query.PhaseChanged += (_, args) =>
        {
            if (args.Phase == QueryPhase.Select)
                phaseEvents++;
        };
        query.DataSourceProgress += (_, args) =>
        {
            if (args.Phase == DataSourcePhase.RowsRead)
                dataSourceEvents++;
        };

        var table = query.Run(CancellationToken.None);
        Assert.AreEqual(0, phaseEvents);
        Assert.AreEqual(0, dataSourceEvents);

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, phaseEvents);
        Assert.AreEqual(1, dataSourceEvents);
        return;

        static IEnumerable<TestRow> Rows(DeferredTableRunnable runnable, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            runnable.RaisePhase(QueryPhase.Select);
            runnable.RaiseDataSourceProgress(DataSourcePhase.RowsRead);
            yield return new TestRow("a");
        }
    }

    private sealed class DeferredTableRunnable(Func<DeferredTableRunnable, CancellationToken, Table> run) : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = null!;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
            = new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;

        public Table Run(CancellationToken token)
        {
            return run(this, token);
        }

        public void RaisePhase(QueryPhase phase)
        {
            PhaseChanged?.Invoke(this, new QueryPhaseEventArgs("query", phase));
        }

        public void RaiseDataSourceProgress(DataSourcePhase phase)
        {
            DataSourceProgress?.Invoke(this, new DataSourceEventArgs("query", "source", phase));
        }
    }

    private sealed class TestRow(string value) : Row
    {
        public override int Count => 1;

        public override object this[int columnNumber] => columnNumber == 0
            ? value
            : throw new IndexOutOfRangeException();
    }
}
