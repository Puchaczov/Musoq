using System;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Runtime;
using Musoq.Tests.Common;

namespace Musoq.Evaluator.Tests.Runtime;

[TestClass]
public class TypedParallelRowsTests
{
    static TypedParallelRowsTests()
    {
        Culture.ApplyWithDefaultCulture();
    }

    [TestMethod]
    public void ProjectValuesParallel_ShouldPreserveDeterministicShardOrder()
    {
        var rows = Enumerable.Range(0, 10_000).ToArray();

        var shards = TypedProjectionRows.ProjectValuesParallel(
            rows,
            maxDegreeOfParallelism: 4,
            value => value % 3 == 0,
            value => value * 2,
            CancellationToken.None);

        var projected = QueryRows.FromShards(shards).ToArray();
        var expected = rows
            .Where(static value => value % 3 == 0)
            .Select(static value => value * 2)
            .ToArray();

        CollectionAssert.AreEqual(expected, projected);
    }

    [TestMethod]
    public void ProjectValuesSerial_ShouldYieldOnlyUntilConsumerStops()
    {
        var rows = new ThrowOnSecondMoveEnumerable<int>(42);

        using var enumerator = TypedProjectionRows
            .ProjectValuesSerial(rows, static _ => true, static value => value, CancellationToken.None)
            .GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(42, enumerator.Current);
    }

    private sealed class ThrowOnSecondMoveEnumerable<T>(T first) : System.Collections.Generic.IEnumerable<T>
    {
        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
        {
            yield return first;
            throw new InvalidOperationException("Second row should not be requested before the first result is consumed.");
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
