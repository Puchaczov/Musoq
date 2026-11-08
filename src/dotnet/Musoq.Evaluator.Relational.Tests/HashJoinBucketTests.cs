using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;

namespace Musoq.Evaluator.Tests;

[TestClass]
public sealed class HashJoinBucketTests
{
    [TestMethod]
    public void HashJoinBucket_ShouldBeValueType()
    {
        Assert.IsTrue(typeof(HashJoinBucket<int>).IsValueType);
    }

    [TestMethod]
    public void GetEnumerator_WhenRowsWereAdded_ShouldReturnRowsInInsertionOrder()
    {
        var bucket = new HashJoinBucket<int>(1);
        bucket.Add(2);
        bucket.Add(3);

        var rows = new List<int>();
        foreach (var row in bucket)
            rows.Add(row);

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, rows);
    }
}
