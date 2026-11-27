using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Components;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CompiledQueryAsyncExecutionTests
{
    [TestMethod]
    public async Task RunAsync_ShouldUseTrueAsyncRunnableWithoutCallingSynchronousRun()
    {
        using var watchdog = new TestWatchdog();
        using var runnable = new AsyncParameterizedRunnable();
        using var query = new CompiledQuery(runnable);
        global::Musoq.Tests.Common.NativeParameterFixture.Set(
            query.TypedParameters,
            query.ParameterDefinitions,
            "value",
            1
        );

        var execution = query.RunAsync().AsTask();
        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(runnable.Started.Task, execution, "the async runnable");
                Assert.Throws<InvalidOperationException>(() => query.TypedParameters.Set("value", 2));
            },
            () =>
            {
                runnable.Release.TrySetResult(true);
                return execution;
            }
        );
        using var result = await execution;

        Assert.AreEqual(1, runnable.AsyncRunCount);
        Assert.AreEqual(0, runnable.SynchronousRunCount);
        Assert.AreEqual(1, result[0].ReadValue(0));

        query.TypedParameters.Set("value", 2);
        using var next = await query.RunAsync();
        Assert.AreEqual(2, next[0].ReadValue(0));
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
        using var watchdog = new TestWatchdog();
        using var runnable = new AsyncParameterizedRunnable();
        using var query = new CompiledQuery(runnable);
        using var cancellation = new CancellationTokenSource();

        var execution = query.RunAsync(cancellation.Token).AsTask();
        var cancellationAssertion = AssertThrowsAsync<OperationCanceledException>(() => execution);
        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(runnable.Started.Task, execution, "the async runnable");
                cancellation.Cancel();
                await watchdog.WaitAsync(cancellationAssertion, "query cancellation");
            },
            () =>
            {
                cancellation.Cancel();
                runnable.Release.TrySetResult(true);
                return cancellationAssertion;
            }
        );
    }

    [TestMethod]
    public async Task Dispose_ShouldWaitForTrueAsyncExecutionAndRejectLaterRuns()
    {
        using var watchdog = new TestWatchdog();
        using var runnable = new AsyncParameterizedRunnable();
        var probe = new CompiledQueryDrainProbe(CompiledQuery.DrainStage.ActiveRuns, watchdog);
        using var query = new CompiledQuery(runnable) { DisposalWait = probe.Wait };
        global::Musoq.Tests.Common.NativeParameterFixture.Set(
            query.TypedParameters,
            query.ParameterDefinitions,
            "value",
            3
        );

        var execution = query.RunAsync().AsTask();
        Task? disposal = null;
        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(runnable.Started.Task, execution, "the async runnable");
                disposal = TestWatchdog.StartWorker(query.Dispose);
                await probe.ObserveAsync(disposal);
                runnable.Release.TrySetResult(true);
                await watchdog.WaitAsync(Task.WhenAll(execution, disposal), "execution and disposal");
            },
            () =>
            {
                runnable.Release.TrySetResult(true);
                return Task.WhenAll(execution, disposal ?? Task.CompletedTask);
            }
        );
        using var result = await execution;

        Assert.IsFalse(runnable.DisposedBeforeExecutionCompleted);
        await AssertThrowsAsync<ObjectDisposedException>(() => query.RunAsync().AsTask());
    }

    private abstract class RunnableBase : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<
            string,
            IReadOnlyDictionary<string, string>
        > SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<
            string,
            IReadOnlyList<SourceRuntimeSettingDescription>
        > SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
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

    private sealed class AsyncParameterizedRunnable
        : RunnableBase,
            IAsyncTableRunnable,
            IParameterizedRunnable,
            ITypedParameterizedRunnable,
            IDisposable
    {
        internal readonly TaskCompletionSource Started = TestWatchdog.Signal();
        internal readonly TaskCompletionSource<bool> Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _executionCompleted;
        private int _disposedBeforeExecutionCompleted;

        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

        public int AsyncRunCount { get; private set; }

        public int SynchronousRunCount { get; private set; }

        public bool DisposedBeforeExecutionCompleted => Volatile.Read(ref _disposedBeforeExecutionCompleted) != 0;

        public override Table Run(CancellationToken token)
        {
            SynchronousRunCount++;
            return CreateTable(TypedParameters.GetRequired<int>("value"));
        }

        public async ValueTask<Table> RunAsync(CancellationToken token)
        {
            AsyncRunCount++;
            Started.TrySetResult();
            try
            {
                await Release.Task.WaitAsync(token);
                return CreateTable(TypedParameters.GetRequired<int>("value"));
            }
            finally
            {
                Volatile.Write(ref _executionCompleted, 1);
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _executionCompleted) == 0)
                Volatile.Write(ref _disposedBeforeExecutionCompleted, 1);
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
        public override void VisitColumns<TVisitor>(ref TVisitor visitor) =>
            TypedColumnVisit.Apply(0, value, ref visitor);

        public override int Count => 1;
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
