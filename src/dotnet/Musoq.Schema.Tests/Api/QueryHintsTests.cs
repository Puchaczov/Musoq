using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Schema.Api;

namespace Musoq.Schema.Tests.Api;

/// <summary>
///     Tests for QueryHints class which provides query optimization hints.
/// </summary>
[TestClass]
public class QueryHintsTests
{
    #region WithDistinct Helper Method Tests

    [TestMethod]
    public void WithDistinct_ShouldSetIsDistinct()
    {
        var hints = QueryHints.WithDistinct();

        Assert.IsNull(hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsTrue(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    #endregion

    #region Empty Tests

    [TestMethod]
    public void Empty_ShouldHaveNoOptimizationHints()
    {
        var hints = QueryHints.Empty;

        Assert.IsFalse(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void Empty_SkipValue_ShouldBeNull()
    {
        var hints = QueryHints.Empty;

        Assert.IsNull(hints.SkipValue);
    }

    [TestMethod]
    public void Empty_TakeValue_ShouldBeNull()
    {
        var hints = QueryHints.Empty;

        Assert.IsNull(hints.TakeValue);
    }

    [TestMethod]
    public void Empty_IsDistinct_ShouldBeFalse()
    {
        var hints = QueryHints.Empty;

        Assert.IsFalse(hints.IsDistinct);
    }

    [TestMethod]
    public void Empty_EffectiveMaxRowsToFetch_ShouldBeNull()
    {
        var hints = QueryHints.Empty;

        Assert.IsNull(hints.EffectiveMaxRowsToFetch);
    }

    #endregion

    #region Create Factory Method Tests

    [TestMethod]
    public void Create_WithNoParameters_ShouldBeEquivalentToEmpty()
    {
        var hints = QueryHints.Create();

        Assert.IsFalse(hints.HasOptimizationHints);
        Assert.IsNull(hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
    }

    [TestMethod]
    public void Create_WithSkipValue_ShouldSetSkipValue()
    {
        var hints = QueryHints.Create(10);

        Assert.AreEqual(10, hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void Create_WithTakeValue_ShouldSetTakeValue()
    {
        var hints = QueryHints.Create(take: 50);

        Assert.IsNull(hints.SkipValue);
        Assert.AreEqual(50, hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void Create_WithSkipAndTake_ShouldSetBothValues()
    {
        var hints = QueryHints.Create(20, 100);

        Assert.AreEqual(20, hints.SkipValue);
        Assert.AreEqual(100, hints.TakeValue);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void Create_WithIsDistinct_ShouldSetIsDistinct()
    {
        var hints = QueryHints.Create(isDistinct: true);

        Assert.IsNull(hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsTrue(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void Create_WithAllParameters_ShouldSetAllValues()
    {
        var hints = QueryHints.Create(5, 25, true);

        Assert.AreEqual(5, hints.SkipValue);
        Assert.AreEqual(25, hints.TakeValue);
        Assert.IsTrue(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    #endregion

    #region EffectiveMaxRowsToFetch Tests

    [TestMethod]
    public void EffectiveMaxRowsToFetch_WithOnlyTake_ShouldReturnTakeValue()
    {
        var hints = QueryHints.Create(take: 50);

        Assert.AreEqual(50, hints.EffectiveMaxRowsToFetch);
    }

    [TestMethod]
    public void EffectiveMaxRowsToFetch_WithSkipAndTake_ShouldReturnSum()
    {
        var hints = QueryHints.Create(10, 50);

        Assert.AreEqual(60, hints.EffectiveMaxRowsToFetch);
    }

    [TestMethod]
    public void EffectiveMaxRowsToFetch_WithOnlySkip_ShouldReturnNull()
    {
        var hints = QueryHints.Create(10);

        Assert.IsNull(hints.EffectiveMaxRowsToFetch);
    }

    [TestMethod]
    public void EffectiveMaxRowsToFetch_WithZeroSkip_ShouldReturnTakeValue()
    {
        var hints = QueryHints.Create(0, 100);

        Assert.AreEqual(100, hints.EffectiveMaxRowsToFetch);
    }

    [TestMethod]
    public void EffectiveMaxRowsToFetch_LargeValues_ShouldHandleCorrectly()
    {
        var hints = QueryHints.Create(1000000, 5000000);

        Assert.AreEqual(6000000, hints.EffectiveMaxRowsToFetch);
    }

    #endregion

    #region HasOptimizationHints Tests

    [TestMethod]
    public void HasOptimizationHints_WithSkipOnly_ShouldReturnTrue()
    {
        var hints = QueryHints.Create(1);

        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void HasOptimizationHints_WithTakeOnly_ShouldReturnTrue()
    {
        var hints = QueryHints.Create(take: 1);

        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void HasOptimizationHints_WithDistinctOnly_ShouldReturnTrue()
    {
        var hints = QueryHints.Create(isDistinct: true);

        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void HasOptimizationHints_WithNoHints_ShouldReturnFalse()
    {
        var hints = QueryHints.Create();

        Assert.IsFalse(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void HasOptimizationHints_WithZeroSkip_ShouldReturnTrue()
    {
        var hints = QueryHints.Create(0);

        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void HasOptimizationHints_WithZeroTake_ShouldReturnTrue()
    {
        var hints = QueryHints.Create(take: 0);

        Assert.IsTrue(hints.HasOptimizationHints);
    }

    #endregion

    #region WithSkipAndTake Helper Method Tests

    [TestMethod]
    public void WithSkipAndTake_ShouldSetBothValues()
    {
        var hints = QueryHints.WithSkipAndTake(15, 75);

        Assert.AreEqual(15, hints.SkipValue);
        Assert.AreEqual(75, hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void WithSkipAndTake_NullSkip_ShouldOnlySetTake()
    {
        var hints = QueryHints.WithSkipAndTake(null, 50);

        Assert.IsNull(hints.SkipValue);
        Assert.AreEqual(50, hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void WithSkipAndTake_NullTake_ShouldOnlySetSkip()
    {
        var hints = QueryHints.WithSkipAndTake(25, null);

        Assert.AreEqual(25, hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsTrue(hints.HasOptimizationHints);
    }

    [TestMethod]
    public void WithSkipAndTake_BothNull_ShouldHaveNoOptimizationHints()
    {
        var hints = QueryHints.WithSkipAndTake(null, null);

        Assert.IsNull(hints.SkipValue);
        Assert.IsNull(hints.TakeValue);
        Assert.IsFalse(hints.IsDistinct);
        Assert.IsFalse(hints.HasOptimizationHints);
    }

    #endregion
}
