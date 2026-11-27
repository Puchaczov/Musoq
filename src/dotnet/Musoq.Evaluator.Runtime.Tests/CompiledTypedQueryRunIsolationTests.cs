using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class CompiledTypedQueryRunIsolationTests
{
    [TestMethod]
    public void Run_WithOptions_WhenParametersChangeAfterRun_ShouldUseSnapshot()
    {
        var query = new CompiledTypedQuery<string>(new ObservedTypedRunnable());
        var parameters = new Dictionary<string, object?> { ["value"] = "first" };
        var options = new TypedQueryRunOptions
        {
            TypedParameters = global::Musoq.Tests.Common.NativeParameterFixture.Capture(parameters),
        };

        var rows = query.Run(options);
        parameters["value"] = "second";

        CollectionAssert.AreEqual(new[] { "first" }, rows.ToArray());
    }

    [TestMethod]
    public void Run_WhenCompatibilityEventsChangeAfterRun_ShouldUseSnapshot()
    {
        var query = new CompiledTypedQuery<string>(new ObservedTypedRunnable());
        var firstPhaseCount = 0;
        var secondPhaseCount = 0;
        query.PhaseChanged += (_, _) => firstPhaseCount++;

        var rows = query.Run(CancellationToken.None);
        query.PhaseChanged += (_, _) => secondPhaseCount++;

        CollectionAssert.AreEqual(new[] { string.Empty }, rows.ToArray());
        Assert.AreEqual(1, firstPhaseCount);
        Assert.AreEqual(0, secondPhaseCount);
    }

    [TestMethod]
    public async Task Run_WhenEnumerationsOverlap_ShouldNotSerializeOneRunnable()
    {
        using var watchdog = new TestWatchdog();
        var runnable = new ObservedTypedRunnable();
        var query = new CompiledTypedQuery<string>(runnable);
        var firstRows = query.Run(
            new TypedQueryRunOptions(
                CancellationToken.None,
                global::Musoq.Tests.Common.NativeParameterFixture.Capture(
                    new Dictionary<string, object?> { ["value"] = "first" }
                )
            )
        );
        var secondRows = query.Run(
            new TypedQueryRunOptions(
                CancellationToken.None,
                global::Musoq.Tests.Common.NativeParameterFixture.Capture(
                    new Dictionary<string, object?> { ["value"] = "second" }
                )
            )
        );

        using var firstEnumerator = firstRows.GetEnumerator();
        Assert.IsTrue(firstEnumerator.MoveNext());
        Assert.AreEqual("first", firstEnumerator.Current);

        var secondTask = TestWatchdog.StartWorker(() => secondRows.ToArray());
        await TestWatchdog.WithCleanupAsync(
            async () =>
            {
                await watchdog.ReachedBeforeCompletionAsync(
                    runnable.SecondRunEntered,
                    secondTask,
                    "the second enumeration"
                );
                firstEnumerator.Dispose();
                await watchdog.WaitAsync(secondTask, "the second result");
                CollectionAssert.AreEqual(new[] { "second" }, await secondTask);
            },
            () =>
            {
                firstEnumerator.Dispose();
                return secondTask;
            }
        );
    }

    private sealed class ObservedTypedRunnable : ITypedRunnable<string>, IParameterizedRunnable
    {
        private readonly TaskCompletionSource _secondRunEntered = TestWatchdog.Signal();
        private QueryPhaseEventHandler? _phaseChanged;
        private DataSourceEventHandler? _dataSourceProgress;
        private int _runCount;

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

        public ILogger Logger { get; set; } = new NullLogger<object>();

        public IDictionary<string, object?> Parameters { get; } =
            new Dictionary<string, object?>(StringComparer.Ordinal);

        public IReadOnlyList<ScriptParameterDefinition> ParameterDefinitions { get; } = [];

        public IReadOnlyList<ScriptParameterContract> ParameterContracts =>
            ParameterDefinitions.Select(static definition => definition.Contract).ToArray();

        public event QueryPhaseEventHandler PhaseChanged
        {
            add => _phaseChanged += value;
            remove => _phaseChanged -= value;
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add => _dataSourceProgress += value;
            remove => _dataSourceProgress -= value;
        }

        public IEnumerable<string> Run(TypedQueryRunOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            if (Interlocked.Increment(ref _runCount) == 2)
                _secondRunEntered.SetResult();

            options.PhaseChanged?.Invoke(this, new QueryPhaseEventArgs("typed", QueryPhase.Select));
            var value = options.TypedParameters.Contains("value")
                ? options.TypedParameters.GetRequired<string>("value")
                : string.Empty;

            return [value];
        }

        public IEnumerable<string> Run(CancellationToken token)
        {
            return Run(
                new TypedQueryRunOptions(
                    token,
                    global::Musoq.Tests.Common.NativeParameterFixture.Capture(Parameters),
                    _phaseChanged,
                    _dataSourceProgress
                )
            );
        }

        internal Task SecondRunEntered => _secondRunEntered.Task;
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException("This runnable is metadata-only in these tests.");
        }
    }
}
