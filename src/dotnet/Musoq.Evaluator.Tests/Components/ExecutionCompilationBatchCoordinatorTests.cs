using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Architecture;
using Musoq.Evaluator.Tests.Schema.Basic;
using Musoq.Schema;
using Musoq.Schema.Optimization;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Components;

[TestClass]
public sealed class ExecutionCompilationBatchCoordinatorTests
{
    [TestMethod]
    public async Task Submit_WhenEightRequestsArriveTogether_ShouldUseOneBoundedBatch()
    {
        var batchCalls = 0;
        var singleCalls = 0;
        var queries = new ConcurrentBag<CompiledQuery>();
        using var coordinator = CreateCoordinator(
            requests =>
            {
                Interlocked.Increment(ref batchCalls);
                return requests
                    .Select(request => CreateSuccess(request, queries))
                    .ToArray();
            },
            request =>
            {
                Interlocked.Increment(ref singleCalls);
                return CreateSuccess(request, queries).Result;
            },
            collectionWindow: TimeSpan.FromMilliseconds(100));

        var start = new Barrier(8);
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            start.SignalAndWait();
            return coordinator.Submit($"query-{index}", new EmptySchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        })));

        Assert.IsTrue(results.All(static result => result.WasBatched));
        Assert.AreEqual(1, batchCalls);
        Assert.AreEqual(0, singleCalls);
        Assert.HasCount(8, queries);
        foreach (var query in queries)
            query.Dispose();
    }

    [TestMethod]
    public async Task Submit_WhenSixteenRequestsArriveTogether_ShouldDrainOneSharedQueueBatch()
    {
        var batchCalls = 0;
        var queries = new ConcurrentBag<CompiledQuery>();
        using var coordinator = CreateCoordinator(
            requests =>
            {
                Interlocked.Increment(ref batchCalls);
                return requests.Select(request => CreateSuccess(request, queries)).ToArray();
            },
            request => CreateSuccess(request, queries).Result,
            maximumBatchSize: 16,
            collectionWindow: TimeSpan.FromMilliseconds(100));

        var start = new Barrier(16);
        var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
        {
            start.SignalAndWait();
            return coordinator.Submit($"query-{index}", new EmptySchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        })));

        Assert.IsTrue(results.All(static result => result.WasBatched));
        Assert.AreEqual(1, batchCalls);
        Assert.HasCount(16, queries);
        foreach (var query in queries)
            query.Dispose();
    }

    [TestMethod]
    public void Submit_WhenRequestIsAlone_ShouldUseOriginalSinglePath()
    {
        var batchCalls = 0;
        var singleCalls = 0;
        var queries = new ConcurrentBag<CompiledQuery>();
        using var coordinator = CreateCoordinator(
            requests =>
            {
                Interlocked.Increment(ref batchCalls);
                return requests.Select(request => CreateSuccess(request, queries)).ToArray();
            },
            request =>
            {
                Interlocked.Increment(ref singleCalls);
                return CreateSuccess(request, queries).Result;
            },
            collectionWindow: TimeSpan.FromMilliseconds(1));

        var result = coordinator.Submit(
            "single-query",
            new EmptySchemaProvider(),
            new TestsLoggerResolver(),
            new CompilationOptions());

        Assert.IsFalse(result.WasBatched);
        Assert.AreEqual(0, batchCalls);
        Assert.AreEqual(1, singleCalls);
        foreach (var query in queries)
            query.Dispose();
    }

    [TestMethod]
    public async Task Submit_WhenOneBatchResultFails_ShouldFallbackOnlyThatRequest()
    {
        var singleCalls = new ConcurrentBag<string>();
        var queries = new ConcurrentBag<CompiledQuery>();
        using var coordinator = CreateCoordinator(
            requests => requests
                .Select(request => request.Key.EndsWith("1", StringComparison.Ordinal)
                    ? new ExecutionBatchCompilationResult(
                        request.Key,
                        BuildResult.Failure([], request.Script, new FormatException("batch failure")))
                    : CreateSuccess(request, queries))
                .ToArray(),
            request =>
            {
                singleCalls.Add(request.Key);
                return CreateSuccess(request, queries).Result;
            },
            collectionWindow: TimeSpan.FromMilliseconds(100));

        var start = new Barrier(2);
        var results = await Task.WhenAll(Enumerable.Range(0, 2).Select(index => Task.Run(() =>
        {
            start.SignalAndWait();
            return coordinator.Submit($"query-{index}", new EmptySchemaProvider(), new TestsLoggerResolver(), new CompilationOptions());
        })));

        Assert.IsTrue(results.Single(result => !result.WasBatched).Result.Succeeded);
        Assert.IsTrue(results.Single(result => result.WasBatched).Result.Succeeded);
        Assert.HasCount(1, singleCalls);
        StringAssert.EndsWith(singleCalls.Single(), "1");
        foreach (var query in queries)
            query.Dispose();
    }

    [TestMethod]
    public async Task Dispose_WhenRequestsAreWaiting_ShouldCompleteThemThroughSinglePath()
    {
        var singleCalls = 0;
        var queries = new ConcurrentBag<CompiledQuery>();
        using var requestEnqueued = new ManualResetEventSlim();
        using var coordinator = CreateCoordinator(
            _ => throw new AssertFailedException("The pending request must not be batched after shutdown."),
            request =>
            {
                Interlocked.Increment(ref singleCalls);
                return CreateSuccess(request, queries).Result;
            },
            collectionWindow: TimeSpan.FromSeconds(10),
            requestEnqueued: requestEnqueued.Set);

        var resultTask = Task.Run(() => coordinator.Submit(
            "shutdown-query",
            new EmptySchemaProvider(),
            new TestsLoggerResolver(),
            new CompilationOptions()));
        Assert.IsTrue(
            requestEnqueued.Wait(TimeSpan.FromSeconds(30)),
            "The request was not enqueued before the coordinator shutdown test continued.");

        coordinator.Dispose();
        var result = await resultTask;

        Assert.IsFalse(result.WasBatched);
        Assert.AreEqual(1, singleCalls);
        foreach (var query in queries)
            query.Dispose();
    }

    [TestMethod]
    public async Task BasicCoordinator_WhenProvidersDiffer_ShouldNeverMixSentinelRows()
    {
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(index => Task.Run(() =>
        {
            var provider = new BasicSchemaProvider<BasicEntity>(
                new Dictionary<string, IEnumerable<BasicEntity>>
                {
                    ["#A"] = [new BasicEntity($"sentinel-{index}")]
                });
            var result = StableTypedExecutionCompilationCoordinator.Submit(
                "select e.Name from #A.Entities() e",
                provider,
                new TestsLoggerResolver(),
                new CompilationOptions(usePrimitiveTypeValidation: false));
            Assert.IsTrue(result.Result.Succeeded);
            return (result, expected: $"sentinel-{index}");
        })));

        foreach (var item in results)
        {
            using var query = item.result.Result.CompiledQuery!;
            using var table = query.Run();
            Assert.AreEqual(1, table.Count);
            Assert.AreEqual(item.expected, table[0][0]);
        }
    }

    [TestMethod]
    public void BinaryTextSpecifications_ShouldUseTheTrackedCompilationHelper()
    {
        var root = RepositorySourceScan.RepositoryRoot();
        var testFiles = Directory
            .EnumerateFiles(root, "BinaryOrTextual*.cs", SearchOption.TopDirectoryOnly)
            .Where(file => !file.EndsWith(
                "BinaryOrTextual.SchemaFeaturesTests.Regressions.cs",
                StringComparison.Ordinal))
            .ToArray();
        var directCompilationCallers = testFiles
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("InstanceCreator.CompileForExecution(", StringComparison.Ordinal) ||
                       text.Contains("InstanceCreator.CompileWithDiagnostics(", StringComparison.Ordinal);
            })
            .ToArray();

        Assert.IsEmpty(
            directCompilationCallers,
            "Positive Binary/Text specification tests must use the tracked batching helper; " +
            string.Join(", ", directCompilationCallers));

        var baseText = File.ReadAllText(Path.Combine(root, "src", "dotnet", "Musoq.Evaluator.Tests", "BinaryOrTextualEvaluatorTestBase.cs"));
        StringAssert.Contains(baseText, "stable-interpretation-specification");
    }

    [TestMethod]
    public void ProductionCompilation_ShouldNotReferenceTheTestCoordinator()
    {
        var repositoryRoot = RepositorySourceScan.RepositoryRoot();
        var converterRoot = Path.Combine(repositoryRoot, "src", "dotnet", "Musoq.Converter");
        var productionText = string.Join(
            "\n",
            Directory.EnumerateFiles(converterRoot, "*.cs", SearchOption.AllDirectories)
                .Select(File.ReadAllText));

        Assert.IsFalse(
            productionText.Contains(nameof(StableTypedExecutionCompilationCoordinator), StringComparison.Ordinal),
            "The test-only compilation coordinator must not leak into production compilation.");
    }

    private static ExecutionCompilationBatchCoordinator CreateCoordinator(
        Func<IReadOnlyList<ExecutionBatchCompilationRequest>, IReadOnlyList<ExecutionBatchCompilationResult>> batchCompiler,
        Func<ExecutionBatchCompilationRequest, BuildResult> singleCompiler,
        TimeSpan collectionWindow,
        int maximumBatchSize = 8,
        Action? requestEnqueued = null)
    {
        return new ExecutionCompilationBatchCoordinator(
            batchCompiler,
            singleCompiler,
            maximumBatchSize,
            collectionWindow: collectionWindow,
            requestEnqueued: requestEnqueued);
    }

    private static ExecutionBatchCompilationResult CreateSuccess(
        ExecutionBatchCompilationRequest request,
        ConcurrentBag<CompiledQuery> queries)
    {
        var query = new CompiledQuery(new EmptyRunnable());
        queries.Add(query);
        return new ExecutionBatchCompilationResult(
            request.Key,
            BuildResult.Success(query, [], request.Script));
    }

    private sealed class EmptyRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new EmptySchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>
            SourceRuntimeSettingsBySourceContextId { get; set; } = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
            SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } = new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

        public IReadOnlyDictionary<string, SourceExecutionPlan> SourceExecutionPlans { get; set; } =
            new Dictionary<string, SourceExecutionPlan>();

        public ILogger Logger { get; set; } = NullLogger.Instance;

#pragma warning disable CS0067
        public event QueryPhaseEventHandler? PhaseChanged;
        public event DataSourceEventHandler? DataSourceProgress;
#pragma warning restore CS0067

        public Table Run(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return new Table("empty", []);
        }
    }

    private sealed class EmptySchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) => throw new NotSupportedException();
    }
}
