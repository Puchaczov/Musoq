using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Parser.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.DataSources;
using Musoq.Schema.Exceptions;
using Musoq.Schema.Optimization;
using Musoq.Schema.Reflection;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Permanent REC-128 lifecycle matrix. Each row is a frozen campaign candidate;
/// the fixture deliberately observes the real DataSourceLifecycle boundary rather
/// than a test-only copy of its iterator logic.
/// </summary>
[TestClass]
public sealed class REC128DeferredLifecycleTests
{
    private const string SchemaName = "#rec128";
    private const string SourceName = "rows";
    private const string Alias = "r";
    private const string ContextId = "rec128-context";
    private const string Secret = "secret-rec128-provider-detail";

    [TestMethod]
    [DataRow("FR-S-MOVE")]
    [DataRow("FR-A-MOVE")]
    [DataRow("FR-S-GET")]
    [DataRow("FR-A-GET")]
    [DataRow("FR-S-CURRENT")]
    [DataRow("FR-A-CURRENT")]
    [DataRow("LR-S-MOVE")]
    [DataRow("LR-A-MOVE")]
    [DataRow("LR-S-CURRENT")]
    [DataRow("LR-A-CURRENT")]
    [DataRow("LR-S-DEFERRED")]
    [DataRow("LR-A-DEFERRED")]
    [DataRow("DP-S-CURRENT")]
    [DataRow("DP-A-CURRENT")]
    [DataRow("DP-S-CURRENT-CLEANUP")]
    [DataRow("DP-A-CURRENT-CLEANUP")]
    [DataRow("DP-S-CHUNKS")]
    [DataRow("DP-A-CHUNKS")]
    [DataRow("RC-S-FIRST")]
    [DataRow("RC-A-FIRST")]
    [DataRow("RC-S-LATER")]
    [DataRow("RC-A-LATER")]
    [DataRow("RC-S-CURRENT")]
    [DataRow("RC-A-CURRENT")]
    [DataRow("DS-S-EARLY")]
    [DataRow("DS-A-EARLY")]
    [DataRow("DS-S-NORMAL")]
    [DataRow("DS-A-NORMAL")]
    [DataRow("DS-S-SUCCESS")]
    [DataRow("DS-A-SUCCESS")]
    [DataRow("OC-S-SCHEMA")]
    [DataRow("OC-S-WRAPPED-SCHEMA")]
    [DataRow("OC-S-ROW")]
    [DataRow("OC-S-WRAPPED-ROW")]
    [DataRow("OC-S-QUERY-ROW")]
    [DataRow("OC-S-WRAPPED-QUERY-ROW")]
    [DataRow("RD-S-FIRST")]
    [DataRow("RD-A-FIRST")]
    [DataRow("RD-S-LATER")]
    [DataRow("RD-A-LATER")]
    [DataRow("RD-S-CURRENT")]
    [DataRow("RD-A-CURRENT")]
    [DataRow("CL-S-NORMAL")]
    [DataRow("CL-A-NORMAL")]
    [DataRow("CL-S-READ")]
    [DataRow("CL-A-READ")]
    [DataRow("CL-A-TOKEN")]
    [DataRow("CL-S-EARLY")]
    public async Task DeferredLifecycleMatrix_ShouldPreservePrimaryFailureAndDisposeExactlyOnce(string caseId)
    {
        switch (caseId)
        {
            case "OC-S-SCHEMA":
                AssertCancellation(() => DataSourceLifecycle.OpenSchema(
                    new CancellingProvider(), SchemaName, SourceName, Alias, ContextId));
                return;
            case "OC-S-WRAPPED-SCHEMA":
                AssertCancellation(() => DataSourceLifecycle.WrapProvider(new CancellingProvider())
                    .GetSchema(SchemaName));
                return;
            case "OC-S-ROW":
                AssertCancellation(() => DataSourceLifecycle.OpenRowSource<int>(
                    new CancellingSchema(), SourceName, CreateContext(), [], SchemaName, Alias, ContextId));
                return;
            case "OC-S-WRAPPED-ROW":
            {
                var schema = DataSourceLifecycle.OpenSchema(
                    new StaticProvider(new CancellingSchema()), SchemaName, SourceName, Alias, ContextId);
                AssertCancellation(() => schema.GetRowSource<int>(SourceName, CreateContext()));
                return;
            }
            case "OC-S-QUERY-ROW":
                AssertCancellation(() => DataSourceLifecycle.OpenQueryScopedRowSource<int, EmptyMaterializer>(
                    new CancellingSchema(), SourceName, CreateQueryScopedRequest(), [],
                    SchemaName, Alias, ContextId));
                return;
            case "OC-S-WRAPPED-QUERY-ROW":
            {
                var schema = DataSourceLifecycle.OpenSchema(
                    new StaticProvider(new CancellingSchema()), SchemaName, SourceName, Alias, ContextId);
                var queryScoped = (IQueryScopedRowSourceSchema)schema;
                AssertCancellation(() => queryScoped.GetQueryScopedRowSource<int, EmptyMaterializer>(
                    SourceName, CreateQueryScopedRequest(), []));
                return;
            }
        }

        if (caseId.StartsWith("FR-", StringComparison.Ordinal) ||
            caseId.StartsWith("LR-", StringComparison.Ordinal) ||
            caseId.StartsWith("DP-", StringComparison.Ordinal) ||
            caseId.StartsWith("RC-", StringComparison.Ordinal) ||
            caseId.StartsWith("DS-", StringComparison.Ordinal) ||
            caseId.StartsWith("RD-", StringComparison.Ordinal) ||
            caseId.StartsWith("CL-", StringComparison.Ordinal))
        {
            if (caseId.Length > 3 && caseId[3] == 'A')
                await AssertAsyncCase(caseId);
            else if (caseId.Length > 3 && caseId[3] == 'S')
                AssertSyncCase(caseId);
            else
                Assert.Fail($"Unrecognized REC-128 execution mode in '{caseId}'.");

            return;
        }

        Assert.Fail($"Unknown REC-128 candidate '{caseId}'.");
    }

