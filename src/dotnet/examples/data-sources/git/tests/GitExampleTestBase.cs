using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Converter;
using Musoq.Evaluator;
using Musoq.Evaluator.Diagnostics;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git.Tests;

public abstract class GitExampleTestBase
{
    protected static readonly CompilationOptions TestCompilationOptions = new(usePrimitiveTypeValidation: false);
    protected static readonly ILoggerResolver LoggerResolver = new NullLoggerResolver();

    protected static CompiledQuery Compile(
        string query,
        GitSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileForExecution(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new GitSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static Table Run(
        string query,
        GitSchemaProvider? provider = null,
        CompilationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return Compile(query, provider, options).Run(cancellationToken);
    }

    protected static BuildResult CompileWithDiagnostics(
        string query,
        GitSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileWithDiagnostics(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new GitSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static QueryInspectionResult Inspect(
        string query,
        GitSchemaProvider? provider = null,
        CompilationOptions? options = null)
    {
        return InstanceCreator.CompileForInspection(
            query,
            Guid.NewGuid().ToString(),
            provider ?? new GitSchemaProvider(),
            LoggerResolver,
            options ?? TestCompilationOptions);
    }

    protected static CompilationOptions CreateOptionsWithRepository(string repository)
    {
        return new CompilationOptions(
            usePrimitiveTypeValidation: false,
            sourceRuntimeSettingsResolver: new StaticSettingsResolver(
                new Dictionary<string, string>
                {
                    [GitSchema.RepositoryRuntimeSetting] = repository
                }));
    }

    protected static SourceExecutionContext CreateExecutionContext(
        SourceExecutionPlan plan,
        DataSourceEventHandler? progress = null,
        CancellationToken cancellationToken = default,
        SourceDiagnostics? diagnostics = null)
    {
        return new SourceExecutionContext(
            "query",
            plan,
            cancellationToken,
            [],
            new Dictionary<string, string>(),
            NullLogger.Instance,
            progress,
            diagnostics);
    }

    protected static InMemoryGitHistoryStore CreateLargeStore(int rows)
    {
        var commits = Enumerable.Range(0, rows)
            .Select(index => new GitCommitRecord(
                "large",
                "main",
                index.ToString("D8").PadRight(40, '0'),
                "Load Tester",
                "load@example.test",
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index),
                $"Commit {index}",
                $"Synthetic commit {index}.",
                false))
            .ToArray();
        var stats = commits.ToDictionary(
            static commit => commit.Sha,
            static _ => new GitCommitStats(1, 1, 0));

        return new InMemoryGitHistoryStore(commits, stats);
    }

    protected static void AssertMetric(SourceProfileSnapshot snapshot, string metricName, long expected)
    {
        var fullName = DiagnosticChunkMetricNames.ForSource(GitCommitsSource.SourceName, metricName);
        Assert.IsTrue(snapshot.Metrics.TryGetValue(fullName, out var value), $"Metric '{fullName}' was not recorded.");
        Assert.AreEqual(expected, value, fullName);
    }

    protected static SourceMetadataContext CreateMetadataContext(
        IReadOnlyCollection<ISchemaColumn>? columns = null,
        string queryId = "query",
        IReadOnlyDictionary<string, string>? sourceRuntimeSettings = null,
        CancellationToken cancellationToken = default)
    {
        return new SourceMetadataContext(
            queryId,
            cancellationToken,
            columns ?? [],
            sourceRuntimeSettings ?? new Dictionary<string, string>(),
            NullLogger.Instance);
    }

    protected sealed record UnsupportedPredicateExpression : SourcePredicateExpression;

    protected sealed class TrackingGitHistoryStore : IGitHistoryStore
    {
        private readonly IGitHistoryStore _inner = InMemoryGitHistoryStore.CreateDefault();

        public int StatsLoadCount { get; private set; }

        public IReadOnlyList<GitCommitRecord> GetCommits(string? repository)
        {
            return _inner.GetCommits(repository);
        }

        public GitCommitStats GetStats(string sha)
        {
            StatsLoadCount += 1;
            return _inner.GetStats(sha);
        }
    }

    protected sealed class ThrowingGitHistoryStore : IGitHistoryStore
    {
        public IReadOnlyList<GitCommitRecord> GetCommits(string? repository)
        {
            throw new InvalidOperationException("Synthetic store failure.");
        }

        public GitCommitStats GetStats(string sha)
        {
            return GitCommitStats.Empty;
        }
    }

    protected sealed class StaticSettingsResolver(IReadOnlyDictionary<string, string> settings)
        : ISourceRuntimeSettingsResolver
    {
        public List<SourceRuntimeSettingsResolutionRequest> Requests { get; } = [];

        public IReadOnlyDictionary<string, string> Resolve(SourceRuntimeSettingsResolutionRequest request)
        {
            Requests.Add(request);
            return settings;
        }
    }

    private sealed class NullLoggerResolver : ILoggerResolver
    {
        public ILogger ResolveLogger()
        {
            return NullLogger.Instance;
        }

        public ILogger<T> ResolveLogger<T>()
        {
            return NullLogger<T>.Instance;
        }
    }
}
