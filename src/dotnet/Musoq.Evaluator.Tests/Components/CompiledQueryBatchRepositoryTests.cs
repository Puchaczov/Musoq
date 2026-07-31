using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator.Tests.Components;

[TestClass]
public sealed class CompiledQueryBatchRepositoryTests
{
    [TestMethod]
    public void Dispose_WhenBatchWasNeverRequested_ShouldNotRunFactory()
    {
        var factoryCalls = 0;
        var repository = new CompiledQueryBatchRepository<string>(() =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new Dictionary<string, CompiledQuery>();
        });

        repository.Dispose();

        Assert.AreEqual(0, factoryCalls);
        Assert.IsFalse(repository.IsValueCreated);
    }

    [TestMethod]
    public void Take_WhenCalledConcurrentlyForSameKey_ShouldTransferOwnershipOnce()
    {
        var (query, lifetime) = CreateQuery();
        var factoryCalls = 0;
        using var repository = new CompiledQueryBatchRepository<string>(() =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new Dictionary<string, CompiledQuery> { ["case"] = query };
        });
        var taken = new ConcurrentBag<CompiledQuery>();
        var failures = new ConcurrentBag<Exception>();

        Parallel.For(0, 8, _ =>
        {
            try
            {
                taken.Add(repository.Take("case"));
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        });

        Assert.HasCount(1, taken);
        Assert.HasCount(7, failures);
        Assert.AreEqual(1, factoryCalls);
        Assert.IsTrue(failures.All(static exception => exception is InvalidOperationException));
        Assert.IsFalse(lifetime.IsDisposed);
        taken.Single().Dispose();
        Assert.IsTrue(lifetime.IsDisposed);
    }

    [TestMethod]
    public void Take_WhenOneCompiledEntryFailed_ShouldKeepSuccessfulSiblingAvailable()
    {
        var (query, lifetime) = CreateQuery();
        using var repository = new CompiledQueryBatchRepository<string>(() =>
            new Dictionary<string, CompiledQueryBatchEntry>
            {
                ["failed"] = CompiledQueryBatchEntry.Failure(new FormatException("invalid query")),
                ["successful"] = CompiledQueryBatchEntry.Success(query)
            });

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => repository.Take("failed"));
        using var successful = repository.Take("successful");

        Assert.IsInstanceOfType<FormatException>(exception.InnerException);
        Assert.IsFalse(lifetime.IsDisposed);
    }

    [TestMethod]
    public void Dispose_WhenOnlyOneFilteredRepositoryWasUsed_ShouldNotInitializeOtherRepository()
    {
        var selectedCalls = 0;
        var excludedCalls = 0;
        var (query, _) = CreateQuery();
        var selected = new CompiledQueryBatchRepository<string>(() =>
        {
            Interlocked.Increment(ref selectedCalls);
            return new Dictionary<string, CompiledQuery> { ["case"] = query };
        });
        var excluded = new CompiledQueryBatchRepository<string>(() =>
        {
            Interlocked.Increment(ref excludedCalls);
            return new Dictionary<string, CompiledQuery>();
        });

        using (selected)
        using (excluded)
        using (selected.Take("case"))
        {
        }

        Assert.AreEqual(1, selectedCalls);
        Assert.AreEqual(0, excludedCalls);
    }

    [TestMethod]
    public void Take_WhenKeyWasAlreadyConsumed_ShouldRejectDuplicateConsumption()
    {
        var (query, _) = CreateQuery();
        using var repository = new CompiledQueryBatchRepository<string>(() =>
            new Dictionary<string, CompiledQuery> { ["case"] = query });
        using var taken = repository.Take("case");

        var exception = Assert.ThrowsExactly<InvalidOperationException>(() => repository.Take("case"));

        Assert.Contains("already consumed", exception.Message);
    }

    [TestMethod]
    public void Take_WhenKeyWasNotProduced_ShouldReportMissingKey()
    {
        using var repository = new CompiledQueryBatchRepository<string>(() =>
            new Dictionary<string, CompiledQuery>());

        var exception = Assert.ThrowsExactly<KeyNotFoundException>(() => repository.Take("missing"));

        Assert.Contains("missing", exception.Message);
    }

    [TestMethod]
    public void Dispose_WhenSomeQueriesWereNotConsumed_ShouldDisposeOnlyUnclaimedQueries()
    {
        var (takenQuery, takenLifetime) = CreateQuery();
        var (unusedQuery, unusedLifetime) = CreateQuery();
        var repository = new CompiledQueryBatchRepository<string>(() =>
            new Dictionary<string, CompiledQuery>
            {
                ["taken"] = takenQuery,
                ["unused"] = unusedQuery
            });
        var taken = repository.Take("taken");

        repository.Dispose();
        repository.Dispose();

        Assert.IsFalse(takenLifetime.IsDisposed);
        Assert.IsTrue(unusedLifetime.IsDisposed);
        taken.Dispose();
        Assert.IsTrue(takenLifetime.IsDisposed);
    }

    private static (CompiledQuery Query, TrackingLifetime Lifetime) CreateQuery()
    {
        var lifetime = new TrackingLifetime();
        return (new CompiledQuery(new EmptyRunnable(), lifetime), lifetime);
    }

    private sealed class TrackingLifetime : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class EmptyRunnable : ITableRunnable
    {
        public ISchemaProvider Provider { get; set; } = new UnusedSchemaProvider();

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> SourceRuntimeSettingsBySourceContextId
        {
            get;
            set;
        } = new Dictionary<string, IReadOnlyDictionary<string, string>>();

        public IReadOnlyDictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>
            SourceRuntimeSettingDescriptionsBySourceContextId { get; set; } =
            new Dictionary<string, IReadOnlyList<SourceRuntimeSettingDescription>>();

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

    private sealed class UnusedSchemaProvider : ISchemaProvider
    {
        public ISchema GetSchema(string schema) =>
            throw new NotSupportedException("The repository ownership test does not execute schemas.");
    }
}