    private static void AssertSyncCase(string caseId)
    {
        var cancellation = caseId.StartsWith("RD-", StringComparison.Ordinal) ||
                           caseId.StartsWith("CL-S-", StringComparison.Ordinal)
            ? new OperationCanceledException($"{caseId}-cancellation")
            : null;
        var plan = CreateSyncPlan(caseId, cancellation);
        var observed = CaptureSync(plan);

        if (cancellation != null)
        {
            Assert.AreSame(cancellation, observed.Exception, caseId);
            Assert.AreEqual(caseId == "FR-S-GET" ? 0 : 1, observed.DisposeCount, caseId);
            return;
        }

        if (caseId == "DS-S-SUCCESS")
        {
            Assert.IsNull(observed.Exception, caseId);
            Assert.AreEqual(1, observed.DisposeCount, caseId);
            return;
        }

        AssertLifecycleFailure(observed.Exception, caseId, caseId.StartsWith("DS-", StringComparison.Ordinal)
            ? DiagnosticCode.MQ7012_DataSourceCleanupFailed
            : DiagnosticCode.MQ7011_DataSourceReadFailed,
            caseId.StartsWith("RC-", StringComparison.Ordinal) ||
            caseId == "DP-S-CURRENT-CLEANUP");
        Assert.AreEqual(caseId is "FR-S-GET" or "DP-S-CHUNKS" ? 0 : 1, observed.DisposeCount, caseId);
    }

    private static async Task AssertAsyncCase(string caseId)
    {
        var cancellation = caseId.StartsWith("RD-", StringComparison.Ordinal) ||
                           caseId is "CL-A-NORMAL" or "CL-A-READ"
            ? new OperationCanceledException($"{caseId}-cancellation")
            : null;
        var plan = CreateAsyncPlan(caseId, cancellation);
        var observed = await CaptureAsync(plan);

        if (cancellation != null)
        {
            Assert.AreSame(cancellation, observed.Exception, caseId);
            Assert.AreEqual(caseId == "FR-A-GET" ? 0 : 1, observed.DisposeCount, caseId);
            return;
        }

        if (caseId == "CL-A-TOKEN")
        {
            Assert.IsInstanceOfType(observed.Exception, typeof(OperationCanceledException), caseId);
            Assert.AreEqual(1, observed.DisposeCount, caseId);
            return;
        }

        if (caseId == "DS-A-SUCCESS")
        {
            Assert.IsNull(observed.Exception, caseId);
            Assert.AreEqual(1, observed.DisposeCount, caseId);
            return;
        }

        AssertLifecycleFailure(observed.Exception, caseId, caseId.StartsWith("DS-", StringComparison.Ordinal)
            ? DiagnosticCode.MQ7012_DataSourceCleanupFailed
            : DiagnosticCode.MQ7011_DataSourceReadFailed,
            caseId.StartsWith("RC-", StringComparison.Ordinal) ||
            caseId == "DP-A-CURRENT-CLEANUP");
        Assert.AreEqual(caseId == "FR-A-GET" || caseId == "DP-A-CHUNKS" ? 0 : 1, observed.DisposeCount, caseId);
    }

