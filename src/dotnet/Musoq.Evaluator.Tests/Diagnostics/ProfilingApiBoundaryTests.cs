using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Runtime;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests.Diagnostics;

[TestClass]
public sealed class ProfilingApiBoundaryTests
{
    private static readonly Column[] Columns = [new("Value", typeof(string), 0)];

    [TestMethod]
    public void RunWithProfile_WhenRunnableIsNotProfiled_ShouldThrow()
    {
        var query = new CompiledQuery(new PlainRunnable());

        var exception = Assert.Throws<InvalidOperationException>(() => query.RunWithProfile(CancellationToken.None));

        StringAssert.Contains(exception.Message, "not compiled with profiling instrumentation");
    }

    [TestMethod]
    public void RunWithProfile_WhenTokenAlreadyCanceled_ShouldThrowBeforeProfilingCheck()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var query = new CompiledQuery(new PlainRunnable());

        Assert.Throws<OperationCanceledException>(() => query.RunWithProfile(cancellation.Token));
    }

    [TestMethod]
    public void RunWithProfile_WhenProfiledRunnableReturnsDeferredTable_ShouldMaterializePublicResult()
    {
        var factoryCalls = 0;
        var enumeratedRows = 0;
        var query = new CompiledQuery(new ProfiledDeferredRunnable((_, token, recorder) =>
        {
            using var scope = recorder.BeginOperator("op1", "FakeOperator");
            recorder.AddOperatorOutputRows("op1", 2);

            return QueryRows.DeferredTable("result", Columns, Rows, token);
        }));

        var profileResult = query.RunWithProfile(CancellationToken.None, emitTelemetry: false);

        Assert.AreEqual(1, factoryCalls);
        Assert.AreEqual(2, enumeratedRows);
        Assert.AreEqual(2, profileResult.Result.Count);
        Assert.AreEqual("a", profileResult.Result[0][0]);
        Assert.AreEqual("b", profileResult.Result[1][0]);
        StringAssert.Contains(profileResult.ProfileText, "Musoq query profile");
        StringAssert.Contains(profileResult.ProfileText, "op1 FakeOperator");

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
    public void QueryProfileTextPrinter_WhenSourcesAndOperatorsArePresent_ShouldFormatBoundaryStats()
    {
        var text = QueryProfileTextPrinter.Print(CreateProfileSnapshot());

        StringAssert.Contains(text, "Musoq query profile");
        StringAssert.Contains(text, "Total source rows: 5");
        StringAssert.Contains(text, "Source: people");
        StringAssert.Contains(text, "Rows read: 5");
        StringAssert.Contains(text, "Rows produced: 3");
        StringAssert.Contains(text, "Bytes read: 128");
        StringAssert.Contains(text, "Metric pages: 2");
        StringAssert.Contains(text, "Diagnosis: SourceBound");
        StringAssert.Contains(text, "op1 SourceScan: input rows=0, output rows=5, elapsed=3 ms");
        StringAssert.Contains(text, "op2 AppendShape: input rows=5, output rows=3, elapsed=1 ms");
    }

    [TestMethod]
    public void ExplainAnalyzeTextPrinter_WhenPlanHasOperatorIds_ShouldAnnotateMatchingLines()
    {
        const string plan = """
                            Physical plan
                              [op1] SourceScan people
                              [op2] AppendShape result
                              Constant line
                            """;

        var text = ExplainAnalyzeTextPrinter.Print(plan, CreateProfileSnapshot());

        StringAssert.Contains(text, "Musoq explain analyze");
        StringAssert.Contains(text, "[op1] SourceScan people  actual rows=5, elapsed=3 ms");
        StringAssert.Contains(text, "[op2] AppendShape result  actual rows=3, elapsed=1 ms");
        StringAssert.Contains(text, "Constant line");
        StringAssert.Contains(text, "Source boundary stats:");
        StringAssert.Contains(text, "Source: people");
        StringAssert.Contains(text, "MoveNext wait (estimated): 7 ms");
        StringAssert.Contains(text, "Consumer gap (estimated): 2 ms");
        StringAssert.Contains(text, "Diagnosis: SourceBound");
    }

    private static QueryProfileSnapshot CreateProfileSnapshot() =>
        new(
            TimeSpan.FromMilliseconds(12),
            [
                new SourceProfileSnapshot(
                    "people",
                    5,
                    TimeSpan.FromMilliseconds(1),
                    TimeSpan.FromMilliseconds(10),
                    TimeSpan.FromMilliseconds(7),
                    TimeSpan.FromMilliseconds(2),
                    0,
                    null,
                    null,
                    SourceProfileDiagnosis.SourceBound)
                {
                    RowsProduced = 3,
                    BytesRead = 128,
                    Metrics = new Dictionary<string, long>(StringComparer.Ordinal)
                    {
                        ["pages"] = 2
                    },
                    IsTimingEstimated = true
                }
            ],
            [
                new OperatorProfileSnapshot(
                    "op1",
                    "SourceScan",
                    0,
                    5,
                    TimeSpan.FromMilliseconds(3)),
                new OperatorProfileSnapshot(
                    "op2",
                    "AppendShape",
                    5,
                    3,
                    TimeSpan.FromMilliseconds(1))
            ]);

    private sealed class PlainRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
            = new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public event QueryPhaseEventHandler PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add { }
            remove { }
        }

        public Table Run(CancellationToken token)
        {
            return new Table("empty", []);
        }
    }

    private sealed class ProfiledDeferredRunnable(
        Func<ProfiledDeferredRunnable, CancellationToken, QueryProfileRecorder, Table> run)
        : ITableRunnable, IProfiledRunnable
    {
        public ISchemaProvider Provider { get; set; } = new ThrowingSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>> SourceRuntimeSettingDescriptionsBySourceContextId { get; set; }
            = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; }
            = new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public event QueryPhaseEventHandler PhaseChanged
        {
            add { }
            remove { }
        }

        public event DataSourceEventHandler DataSourceProgress
        {
            add { }
            remove { }
        }

        public Table Run(CancellationToken token)
        {
            return new Table("empty", []);
        }

        public Table RunWithProfile(CancellationToken token, QueryProfileRecorder profileRecorder)
        {
            return run(this, token, profileRecorder);
        }
    }

    private sealed class TestRow(string value) : Row
    {
        public override int Count => 1;

        public override object this[int columnNumber] => columnNumber == 0
            ? value
            : throw new IndexOutOfRangeException();
    }

    private sealed class ThrowingSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema)
        {
            throw new NotSupportedException("This runnable does not access schemas.");
        }
    }
}
