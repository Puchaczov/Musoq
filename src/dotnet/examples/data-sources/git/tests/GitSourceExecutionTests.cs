using Musoq.Evaluator.Diagnostics;
using Musoq.Schema;
using Musoq.Schema.Diagnostics;
using Musoq.Schema.Optimization;

namespace Musoq.Examples.DataSources.Git.Tests;

[TestClass]
public sealed class GitSourceExecutionTests : GitExampleTestBase
{
    [TestMethod]
    public void Source_WhenExecutionPlanHasAcceptedOperations_ShouldApplyThem()
    {
        var predicate = new SourcePredicateComparison(
            SourcePredicateComparisonOperator.Equal,
            new SourcePredicateColumn(new SourceColumnRef(nameof(GitCommitRow.AuthorName))),
            new SourcePredicateLiteral("Bob Evaluator"));
        var plan = SourceExecutionPlan.Empty(SourceIdentity.Empty) with
        {
            AcceptedPredicate = predicate,
            AcceptedOrderBy =
            [
                new OrderByExpression(
                    new SourceColumnRef(nameof(GitCommitRow.AuthoredAt)),
                    OrderDirection.Descending)
            ],
            AcceptedTake = 1
        };
        var context = CreateExecutionContext(plan);
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(1, rows.Length);
        Assert.AreEqual("docs", rows[0].Repository);
        Assert.AreEqual("Refresh runtime docs", rows[0].Subject);
    }

    [TestMethod]
    public void Source_WhenExecutionPlanContainsRepositoryProperty_ShouldUseItWhenNoArgumentIsProvided()
    {
        var plan = SourceExecutionPlan.Empty(SourceIdentity.Empty) with
        {
            Properties = new Dictionary<string, object?>
            {
                [GitSourcePlanProperties.Repository] = "docs"
            }
        };
        var context = CreateExecutionContext(plan);
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(2, rows.Length);
        Assert.IsTrue(rows.All(static row => row.Repository == "docs"));
    }

    [TestMethod]
    public void Source_WhenArgumentAndPlanRepositoryAreProvided_ShouldPreferArgument()
    {
        var plan = SourceExecutionPlan.Empty(SourceIdentity.Empty) with
        {
            Properties = new Dictionary<string, object?>
            {
                [GitSourcePlanProperties.Repository] = "docs"
            }
        };
        var context = CreateExecutionContext(plan);
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context, "musoq");

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(3, rows.Length);
        Assert.IsTrue(rows.All(static row => row.Repository == "musoq"));
    }


    [TestMethod]
    public void Source_WhenEnumerated_ShouldReportProgress()
    {
        var phases = new List<DataSourcePhase>();
        var context = CreateExecutionContext(
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            (_, args) => phases.Add(args.Phase));
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();

        Assert.AreEqual(5, rows.Length);
        CollectionAssert.AreEqual(
            new[]
            {
                DataSourcePhase.Begin,
                DataSourcePhase.RowsKnown,
                DataSourcePhase.RowsRead,
                DataSourcePhase.End
            },
            phases);
    }

    [TestMethod]
    public void Source_WhenCancellationIsRequestedBeforeEnumeration_ShouldThrowWithoutStarting()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var phases = new List<DataSourcePhase>();
        var context = CreateExecutionContext(
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            (_, args) => phases.Add(args.Phase),
            cancellation.Token);
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        Assert.Throws<OperationCanceledException>(() => source.Chunks.SelectMany(static chunk => chunk).ToArray());

        Assert.AreEqual(0, phases.Count);
    }

    [TestMethod]
    public void Source_WhenCancellationIsRequestedDuringEnumeration_ShouldReportCompletedChunk()
    {
        using var cancellation = new CancellationTokenSource();
        var events = new List<DataSourceEventArgs>();
        var context = CreateExecutionContext(
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            (_, args) =>
            {
                events.Add(args);
                if (args is { Phase: DataSourcePhase.RowsRead, RowsProcessed: >= 32 })
                    cancellation.Cancel();
            },
            cancellation.Token);
        var source = new GitSchema(CreateLargeStore(40)).GetRowSource<GitCommitRow>(GitSchema.Commits, context);
        var rows = new List<GitCommitRow>();

        Assert.Throws<OperationCanceledException>(() =>
        {
            foreach (var chunk in source.Chunks)
                rows.AddRange(chunk);
        });

        Assert.IsTrue(
            rows.Count is 0 or 32,
            $"The consumer should either observe cancellation before draining the buffered chunk or after receiving the completed chunk. Actual rows: {rows.Count}.");
        Assert.AreEqual(DataSourcePhase.End, events.Last().Phase);
        Assert.AreEqual(32, events.Last().RowsProcessed);
    }

    [TestMethod]
    public void Source_WhenDiagnosticsAreEnabled_ShouldRecordChunkMetrics()
    {
        var recorder = new SourceProfileRecorder("git-example", StopwatchProfileClock.Instance);
        var context = CreateExecutionContext(
            SourceExecutionPlan.Empty(SourceIdentity.Empty),
            diagnostics: recorder.CreateDiagnostics());
        var source = new GitSchema().GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        var rows = source.Chunks.SelectMany(static chunk => chunk).ToArray();
        var snapshot = recorder.CreateSnapshot();

        Assert.AreEqual(5, rows.Length);
        Assert.AreEqual(5, snapshot.RowsProduced);
        AssertMetric(snapshot, DiagnosticChunkMetricNames.RowsProduced, 5);
        AssertMetric(snapshot, DiagnosticChunkMetricNames.RowsConsumed, 5);
        AssertMetric(snapshot, DiagnosticChunkMetricNames.ChunksProduced, 1);
        AssertMetric(snapshot, DiagnosticChunkMetricNames.ChunksConsumed, 1);
    }

    [TestMethod]
    public void Source_WhenStoreThrows_ShouldSurfaceProducerException()
    {
        var context = CreateExecutionContext(SourceExecutionPlan.Empty(SourceIdentity.Empty));
        var source = new GitSchema(new ThrowingGitHistoryStore())
            .GetRowSource<GitCommitRow>(GitSchema.Commits, context);

        var exception = Assert.Throws<InvalidOperationException>(() => source.Chunks.ToArray());

        Assert.Contains("Synthetic store failure", exception.Message);
    }
}