    private static void AssertLifecycleFailure(
        Exception? exception,
        string caseId,
        DiagnosticCode expectedCode,
        bool expectRelatedCleanup)
    {
        var lifecycle = exception as DataSourceLifecycleException;
        Assert.IsNotNull(lifecycle, caseId);
        Assert.AreEqual(expectedCode, lifecycle!.Code, caseId);
        Assert.AreEqual(expectedCode == DiagnosticCode.MQ7012_DataSourceCleanupFailed ? "cleanup" : "read",
            lifecycle.Operation,
            caseId);
        Assert.IsNotNull(lifecycle.InnerException, caseId);
        Assert.DoesNotContain(Secret, lifecycle.Message, caseId);
        Assert.IsFalse(lifecycle.ToDiagnostic().Arguments.Values.Any(value => value.Contains(Secret, StringComparison.Ordinal)), caseId);

        if (expectRelatedCleanup)
        {
            Assert.IsTrue(lifecycle.ToDiagnostic().Arguments.ContainsKey("relatedCauseType"), caseId);
            Assert.AreEqual(typeof(IOException).FullName, lifecycle.ToDiagnostic().Arguments["relatedCauseType"], caseId);
        }
        else
            Assert.IsFalse(lifecycle.ToDiagnostic().Arguments.ContainsKey("relatedCauseType"), caseId);
    }

    private static void AssertCancellation(Action action)
    {
        var cancellation = Assert.Throws<OperationCanceledException>(action);
        Assert.IsNotNull(cancellation, "Cancellation must cross the lifecycle boundary unchanged.");
    }

    private static SyncPlan CreateSyncPlan(string caseId, OperationCanceledException? cancellation)
    {
        Exception readFailure = (Exception?)cancellation ?? new IOException($"{caseId}-{Secret}");
        var cleanupFailure = new IOException($"{caseId}-{Secret}-cleanup");
        var moveNextThrow = caseId switch
        {
            "FR-S-MOVE" or "RC-S-FIRST" or "RD-S-FIRST" => readFailure,
            "LR-S-MOVE" or "RC-S-LATER" or "RD-S-LATER" => readFailure,
            _ => null
        };
        var currentThrow = caseId switch
        {
            "FR-S-CURRENT" or "DP-S-CURRENT" or "DP-S-CURRENT-CLEANUP" or "RC-S-CURRENT" or "RD-S-CURRENT" => readFailure,
            "LR-S-CURRENT" or "LR-S-DEFERRED" => readFailure,
            _ => null
        };
        var getThrow = caseId == "FR-S-GET" ? readFailure : null;
        Exception? disposeThrow = caseId switch
        {
            "RC-S-FIRST" or "RC-S-LATER" or "RC-S-CURRENT" or "DP-S-CURRENT-CLEANUP" => cleanupFailure,
            "DS-S-EARLY" or "DS-S-NORMAL" => cleanupFailure,
            "CL-S-NORMAL" or "CL-S-READ" or "CL-S-EARLY" => cancellation ?? new OperationCanceledException($"{caseId}-cancellation"),
            _ => null
        };
        var laterRead = caseId is "LR-S-MOVE" or "LR-S-DEFERRED" or "LR-S-CURRENT" or "RC-S-LATER" or "RD-S-LATER";
        var currentOnSecond = caseId is "LR-S-CURRENT";
        var twoChunks = caseId is "LR-S-CURRENT";
        var normal = caseId is "DS-S-EARLY" or "DS-S-NORMAL" or "DS-S-SUCCESS" or "CL-S-NORMAL";
        var earlyDispose = caseId == "DS-S-EARLY" || caseId == "CL-S-EARLY";

        return new SyncPlan(
            new ScriptedEnumerable(getThrow, moveNextThrow, currentThrow, disposeThrow, laterRead, currentOnSecond, twoChunks),
            readFailure,
            cleanupFailure,
            normal,
            earlyDispose,
            caseId == "DP-S-CHUNKS");
    }

