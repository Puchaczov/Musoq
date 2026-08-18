using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter.Tests.Components;
using Musoq.Evaluator;
using Musoq.Evaluator.Exceptions;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Exceptions;

namespace Musoq.Converter.Tests;

[TestClass]
public sealed class QueryScopedRowLifecycleTests
{
    private readonly TestsLoggerResolver _loggerResolver = new();

    [TestMethod]
    [DataRow(QueryRowLifecycleReaderStyle.Ordinal)]
    [DataRow(QueryRowLifecycleReaderStyle.Property)]
    [DataRow(QueryRowLifecycleReaderStyle.Path)]
    public void CompiledQuery_WhenReaderStyleChanges_ShouldMaterializeNullableAndMissingFields(
        QueryRowLifecycleReaderStyle readerStyle)
    {
        var scenario = new QueryRowLifecycleScenario
        {
            ReaderStyle = readerStyle,
            Rows =
            [
                new QueryRowLifecycleRawRow(1, "first", "a", 10, "present"),
                new QueryRowLifecycleRawRow(2, "second", "b", null, null, omitNullableFields: true)
            ]
        };
        using var compiled = Compile(
            "select r.Id, r.Label, r.MaybeNumber, r.MaybeText from #lifecycle.rows() r order by r.Id",
            scenario);

        var rows = Execute(compiled);

        Assert.HasCount(2, rows);
        CollectionAssert.AreEqual(new object?[] { 1, "first", 10, "present" }, rows[0]);
        CollectionAssert.AreEqual(new object?[] { 2, "second", null, null }, rows[1]);
        Assert.AreEqual(2, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(8, scenario.Recorder.FieldReads);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
    }

    [TestMethod]
    public void CompiledQuery_WhenPredicateAndTakeArePushed_ShouldFilterBeforeMaterializingAndStopUpstream()
    {
        var scenario = new QueryRowLifecycleScenario
        {
            ReaderStyle = QueryRowLifecycleReaderStyle.Ordinal,
            Rows = CreateNumberedRows(20),
            AcceptPredicate = true,
            AcceptTake = true,
            PushedPredicate = static row => row.Id >= 8,
            ChunkSize = 4
        };
        using var compiled = Compile(
            "select r.Id from #lifecycle.rows() r where r.Id >= 8 take 2",
            scenario);
        var progress = new List<DataSourceEventArgs>();
        compiled.DataSourceProgress += (_, args) => progress.Add(args);

        var rows = Execute(compiled);

        Assert.HasCount(2, rows);
        Assert.AreEqual(8, rows[0][0]);
        Assert.AreEqual(9, rows[1][0]);
        Assert.AreEqual(10, scenario.Recorder.RawRowsEnumerated);
        Assert.AreEqual(8, scenario.Recorder.RowsRejected);
        Assert.AreEqual(2, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(2, scenario.Recorder.RowsProduced);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
        CollectionAssert.AreEqual(
            new[] { DataSourcePhase.Begin, DataSourcePhase.RowsKnown, DataSourcePhase.RowsRead, DataSourcePhase.End },
            progress.Select(static item => item.Phase).Distinct().ToArray());
    }

    [TestMethod]
    public void CompiledQuery_WhenTakeRemainsResidual_ShouldDisposeImmediatelyAfterLimit()
    {
        var scenario = new QueryRowLifecycleScenario
        {
            ReaderStyle = QueryRowLifecycleReaderStyle.Property,
            Rows = CreateNumberedRows(20),
            ChunkSize = 1
        };
        using var compiled = Compile("select r.Id from #lifecycle.rows() r take 2", scenario);

        var rows = Execute(compiled);

        Assert.HasCount(2, rows);
        Assert.AreEqual(2, scenario.Recorder.RawRowsEnumerated);
        Assert.AreEqual(2, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
        Assert.IsTrue(scenario.Recorder.Events.Contains(QueryRowLifecycleEvent.End));
    }

    [TestMethod]
    public void CompiledQuery_WhenPredicateAndTakeRemainResidual_ShouldStopAfterAcceptedEngineRows()
    {
        var scenario = new QueryRowLifecycleScenario
        {
            ReaderStyle = QueryRowLifecycleReaderStyle.Ordinal,
            Rows = CreateNumberedRows(20),
            ChunkSize = 1
        };
        using var compiled = Compile(
            "select r.Id from #lifecycle.rows() r where r.Id >= 8 take 2",
            scenario);

        var rows = Execute(compiled);

        Assert.HasCount(2, rows);
        Assert.AreEqual(8, rows[0][0]);
        Assert.AreEqual(9, rows[1][0]);
        Assert.AreEqual(10, scenario.Recorder.RawRowsEnumerated);
        Assert.AreEqual(10, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
    }

    [TestMethod]
    public void ProfiledQuery_WhenMaterializingQueryRows_ShouldRecordProducedRowsAndTypedTransferOperation()
    {
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Ordinal);
        using var compiled = InstanceCreator.CompileForProfile(
            "select r.Id from #lifecycle.rows() r",
            $"query-row-lifecycle-profile-{Guid.NewGuid():N}",
            new QueryRowLifecycleSchemaProvider(scenario),
            _loggerResolver);

        var profileResult = compiled.RunWithProfile();
        using var result = profileResult.Result;
        var source = profileResult.Profile.Sources.Single();

        Assert.AreEqual(3, source.RowsProduced);
        var materialize = source.Operations.Single(operation => operation.Name == "query-row.materialize");
        Assert.AreEqual(3, materialize.Count);
        Assert.AreEqual(SourceDiagnosticOperation.Transform, materialize.Operation);
    }

    [TestMethod]
    public void CompiledQuery_WhenTokenIsPreCancelled_ShouldPreserveCancellationWithoutOpeningSource()
    {
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Ordinal);
        using var compiled = Compile("select r.Id from #lifecycle.rows() r", scenario);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => Execute(compiled, cancellation.Token));

        Assert.AreEqual(0, scenario.Recorder.OpenCalls);
        Assert.AreEqual(0, scenario.Recorder.MaterializerCalls);
    }

    [TestMethod]
    public void CompiledQuery_WhenCancelledMidStream_ShouldPreserveCancellationAndDisposeSource()
    {
        using var cancellation = new CancellationTokenSource();
        var scenario = new QueryRowLifecycleScenario
        {
            ReaderStyle = QueryRowLifecycleReaderStyle.Path,
            Rows = CreateNumberedRows(20),
            ChunkSize = 1,
            OnRawRowEnumerated = count =>
            {
                if (count == 3)
                    cancellation.Cancel();
            }
        };
        using var compiled = Compile("select r.Id from #lifecycle.rows() r", scenario);

        Assert.Throws<OperationCanceledException>(() => Execute(compiled, cancellation.Token));

        Assert.AreEqual(3, scenario.Recorder.RawRowsEnumerated);
        Assert.AreEqual(2, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
        AssertLifecycleSequenceContains(
            scenario,
            QueryRowLifecycleEvent.Begin,
            QueryRowLifecycleEvent.Cancellation,
            QueryRowLifecycleEvent.End,
            QueryRowLifecycleEvent.Dispose);
    }

    [TestMethod]
    public void CompiledQuery_WhenQueryScopedSourceOpenFails_ShouldPreserveContextAndInnerException()
    {
        var original = new IOException("query-row-open-failure");
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Ordinal) with
        {
            OpenFailure = original
        };
        using var compiled = Compile("select r.Id from #lifecycle.rows() r", scenario);

        var exception = Assert.Throws<QueryExecutionException>(() => Execute(compiled));

        AssertDataSourceFailure(exception, DiagnosticCode.MQ7010_DataSourceOpenFailed, "open", original);
    }

    [TestMethod]
    public void CompiledQuery_WhenQueryScopedSourceReturnsNull_ShouldProduceContextualOpenFailure()
    {
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Ordinal) with
        {
            ReturnNullSource = true
        };
        using var compiled = Compile("select r.Id from #lifecycle.rows() r", scenario);

        var exception = Assert.Throws<QueryExecutionException>(() => Execute(compiled));

        AssertDataSourceFailure(exception, DiagnosticCode.MQ7010_DataSourceOpenFailed, "open");
        Assert.Contains("returned null", exception.InnerException!.InnerException!.Message);
    }

