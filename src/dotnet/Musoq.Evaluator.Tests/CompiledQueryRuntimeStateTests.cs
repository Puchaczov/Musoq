using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CompiledQueryRuntimeStateTests
{
    [TestMethod]
    public async Task ConcurrentRuns_ShouldUseIndependentParameterSnapshots()
    {
        using var runnable = new BlockingParameterizedRunnable();
        using var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 1;

        var firstRun = Task.Run(() => query.Run());
        Assert.IsTrue(runnable.FirstRunStarted.Wait(TimeSpan.FromSeconds(5)));

        var secondRun = Task.Run(() =>
        {
            query.Parameters["value"] = 2;
            return query.Run();
        });

        Assert.IsFalse(secondRun.Wait(TimeSpan.FromMilliseconds(100)));
        runnable.Release.Set();

        var firstResult = await firstRun;
        var secondResult = await secondRun;

        Assert.AreEqual(1, firstResult[0][0]);
        Assert.AreEqual(2, secondResult[0][0]);
    }

    [TestMethod]
    public async Task Dispose_ShouldWaitForAnActiveRunAndRejectLaterRuns()
    {
        using var runnable = new BlockingParameterizedRunnable();
        var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 3;

        var run = Task.Run(() => query.Run());
        Assert.IsTrue(runnable.FirstRunStarted.Wait(TimeSpan.FromSeconds(5)));

        var dispose = Task.Run(query.Dispose);
        Assert.IsFalse(dispose.Wait(TimeSpan.FromMilliseconds(100)));

        runnable.Release.Set();
        await run;
        await dispose;

        Assert.IsTrue(runnable.IsDisposed);
        Assert.ThrowsExactly<ObjectDisposedException>(() => query.Run());
    }

    [TestMethod]
    public async Task ContextualRuns_ShouldExecuteConcurrentlyWithIndependentSnapshots()
    {
        using var runnable = new BlockingContextualRunnable();
        using var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 1;

        var firstRun = Task.Run(() => query.Run());
        Assert.IsTrue(runnable.FirstRunStarted.Wait(TimeSpan.FromSeconds(5)));

        var secondRun = Task.Run(() =>
        {
            query.Parameters["value"] = 2;
            return query.Run();
        });

        Assert.IsTrue(runnable.SecondRunStarted.Wait(TimeSpan.FromSeconds(5)));
        Assert.AreEqual(2, runnable.MaximumConcurrentRuns);

        runnable.Release.Set();
        var firstResult = await firstRun;
        var secondResult = await secondRun;

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, new[] { firstResult[0][0], secondResult[0][0] });
    }

    [TestMethod]
    public async Task Dispose_ShouldWaitForConcurrentContextualRuns()
    {
        using var runnable = new BlockingContextualRunnable();
        var query = new CompiledQuery(runnable);
        query.Parameters["value"] = 3;

        var firstRun = Task.Run(() => query.Run());
        Assert.IsTrue(runnable.FirstRunStarted.Wait(TimeSpan.FromSeconds(5)));
        var secondRun = Task.Run(() => query.Run());
        Assert.IsTrue(runnable.SecondRunStarted.Wait(TimeSpan.FromSeconds(5)));

        var dispose = Task.Run(query.Dispose);
        Assert.IsFalse(dispose.Wait(TimeSpan.FromMilliseconds(100)));

        runnable.Release.Set();
        await firstRun;
        await secondRun;
        await dispose;

        Assert.IsTrue(runnable.IsDisposed);
    }

    private sealed class BlockingParameterizedRunnable : ITableRunnable, IParameterizedRunnable, IDisposable
    {
        internal readonly ManualResetEventSlim FirstRunStarted = new(false);
        internal readonly ManualResetEventSlim Release = new(false);

        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

        #pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;
        #pragma warning restore CS0067

        public bool IsDisposed { get; private set; }

        public Table Run(CancellationToken token)
        {
            FirstRunStarted.Set();
            Release.Wait(token);

            var value = (int)Parameters["value"]!;
            var table = new Table("result", [new Column("Value", typeof(int), 0)]);
            table.Add(new ValueRow(value));
            return table;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            FirstRunStarted.Dispose();
            Release.Dispose();
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
                throw new NotSupportedException("Runtime-state test does not use a schema provider.");
        }
    }

    private sealed class BlockingContextualRunnable : IContextTableRunnable, IParameterizedRunnable, IDisposable
    {
        private int _activeRuns;
        private int _maximumConcurrentRuns;

        internal readonly ManualResetEventSlim FirstRunStarted = new(false);
        internal readonly ManualResetEventSlim SecondRunStarted = new(false);
        internal readonly ManualResetEventSlim Release = new(false);

        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public IDictionary<string, object?> Parameters { get; } = new Dictionary<string, object?>(StringComparer.Ordinal);

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts { get; } = [];

        #pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;

        public event DataSourceEventHandler? DataSourceProgress;
        #pragma warning restore CS0067

        public bool IsDisposed { get; private set; }

        public int MaximumConcurrentRuns => Volatile.Read(ref _maximumConcurrentRuns);

        public Table Run(CancellationToken token) =>
            throw new InvalidOperationException("The contextual contract must be used.");

        public Table Run(QueryRunContext context)
        {
            var active = Interlocked.Increment(ref _activeRuns);
            UpdateMaximum(active);
            if (active == 1)
                FirstRunStarted.Set();
            if (active == 2)
                SecondRunStarted.Set();

            try
            {
                Release.Wait(context.CancellationToken);
                var value = context.RuntimeParameters.TryGetValue("value", out var parameter)
                    ? (int)parameter!
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
            IsDisposed = true;
        }

        private void UpdateMaximum(int active)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrentRuns);
                if (active <= current || Interlocked.CompareExchange(ref _maximumConcurrentRuns, active, current) == current)
                    return;
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
                throw new NotSupportedException("Runtime-state test does not use a schema provider.");
        }
    }
}
