using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class EvaluationHelperParallelRowsTests
{
    [TestMethod]
    public void ProjectRowsParallel_WhenRowsFitTwentyChunks_ShouldUseTwentyShards()
    {
        var rows = Enumerable.Range(0, 10_000).ToArray();

        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            rows,
            24,
            value => new TestRow([value]),
            CancellationToken.None);

        Assert.HasCount(20, shards);
        Assert.AreEqual(10_000, shards.Sum(static shard => shard.Count));
    }

    [TestMethod]
    public void ProjectRowsParallel_WhenRowsFitSingleChunk_ShouldAvoidExtraShards()
    {
        var rows = Enumerable.Range(0, 512).ToArray();

        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            rows,
            24,
            value => new TestRow([value]),
            CancellationToken.None);

        Assert.HasCount(1, shards);
        Assert.AreEqual(512, shards[0].Count);
    }

    [TestMethod]
    public void ProjectRowsParallel_WithPredicate_ShouldPublishExactShardCounts()
    {
        var rows = Enumerable.Range(0, 10_000).ToArray();

        var shards = EvaluationHelper.ProjectRowsParallel<int, TestRow>(
            rows,
            24,
            static value => value % 2 == 0,
            value => new TestRow([value]),
            CancellationToken.None);

        var projectedValues = shards
            .SelectMany(static shard => shard)
            .Select(static row => (int)row[0])
            .OrderBy(static value => value)
            .ToArray();

        Assert.HasCount(20, shards);
        Assert.HasCount(5000, projectedValues);
        CollectionAssert.AreEqual(
            Enumerable.Range(0, 5000).Select(static value => value * 2).ToArray(),
            projectedValues);
    }
}