    private static AsyncPlan CreateAsyncPlan(string caseId, OperationCanceledException? cancellation)
    {
        Exception readFailure = (Exception?)cancellation ?? new IOException($"{caseId}-{Secret}");
        var cleanupFailure = new IOException($"{caseId}-{Secret}-cleanup");
        var moveNextThrow = caseId switch
        {
            "FR-A-MOVE" or "RC-A-FIRST" or "RD-A-FIRST" => readFailure,
            "LR-A-MOVE" or "RC-A-LATER" or "RD-A-LATER" => readFailure,
            _ => null
        };
        var currentThrow = caseId switch
        {
            "FR-A-CURRENT" or "DP-A-CURRENT" or "DP-A-CURRENT-CLEANUP" or "RC-A-CURRENT" or "RD-A-CURRENT" => readFailure,
            "LR-A-CURRENT" or "LR-A-DEFERRED" => readFailure,
            _ => null
        };
        var getThrow = caseId is "FR-A-GET" or "DP-A-CHUNKS" ? readFailure : null;
        Exception? disposeThrow = caseId switch
        {
            "RC-A-FIRST" or "RC-A-LATER" or "RC-A-CURRENT" or "DP-A-CURRENT-CLEANUP" => cleanupFailure,
            "DS-A-EARLY" or "DS-A-NORMAL" => cleanupFailure,
            "CL-A-NORMAL" or "CL-A-READ" => cancellation ?? new OperationCanceledException($"{caseId}-cancellation"),
            "CL-A-TOKEN" => cleanupFailure,
            _ => null
        };
        var laterRead = caseId is "LR-A-MOVE" or "LR-A-DEFERRED" or "RC-A-LATER" or "RD-A-LATER";
        var currentOnSecond = caseId is "LR-A-CURRENT";
        var twoChunks = caseId is "LR-A-CURRENT";
        var normal = caseId is "DS-A-EARLY" or "DS-A-NORMAL" or "DS-A-SUCCESS" or "CL-A-NORMAL" or "CL-A-TOKEN";
        var earlyDispose = caseId == "DS-A-EARLY";
        var cancelToken = caseId == "CL-A-TOKEN" ? new CancellationTokenSource() : null;
        if (cancelToken != null)
            cancelToken.Cancel();

        return new AsyncPlan(
            new ScriptedAsyncEnumerable(getThrow, moveNextThrow, currentThrow, disposeThrow, laterRead, currentOnSecond, twoChunks),
            readFailure,
            cleanupFailure,
            normal,
            earlyDispose,
            cancelToken);
    }

    private static Observed CaptureSync(SyncPlan plan)
    {
        Exception? exception = null;
        try
        {
            var enumerable = plan.Chunks;
            using var enumerator = DataSourceLifecycle.Read(
                    new TestRowSource(enumerable, plan.ChunksPropertyThrows), SchemaName, SourceName, Alias, ContextId)
                .GetEnumerator();
            if (plan.EarlyDispose)
            {
                _ = enumerator.MoveNext();
                return new Observed(null, enumerable.DisposeCount);
            }

            while (enumerator.MoveNext())
                _ = enumerator.Current;
        }
        catch (Exception caught)
        {
            exception = caught;
        }

        return new Observed(exception, plan.Chunks.DisposeCount);
    }

