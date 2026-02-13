using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
/// Tests for DISTINCT aggregate functions: CountDistinct, SumDistinct, AvgDistinct, MinDistinct, MaxDistinct
/// </summary>
[TestClass]
public class DistinctAggregatesTests : LibraryBaseBaseTests
{
    #region CountDistinct Tests

    [TestMethod]
    public void CountDistinct_IntValues_ShouldCountUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1);
        Library.SetDistinctAggregate(Group, "test", 1); // duplicate
        Library.SetDistinctAggregate(Group, "test", 2);
        Library.SetDistinctAggregate(Group, "test", 3);
        Library.SetDistinctAggregate(Group, "test", 2); // duplicate

        Assert.AreEqual(3, Library.CountDistinct(Group, "test"));
    }

    [TestMethod]
    public void CountDistinct_IntWithNull_ShouldExcludeNulls()
    {
        Library.SetDistinctAggregate(Group, "test", 1);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 2);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 1); // duplicate

        Assert.AreEqual(2, Library.CountDistinct(Group, "test"));
    }

    [TestMethod]
    public void CountDistinct_StringValues_ShouldCountUnique()
    {
        Library.SetDistinctAggregate(Group, "test", "a");
        Library.SetDistinctAggregate(Group, "test", "a"); // duplicate
        Library.SetDistinctAggregate(Group, "test", "b");
        Library.SetDistinctAggregate(Group, "test", "c");
        Library.SetDistinctAggregate(Group, "test", "b"); // duplicate

        Assert.AreEqual(3, Library.CountDistinct(Group, "test"));
    }

    [TestMethod]
    public void CountDistinct_DecimalValues_ShouldCountUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1.5m);
        Library.SetDistinctAggregate(Group, "test", 1.5m); // duplicate
        Library.SetDistinctAggregate(Group, "test", 2.5m);
        Library.SetDistinctAggregate(Group, "test", 3.5m);

        Assert.AreEqual(3, Library.CountDistinct(Group, "test"));
    }

    [TestMethod]
    public void CountDistinct_AllDuplicates_ShouldReturnOne()
    {
        Library.SetDistinctAggregate(Group, "test", "same");
        Library.SetDistinctAggregate(Group, "test", "same");
        Library.SetDistinctAggregate(Group, "test", "same");

        Assert.AreEqual(1, Library.CountDistinct(Group, "test"));
    }

    #endregion

    #region SumDistinct Tests

    [TestMethod]
    public void SumDistinct_IntValues_ShouldSumUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1);
        Library.SetDistinctAggregate(Group, "test", 1); // duplicate, should not be summed again
        Library.SetDistinctAggregate(Group, "test", 2);
        Library.SetDistinctAggregate(Group, "test", 3);
        Library.SetDistinctAggregate(Group, "test", 2); // duplicate

        Assert.AreEqual(6m, Library.SumDistinct(Group, "test")); // 1 + 2 + 3 = 6
    }

    [TestMethod]
    public void SumDistinct_DecimalValues_ShouldSumUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1.5m);
        Library.SetDistinctAggregate(Group, "test", 1.5m); // duplicate
        Library.SetDistinctAggregate(Group, "test", 2.5m);
        Library.SetDistinctAggregate(Group, "test", 3.5m);

        Assert.AreEqual(7.5m, Library.SumDistinct(Group, "test")); // 1.5 + 2.5 + 3.5 = 7.5
    }

    [TestMethod]
    public void SumDistinct_NullValues_ShouldExcludeNulls()
    {
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 20);
        Library.SetDistinctAggregate(Group, "test", (int?)null);

        Assert.AreEqual(30m, Library.SumDistinct(Group, "test")); // 10 + 20 = 30
    }

    [TestMethod]
    public void SumDistinct_DoubleValues_ShouldSumUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1.1);
        Library.SetDistinctAggregate(Group, "test", 1.1); // duplicate
        Library.SetDistinctAggregate(Group, "test", 2.2);
        Library.SetDistinctAggregate(Group, "test", 3.3);

        Assert.AreEqual(6.6m, Library.SumDistinct(Group, "test")); // 1.1 + 2.2 + 3.3 = 6.6
    }

    [TestMethod]
    public void SumDistinct_LongValues_ShouldSumUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 100L);
        Library.SetDistinctAggregate(Group, "test", 100L); // duplicate
        Library.SetDistinctAggregate(Group, "test", 200L);

        Assert.AreEqual(300m, Library.SumDistinct(Group, "test")); // 100 + 200 = 300
    }

    #endregion

    #region AvgDistinct Tests

    [TestMethod]
    public void AvgDistinct_IntValues_ShouldAverageUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", 10); // duplicate
        Library.SetDistinctAggregate(Group, "test", 20);
        Library.SetDistinctAggregate(Group, "test", 30);
        Library.SetDistinctAggregate(Group, "test", 20); // duplicate

        Assert.AreEqual(20m, Library.AvgDistinct(Group, "test")); // (10 + 20 + 30) / 3 = 20
    }

    [TestMethod]
    public void AvgDistinct_DecimalValues_ShouldAverageUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1.0m);
        Library.SetDistinctAggregate(Group, "test", 2.0m);
        Library.SetDistinctAggregate(Group, "test", 3.0m);

        Assert.AreEqual(2.0m, Library.AvgDistinct(Group, "test")); // (1 + 2 + 3) / 3 = 2
    }

    [TestMethod]
    public void AvgDistinct_NullValues_ShouldExcludeNulls()
    {
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 30);

        Assert.AreEqual(20m, Library.AvgDistinct(Group, "test")); // (10 + 30) / 2 = 20
    }

    [TestMethod]
    public void AvgDistinct_DoubleValues_ShouldAverageUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 1.5);
        Library.SetDistinctAggregate(Group, "test", 1.5); // duplicate
        Library.SetDistinctAggregate(Group, "test", 4.5);

        Assert.AreEqual(3.0m, Library.AvgDistinct(Group, "test")); // (1.5 + 4.5) / 2 = 3
    }

    #endregion

    #region MinDistinct Tests

    [TestMethod]
    public void MinDistinct_IntValues_ShouldFindMinUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 5);
        Library.SetDistinctAggregate(Group, "test", 5); // duplicate
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", 3);
        Library.SetDistinctAggregate(Group, "test", 10); // duplicate

        Assert.AreEqual(3m, Library.MinDistinct(Group, "test"));
    }

    [TestMethod]
    public void MinDistinct_DecimalValues_ShouldFindMinUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 5.5m);
        Library.SetDistinctAggregate(Group, "test", 2.2m);
        Library.SetDistinctAggregate(Group, "test", 7.7m);

        Assert.AreEqual(2.2m, Library.MinDistinct(Group, "test"));
    }

    [TestMethod]
    public void MinDistinct_NullValues_ShouldExcludeNulls()
    {
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 5);

        Assert.AreEqual(5m, Library.MinDistinct(Group, "test"));
    }

    [TestMethod]
    public void MinDistinct_NegativeValues_ShouldFindMinUnique()
    {
        Library.SetDistinctAggregate(Group, "test", -5);
        Library.SetDistinctAggregate(Group, "test", -5); // duplicate
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", -10);

        Assert.AreEqual(-10m, Library.MinDistinct(Group, "test"));
    }

    #endregion

    #region MaxDistinct Tests

    [TestMethod]
    public void MaxDistinct_IntValues_ShouldFindMaxUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 5);
        Library.SetDistinctAggregate(Group, "test", 5); // duplicate
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", 3);
        Library.SetDistinctAggregate(Group, "test", 10); // duplicate

        Assert.AreEqual(10m, Library.MaxDistinct(Group, "test"));
    }

    [TestMethod]
    public void MaxDistinct_DecimalValues_ShouldFindMaxUnique()
    {
        Library.SetDistinctAggregate(Group, "test", 5.5m);
        Library.SetDistinctAggregate(Group, "test", 2.2m);
        Library.SetDistinctAggregate(Group, "test", 7.7m);

        Assert.AreEqual(7.7m, Library.MaxDistinct(Group, "test"));
    }

    [TestMethod]
    public void MaxDistinct_NullValues_ShouldExcludeNulls()
    {
        Library.SetDistinctAggregate(Group, "test", 10);
        Library.SetDistinctAggregate(Group, "test", (int?)null);
        Library.SetDistinctAggregate(Group, "test", 15);

        Assert.AreEqual(15m, Library.MaxDistinct(Group, "test"));
    }

    [TestMethod]
    public void MaxDistinct_NegativeValues_ShouldFindMaxUnique()
    {
        Library.SetDistinctAggregate(Group, "test", -5);
        Library.SetDistinctAggregate(Group, "test", -5); // duplicate
        Library.SetDistinctAggregate(Group, "test", -2);
        Library.SetDistinctAggregate(Group, "test", -10);

        Assert.AreEqual(-2m, Library.MaxDistinct(Group, "test"));
    }

    #endregion

    #region Mixed Type Tests

    [TestMethod]
    public void DistinctAggregates_ByteValues_ShouldWork()
    {
        Library.SetDistinctAggregate(Group, "test", (byte)10);
        Library.SetDistinctAggregate(Group, "test", (byte)10); // duplicate
        Library.SetDistinctAggregate(Group, "test", (byte)20);

        Assert.AreEqual(2, Library.CountDistinct(Group, "test"));
        Assert.AreEqual(30m, Library.SumDistinct(Group, "test"));
        Assert.AreEqual(15m, Library.AvgDistinct(Group, "test"));
    }

    [TestMethod]
    public void DistinctAggregates_ShortValues_ShouldWork()
    {
        Library.SetDistinctAggregate(Group, "test", (short)100);
        Library.SetDistinctAggregate(Group, "test", (short)100); // duplicate
        Library.SetDistinctAggregate(Group, "test", (short)200);

        Assert.AreEqual(2, Library.CountDistinct(Group, "test"));
        Assert.AreEqual(300m, Library.SumDistinct(Group, "test"));
    }

    [TestMethod]
    public void DistinctAggregates_FloatValues_ShouldWork()
    {
        Library.SetDistinctAggregate(Group, "test", 1.5f);
        Library.SetDistinctAggregate(Group, "test", 1.5f); // duplicate
        Library.SetDistinctAggregate(Group, "test", 2.5f);

        Assert.AreEqual(2, Library.CountDistinct(Group, "test"));
        Assert.AreEqual(4m, Library.SumDistinct(Group, "test"));
    }

    [TestMethod]
    public void DistinctAggregates_WithParentGroup_ShouldWork()
    {
        Library.SetDistinctAggregate(Group, "test", 10, 1);
        Library.SetDistinctAggregate(Group, "test", 10, 1); // duplicate at parent level
        Library.SetDistinctAggregate(Group, "test", 20, 1);
        Library.SetDistinctAggregate(Group, "test", 5); // at current level
        Library.SetDistinctAggregate(Group, "test", 5); // duplicate at current level
        Library.SetDistinctAggregate(Group, "test", 15);

        // Parent level should have 10 + 20 = 30
        Assert.AreEqual(2, Library.CountDistinct(Group, "test", 1));
        Assert.AreEqual(30m, Library.SumDistinct(Group, "test", 1));

        // Current level should have 5 + 15 = 20
        Assert.AreEqual(2, Library.CountDistinct(Group, "test"));
        Assert.AreEqual(20m, Library.SumDistinct(Group, "test"));
    }

    #endregion
}