    [TestMethod]
    public void CompiledQuery_WhenRawReaderFails_ShouldPreserveContextInnerExceptionAndLifecycle()
    {
        var original = new IOException("query-row-raw-read-failure");
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Property) with
        {
            RawReadFailure = original,
            RawReadFailureIndex = 1,
            ChunkSize = 1
        };
        using var compiled = Compile("select r.Id from #lifecycle.rows() r", scenario);

        var exception = Assert.Throws<QueryExecutionException>(() => Execute(compiled));

        AssertDataSourceFailure(exception, DiagnosticCode.MQ7011_DataSourceReadFailed, "read", original);
        Assert.AreEqual(1, scenario.Recorder.MaterializerCalls);
        AssertLifecycleSequenceContains(
            scenario,
            QueryRowLifecycleEvent.Begin,
            QueryRowLifecycleEvent.Failure,
            QueryRowLifecycleEvent.End,
            QueryRowLifecycleEvent.Dispose);
    }

    [TestMethod]
    public void CompiledQuery_WhenFieldReaderFailsInsideMaterializer_ShouldPreserveContextAndInnerException()
    {
        var original = new InvalidDataException("query-row-materializer-read-failure");
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Path) with
        {
            FieldReadFailure = original,
            FieldReadFailureSourceOrdinal = 1
        };
        using var compiled = Compile("select r.Label from #lifecycle.rows() r", scenario);

        var exception = Assert.Throws<QueryExecutionException>(() => Execute(compiled));

        AssertDataSourceFailure(exception, DiagnosticCode.MQ7011_DataSourceReadFailed, "read", original);
        Assert.AreEqual(1, scenario.Recorder.MaterializerCalls);
        Assert.AreEqual(1, scenario.Recorder.FieldReads);
        Assert.AreEqual(1, scenario.Recorder.SourcesDisposed);
    }

    [TestMethod]
    [DynamicData(nameof(FeatureQueries))]
    public void CompiledQuery_WhenUsingRetainingFeatures_ShouldFlowThroughRecordedChunkedSource(
        string query,
        int expectedRows)
    {
        var scenario = CreateDefaultScenario(QueryRowLifecycleReaderStyle.Path) with
        {
            ChunkSize = 2
        };
        using var compiled = Compile(query, scenario);

        var rows = Execute(compiled);

        Assert.HasCount(expectedRows, rows);
        Assert.IsGreaterThan(0, scenario.Recorder.MaterializerCalls);
        Assert.IsGreaterThan(0, scenario.Recorder.FieldReads);
        Assert.AreEqual(scenario.Recorder.OpenCalls, scenario.Recorder.SourcesDisposed);
    }

    public static IEnumerable<object[]> FeatureQueries
    {
        get
        {
            yield return
            [
                "select l.Id, r.Id from #lifecycle.rows() l inner join #lifecycle.rows() r on l.Id = r.Id",
                3
            ];
            yield return
            [
                "select l.Id, r.Id from #lifecycle.rows() l left join #lifecycle.rows() r on r.Id = 99",
                3
            ];
            yield return
            [
                "select r.Category, Count(*) from #lifecycle.rows() r group by r.Category",
                2
            ];
            yield return
            [
                "select r.Id, RowNumber() over (order by r.Id) from #lifecycle.rows() r",
                3
            ];
            yield return
            [
                "with valuesCte as (select r.Id as Id from #lifecycle.rows() r) select v.Id from valuesCte v",
                3
            ];
            yield return
            [
                "select r.Id from #lifecycle.rows() r union all select s.Id from #lifecycle.rows() s",
                6
            ];
        }
    }

    private CompiledQuery Compile(string query, QueryRowLifecycleScenario scenario)
    {
        var result = InstanceCreator.CompileWithDiagnostics(
            query,
            $"query-row-lifecycle-{Guid.NewGuid():N}",
            new QueryRowLifecycleSchemaProvider(scenario),
            _loggerResolver);

        Assert.IsTrue(
            result.Succeeded,
            $"{result.CaughtException}{Environment.NewLine}{string.Join(Environment.NewLine, result.Diagnostics)}");
        return result.CompiledQuery!;
    }

    private static object?[][] Execute(CompiledQuery compiled, CancellationToken token = default)
    {
        using var table = compiled.Run(token);
        var rows = new object?[table.Count][];
        for (var rowIndex = 0; rowIndex < table.Count; rowIndex++)
        {
            rows[rowIndex] = new object?[table.Columns.Count()];
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
                rows[rowIndex][columnIndex] = table[rowIndex][columnIndex];
        }

        return rows;
    }

    private static QueryRowLifecycleScenario CreateDefaultScenario(QueryRowLifecycleReaderStyle readerStyle)
    {
        return new QueryRowLifecycleScenario
        {
            ReaderStyle = readerStyle,
            Rows =
            [
                new QueryRowLifecycleRawRow(1, "one", "a", 10, "first"),
                new QueryRowLifecycleRawRow(2, "two", "a", 20, "second"),
                new QueryRowLifecycleRawRow(3, "three", "b", null, null)
            ]
        };
    }

    private static IReadOnlyList<QueryRowLifecycleRawRow> CreateNumberedRows(int count)
    {
        return Enumerable.Range(0, count)
            .Select(index => new QueryRowLifecycleRawRow(index, $"row-{index}", "number", index, null))
            .ToArray();
    }

    private static void AssertDataSourceFailure(
        QueryExecutionException exception,
        DiagnosticCode code,
        string operation,
        Exception? original = null)
    {
        var envelope = exception.Envelope ?? throw new AssertFailedException("Expected a datasource envelope.");
        Assert.AreEqual(code, envelope.Code);
        Assert.AreEqual("#lifecycle", envelope.Arguments["schema"]);
        Assert.AreEqual("rows", envelope.Arguments["source"]);
        Assert.AreEqual("r", envelope.Arguments["alias"]);
        Assert.AreEqual(operation, envelope.Arguments["operation"]);
        Assert.IsFalse(string.IsNullOrWhiteSpace(envelope.Arguments["sourceContextId"]));

        var lifecycle = exception.InnerException as DataSourceLifecycleException ??
                        throw new AssertFailedException("Expected the datasource lifecycle exception.");
        if (original != null)
            Assert.AreSame(original, lifecycle.InnerException);
    }

    private static void AssertLifecycleSequenceContains(
        QueryRowLifecycleScenario scenario,
        params QueryRowLifecycleEvent[] expected)
    {
        var events = scenario.Recorder.Events;
        var nextIndex = 0;
        foreach (var lifecycleEvent in events)
        {
            if (nextIndex < expected.Length && lifecycleEvent == expected[nextIndex])
                nextIndex++;
        }

        Assert.AreEqual(
            expected.Length,
            nextIndex,
            $"Expected lifecycle subsequence [{string.Join(", ", expected)}], actual [{string.Join(", ", events)}].");
    }
}