    private static async Task<Observed> CaptureAsync(AsyncPlan plan)
    {
        Exception? exception = null;
        try
        {
            var enumerable = plan.Chunks;
            if (plan.EarlyDispose)
            {
                await using var enumerator = DataSourceLifecycle.ReadAsync(
                        enumerable, SchemaName, SourceName, Alias, ContextId,
                        plan.Cancellation?.Token ?? default)
                    .GetAsyncEnumerator(plan.Cancellation?.Token ?? default);
                _ = await enumerator.MoveNextAsync();
                return new Observed(null, enumerable.DisposeCount);
            }

            await using (var enumerator = DataSourceLifecycle.ReadAsync(
                             enumerable, SchemaName, SourceName, Alias, ContextId,
                             plan.Cancellation?.Token ?? default)
                         .GetAsyncEnumerator(plan.Cancellation?.Token ?? default))
            {
                while (await enumerator.MoveNextAsync())
                    _ = enumerator.Current;
            }
        }
        catch (Exception caught)
        {
            exception = caught;
        }

        return new Observed(exception, plan.Chunks.DisposeCount);
    }

    private static SourceExecutionContext CreateContext()
    {
        return new SourceExecutionContext(
            "rec128-query",
            SourceExecutionPlan.Empty(new SourceIdentity(SchemaName, SourceName, ContextId, Alias)),
            CancellationToken.None,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    private static QueryScopedRowSourceRequest CreateQueryScopedRequest() =>
        new(CreateContext(), new QueryRowShape([]));

    private sealed record SyncPlan(
        ScriptedEnumerable Chunks,
        Exception ReadFailure,
        Exception CleanupFailure,
        bool Normal,
        bool EarlyDispose,
        bool ChunksPropertyThrows);

    private sealed record AsyncPlan(
        ScriptedAsyncEnumerable Chunks,
        Exception ReadFailure,
        Exception CleanupFailure,
        bool Normal,
        bool EarlyDispose,
        CancellationTokenSource? Cancellation);

    private sealed record Observed(Exception? Exception, int DisposeCount);

    private sealed class TestRowSource(ScriptedEnumerable chunks, bool throwOnChunks) : RowSource<int>
    {
        public override IEnumerable<IReadOnlyList<int>> Chunks => throwOnChunks
            ? throw new InvalidOperationException($"{Secret}-chunks")
            : chunks;
    }

    private sealed class ScriptedEnumerable(
        Exception? getEnumeratorThrow,
        Exception? moveNextThrow,
        Exception? currentThrow,
        Exception? disposeThrow,
        bool laterRead,
        bool currentOnSecond,
        bool twoChunks) : IEnumerable<IReadOnlyList<int>>
    {
        public int DisposeCount { get; private set; }

        public IEnumerator<IReadOnlyList<int>> GetEnumerator()
        {
            if (getEnumeratorThrow != null)
                throw getEnumeratorThrow;

            return new ScriptedEnumerator(this, moveNextThrow, currentThrow, disposeThrow, laterRead, currentOnSecond, twoChunks);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class ScriptedEnumerator(
            ScriptedEnumerable owner,
            Exception? moveNextThrow,
            Exception? currentThrow,
            Exception? disposeThrow,
            bool laterRead,
            bool currentOnSecond,
            bool twoChunks) : IEnumerator<IReadOnlyList<int>>
        {
            private int _moveNextCount;
            private int _currentCount;
            private bool _hasCurrent;

            public IReadOnlyList<int> Current
            {
                get
                {
                    _currentCount++;
                    var shouldThrow = currentThrow != null &&
                                      (!currentOnSecond || _currentCount == 2);
                    if (shouldThrow)
                        throw currentThrow!;

                    return [1];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                _moveNextCount++;
                if (moveNextThrow != null &&
                    ((!laterRead && _moveNextCount == 1) || (laterRead && _moveNextCount == 2)))
                    throw moveNextThrow;

                _hasCurrent = _moveNextCount <= (twoChunks ? 2 : 1);
                return _hasCurrent;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
                if (_hasCurrent || _moveNextCount > 0)
                {
                    owner.DisposeCount++;
                    _hasCurrent = false;
                    if (disposeThrow != null)
                        throw disposeThrow;
                }
            }
        }
    }

    private sealed class ScriptedAsyncEnumerable(
        Exception? getEnumeratorThrow,
        Exception? moveNextThrow,
        Exception? currentThrow,
        Exception? disposeThrow,
        bool laterRead,
        bool currentOnSecond,
        bool twoChunks) : IAsyncEnumerable<IReadOnlyList<int>>
    {
        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<IReadOnlyList<int>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            if (getEnumeratorThrow != null)
                throw getEnumeratorThrow;

            return new ScriptedAsyncEnumerator(this, moveNextThrow, currentThrow, disposeThrow, laterRead, currentOnSecond, twoChunks);
        }

        private sealed class ScriptedAsyncEnumerator(
            ScriptedAsyncEnumerable owner,
            Exception? moveNextThrow,
            Exception? currentThrow,
            Exception? disposeThrow,
            bool laterRead,
            bool currentOnSecond,
            bool twoChunks) : IAsyncEnumerator<IReadOnlyList<int>>
        {
            private int _moveNextCount;
            private int _currentCount;
            private bool _hasCurrent;

            public IReadOnlyList<int> Current
            {
                get
                {
                    _currentCount++;
                    var shouldThrow = currentThrow != null &&
                                      (!currentOnSecond || _currentCount == 2);
                    if (shouldThrow)
                        throw currentThrow!;

                    return [1];
                }
            }

            public ValueTask<bool> MoveNextAsync()
            {
                _moveNextCount++;
                if (moveNextThrow != null &&
                    ((!laterRead && _moveNextCount == 1) || (laterRead && _moveNextCount == 2)))
                    return ValueTask.FromException<bool>(moveNextThrow);

                _hasCurrent = _moveNextCount <= (twoChunks ? 2 : 1);
                return ValueTask.FromResult(_hasCurrent);
            }

            public ValueTask DisposeAsync()
            {
                if (!_hasCurrent && _moveNextCount == 0)
                    return ValueTask.CompletedTask;

                owner.DisposeCount++;
                _hasCurrent = false;
                return disposeThrow == null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(disposeThrow);
            }
        }
    }

    private sealed class CancellingProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => throw new OperationCanceledException("rec128-open-cancellation");
    }

    private sealed class StaticProvider(ISchema schema) : ISchemaProvider
    {
        public ISchema GetSchema(string schemaName) => schema;
    }

    private sealed class CancellingSchema : ISchema, IQueryScopedRowSourceSchema
    {
        public string Name => SchemaName;

        public ISchemaTable GetTableByName(string name, SourceMetadataContext metadataContext, params object?[] parameters) =>
            throw new NotSupportedException();

        public SourceDescriptor DescribeSource(string name, SourceDescribeContext context, params object?[] parameters) =>
            throw new NotSupportedException();

        public IReadOnlyList<SourceRuntimeSettingRequirement> DescribeSourceRuntimeSettings(
            string name,
            SourceRuntimeSettingsDescribeContext context,
            params object?[] parameters) => [];

        public SourcePlanResult TryPlanSource(string name, SourcePlanRequest request, params object?[] parameters) =>
            SourcePlanResult.RejectAll(request);

        public RowSource<T> GetRowSource<T>(string name, SourceExecutionContext executionContext, params object?[] parameters) =>
            throw new OperationCanceledException("rec128-row-open-cancellation");

        public RowSource<TRow> GetQueryScopedRowSource<TRow, TMaterializer>(
            string name,
            QueryScopedRowSourceRequest request,
            params object?[] parameters)
            where TMaterializer : struct, IQueryRowMaterializer<TRow> =>
            throw new OperationCanceledException("rec128-query-row-open-cancellation");

        public SchemaMethodInfo[] GetRawConstructors(SourceMetadataContext metadataContext) => [];

        public SchemaMethodInfo[] GetRawConstructors(string methodName, SourceMetadataContext metadataContext) => [];

        public bool TryResolveMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveRawMethod(string method, Type[] parameters, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(string method, Type[] parameters, Type? entityType, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveAggregationMethod(
            string method,
            Type[] parameters,
            Type? entityType,
            Func<MethodInfo, bool> methodFilter,
            [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public bool TryResolveWindowFunction(string method, [NotNullWhen(true)] out MethodInfo? methodInfo)
        {
            methodInfo = null;
            return false;
        }

        public IReadOnlyDictionary<string, IReadOnlyList<MethodInfo>> GetAllLibraryMethods() =>
            new Dictionary<string, IReadOnlyList<MethodInfo>>();
    }

    private readonly struct EmptyMaterializer : IQueryRowMaterializer<int>
    {
        public static int Materialize<TReader>(scoped ref TReader reader)
            where TReader : IQuerySourceFieldReader, allows ref struct => 0;
    }
}
