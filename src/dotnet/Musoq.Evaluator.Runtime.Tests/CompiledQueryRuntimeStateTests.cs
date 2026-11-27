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
public sealed class CompiledQueryRuntimeStateTests
{
    [TestMethod]
    public async Task ConcurrentRuns_ShouldUseIndependentParameterSnapshots()
    {
        using var watchdog = new TestWatchdog();
        using var runnable = new BlockingParameterizedRunnable();
        var secondWaiting = TestWatchdog.Signal();
        using var query = new CompiledQuery(runnable)
        {
            SynchronousAdmissionWait = (gate, token) =>
            {
                if (gate.Wait(0, token))
                    return;

                secondWaiting.SetResult();
                gate.Wait(token);
            },
        };
        SetValue(query, 1);
        var first = TestWatchdog.StartWorker(() => query.Run());
        Task<Table>? second = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(runnable.FirstRunStarted.Task, first, "the first run");
                second = StartWithValue(query, 2);
                await watchdog.ReachedBeforeCompletionAsync(secondWaiting.Task, second, "the second run admission");
                runnable.Release.TrySetResult();
                await watchdog.WaitAsync(Task.WhenAll(first, second), "both parameterized runs");
                using var firstResult = await first;
                using var secondResult = await second;
                Assert.AreEqual(1, firstResult[0].ReadValue(0));
                Assert.AreEqual(2, secondResult[0].ReadValue(0));
            },
            () =>
            {
                runnable.Release.TrySetResult();
                return Task.WhenAll(first, second ?? Task.CompletedTask);
            }
        );
    }

    [TestMethod]
    public async Task Dispose_ShouldWaitForAnActiveRunAndRejectLaterRuns()
    {
        using var watchdog = new TestWatchdog();
        using var runnable = new BlockingParameterizedRunnable();
        var probe = new CompiledQueryDrainProbe(CompiledQuery.DrainStage.ActiveRuns, watchdog);
        using var query = new CompiledQuery(runnable) { DisposalWait = probe.Wait };
        SetValue(query, 3);
        var run = TestWatchdog.StartWorker(() => query.Run());
        Task? disposal = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(runnable.FirstRunStarted.Task, run, "the active run");
                disposal = TestWatchdog.StartWorker(query.Dispose);
                await probe.ObserveAsync(disposal);
                runnable.Release.TrySetResult();
                await watchdog.WaitAsync(Task.WhenAll(run, disposal), "the run and disposal");
                using var result = await run;

                Assert.IsFalse(runnable.DisposeObservedActiveRun);
                Assert.IsTrue(runnable.IsDisposed);
                Assert.ThrowsExactly<ObjectDisposedException>(() => query.Run());
            },
            () =>
            {
                runnable.Release.TrySetResult();
                return Task.WhenAll(run, disposal ?? Task.CompletedTask);
            }
        );
    }

    [TestMethod]
    public Task ContextualRuns_ShouldExecuteConcurrentlyWithIndependentSnapshots() =>
        AssertContextualRuns(dispose: false);

    [TestMethod]
    public Task Dispose_ShouldWaitForConcurrentContextualRuns() => AssertContextualRuns(dispose: true);

    private static async Task AssertContextualRuns(bool dispose)
    {
        using var watchdog = new TestWatchdog();
        using var runnable = new BlockingContextualRunnable();
        var probe = new CompiledQueryDrainProbe(CompiledQuery.DrainStage.ActiveRuns, watchdog);
        using var query = new CompiledQuery(runnable) { DisposalWait = dispose ? probe.Wait : null };
        SetValue(query, 1);
        var first = TestWatchdog.StartWorker(() => query.Run());
        Task<Table>? second = null;
        Task? disposal = null;

        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(
                    runnable.FirstRunStarted.Task,
                    first,
                    "the first contextual run"
                );
                second = StartWithValue(query, 2);
                await watchdog.ReachedBeforeCompletionAsync(
                    runnable.SecondRunStarted.Task,
                    second,
                    "the second contextual run"
                );
                Assert.AreEqual(2, runnable.MaximumConcurrentRuns);

                if (dispose)
                {
                    disposal = TestWatchdog.StartWorker(query.Dispose);
                    await probe.ObserveAsync(disposal);
                }

                runnable.Release.TrySetResult();
                await watchdog.WaitAsync(
                    Task.WhenAll(first, second, disposal ?? Task.CompletedTask),
                    "the contextual runs"
                );
                using var firstResult = await first;
                using var secondResult = await second;
                CollectionAssert.AreEquivalent(
                    new[] { 1, 2 },
                    new[] { firstResult[0].ReadValue(0), secondResult[0].ReadValue(0) }
                );

                if (dispose)
                {
                    Assert.IsFalse(runnable.DisposeObservedActiveRun);
                    Assert.IsTrue(runnable.IsDisposed);
                    Assert.ThrowsExactly<ObjectDisposedException>(() => query.Run());
                }
            },
            () =>
            {
                runnable.Release.TrySetResult();
                return Task.WhenAll(first, second ?? Task.CompletedTask, disposal ?? Task.CompletedTask);
            }
        );
    }

    private static void SetValue(CompiledQuery query, int value) =>
        NativeParameterFixture.Set(query.TypedParameters, query.ParameterDefinitions, "value", value);

    private static Task<Table> StartWithValue(CompiledQuery query, int value) =>
        TestWatchdog.StartWorker(() =>
        {
            var values = new TypedParameterValues();
            values.Set("value", value);
            return query.Run(new TypedQueryRunOptions { TypedParameters = values.Capture() });
        });

    private sealed class BlockingParameterizedRunnable
        : ITableRunnable,
            IParameterizedRunnable,
            ITypedParameterizedRunnable,
            IDisposable
    {
        internal readonly TaskCompletionSource FirstRunStarted = TestWatchdog.Signal();
        internal readonly TaskCompletionSource Release = TestWatchdog.Signal();
        private int _activeRuns;
        private int _disposeObservedActiveRun;

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

        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

#pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;
#pragma warning restore CS0067

        public bool IsDisposed { get; private set; }

        public bool DisposeObservedActiveRun => Volatile.Read(ref _disposeObservedActiveRun) != 0;

        public Table Run(CancellationToken token)
        {
            var runNumber = Interlocked.Increment(ref _activeRuns);
            if (runNumber == 1)
                FirstRunStarted.TrySetResult();

            try
            {
                Release.Task.WaitAsync(token).GetAwaiter().GetResult();

                var value = TypedParameters.GetRequired<int>("value");
                var table = new Table("result", [new Column("Value", typeof(int), 0)]);
                table.Add(new ValueRow(value));
                return table;
            }
            finally
            {
                Interlocked.Decrement(ref _activeRuns);
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _activeRuns) != 0)
                Volatile.Write(ref _disposeObservedActiveRun, 1);
            if (IsDisposed)
                return;

            IsDisposed = true;
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
                throw new NotSupportedException("Runtime-state test does not use a schema provider.");
        }
    }

    private sealed class BlockingContextualRunnable
        : IContextTableRunnable,
            IParameterizedRunnable,
            ITypedParameterizedRunnable,
            IDisposable
    {
        private int _activeRuns;
        private int _maximumConcurrentRuns;
        private int _disposeObservedActiveRun;

        internal readonly TaskCompletionSource FirstRunStarted = TestWatchdog.Signal();
        internal readonly TaskCompletionSource SecondRunStarted = TestWatchdog.Signal();
        internal readonly TaskCompletionSource Release = TestWatchdog.Signal();

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

        public TypedParameterSnapshot TypedParameters { get; set; } = TypedParameterSnapshot.Empty;

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

#pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;
#pragma warning restore CS0067

        public bool IsDisposed { get; private set; }

        public bool DisposeObservedActiveRun => Volatile.Read(ref _disposeObservedActiveRun) != 0;

        public int MaximumConcurrentRuns => Volatile.Read(ref _maximumConcurrentRuns);

        public Table Run(CancellationToken token) =>
            throw new InvalidOperationException("The contextual contract must be used.");

        public Table Run(QueryRunContext context)
        {
            var active = Interlocked.Increment(ref _activeRuns);
            UpdateMaximum(active);
            if (active == 1)
                FirstRunStarted.TrySetResult();
            if (active == 2)
                SecondRunStarted.TrySetResult();

            try
            {
                Release.Task.WaitAsync(context.CancellationToken).GetAwaiter().GetResult();
                var value = context.TypedParameters.Contains("value")
                    ? context.TypedParameters.GetRequired<int>("value")
                    : 0;
                var table = new Table("result", [new Column("Value", typeof(int), 0)]);
                table.Add(new ValueRow(value));
                return table;
            }
            finally
            {
                Interlocked.Decrement(ref _activeRuns);
            }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _activeRuns) != 0)
                Volatile.Write(ref _disposeObservedActiveRun, 1);
            IsDisposed = true;
        }

        private void UpdateMaximum(int active)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentRuns);
                if (
                    active <= current
                    || Interlocked.CompareExchange(ref _maximumConcurrentRuns, active, current) == current
                )
                    return;
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
                throw new NotSupportedException("Runtime-state test does not use a schema provider.");
        }
    }
}
