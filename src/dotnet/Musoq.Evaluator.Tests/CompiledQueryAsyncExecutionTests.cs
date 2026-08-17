using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CompiledQueryAsyncExecutionTests
{
    [TestMethod]
    public async Task RunAsync_ShouldUseTrueAsyncRunnableWithoutCallingSynchronousRun()
    {
        using var runnable = new AsyncParameterizedRunnable();
        using var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 1;

        var execution = query.RunAsync().AsTask();
        Assert.IsTrue(runnable.Started.Wait(TimeSpan.FromSeconds(5)));

        query.Parameters["value"] = 2;
        runnable.Release.TrySetResult(true);

        var result = await execution;

        Assert.AreEqual(1, runnable.AsyncRunCount);
        Assert.AreEqual(0, runnable.SynchronousRunCount);
        Assert.AreEqual(1, result[0][0]);

        var next = await query.RunAsync();
        Assert.AreEqual(2, next[0][0]);
    }

    [TestMethod]
    public async Task RunAsync_ShouldUseLegacySynchronousFallbackWhenNoAsyncContractExists()
    {
        var runnable = new LegacyRunnable();
        using var query = new CompiledQuery(runnable);

        var result = await query.RunAsync();

        Assert.IsNotNull(result);
        Assert.AreEqual(1, runnable.RunCount);
    }

    [TestMethod]
    public async Task RunAsync_ShouldPreserveCancellationDuringTrueAsyncExecution()
    {
        using var runnable = new AsyncParameterizedRunnable();
        using var query = new CompiledQuery(runnable);
        using var cancellation = new CancellationTokenSource();

        var execution = query.RunAsync(cancellation.Token).AsTask();
        Assert.IsTrue(runnable.Started.Wait(TimeSpan.FromSeconds(5)));
        cancellation.Cancel();

        await AssertThrowsAsync<OperationCanceledException>(() => execution);
    }

    [TestMethod]
    public async Task Dispose_ShouldWaitForTrueAsyncExecutionAndRejectLaterRuns()
    {
        using var runnable = new AsyncParameterizedRunnable();
        var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 3;

        var execution = query.RunAsync().AsTask();
        Assert.IsTrue(runnable.Started.Wait(TimeSpan.FromSeconds(5)));

        var dispose = Task.Run(query.Dispose);
        Assert.IsFalse(dispose.Wait(TimeSpan.FromMilliseconds(100)));

        runnable.Release.TrySetResult(true);
        await execution;
        await dispose;

        await AssertThrowsAsync<ObjectDisposedException>(() => query.RunAsync().AsTask());
    }

    private abstract class RunnableBase : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public event QueryPhaseEventHandler? PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler? DataSourceProgress
        {
            add { }
            remove { }
        }

        protected static Table CreateTable(int value)
        {
            var table = new Table("result", [new Column("Value", typeof(int), 0)]);
            table.Add(new ValueRow(value));
            return table;
        }

        public abstract Table Run(CancellationToken token);
    }

    private sealed class AsyncParameterizedRunnable : RunnableBase, IAsyncTableRunnable, IParameterizedRunnable, IDisposable
    {
        internal readonly ManualResetEventSlim Started = new(false);
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

        public int AsyncRunCount { get; private set; }

        public int SynchronousRunCount { get; private set; }

        public override Table Run(CancellationToken token)
        {
            SynchronousRunCount++;
            return CreateTable((int)Parameters["value"]!);
        }

        public async ValueTask<Table> RunAsync(CancellationToken token)
        {
            AsyncRunCount++;
            Started.Set();
            await Release.Task.WaitAsync(token);
            return CreateTable((int)Parameters["value"]!);
        }

        public void Dispose()
        {
            Started.Dispose();
        }
    }

    private sealed class LegacyRunnable : RunnableBase
    {
        public int RunCount { get; private set; }

        public override Table Run(CancellationToken token)
        {
            RunCount++;
            return new Table("empty", []);
        }
    }

    private sealed class ValueRow(int value) : Row
    {
        public override int Count => 1;

        public override object this[int columnNumber] => columnNumber == 0
            ? value
            : throw new IndexOutOfRangeException();
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) =>
            throw new NotSupportedException("This runnable does not expose a schema.");
    }

    private static async Task AssertThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        Assert.Fail($"Expected {typeof(TException).Name}.");
    }
}
