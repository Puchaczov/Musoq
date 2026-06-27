using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for math methods in LibraryBaseMath.cs to improve branch coverage
/// </summary>
[TestClass]
public partial class MathExtendedTests : PluginsTestBase
{
    #region Abs Tests

    [TestMethod]
    public void Abs_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Abs((decimal?)null));
    }

    [TestMethod]
    public void Abs_PositiveDecimal_ReturnsPositive()
    {
        Assert.AreEqual(5.5m, Library.Abs(5.5m));
    }

    [TestMethod]
    public void Abs_NegativeDecimal_ReturnsPositive()
    {
        Assert.AreEqual(5.5m, Library.Abs(-5.5m));
    }

    [TestMethod]
    public void Abs_ZeroDecimal_ReturnsZero()
    {
        Assert.AreEqual(0m, Library.Abs(0m));
    }

    [TestMethod]
    public void Abs_NullInt_ReturnsNull()
    {
        Assert.IsNull(Library.Abs(null));
    }

    [TestMethod]
    public void Abs_NegativeInt_ReturnsPositive()
    {
        Assert.AreEqual(5, Library.Abs(-5));
    }

    [TestMethod]
    public void Abs_NullLong_ReturnsNull()
    {
        Assert.IsNull(Library.Abs((long?)null));
    }

    [TestMethod]
    public void Abs_NegativeLong_ReturnsPositive()
    {
        Assert.AreEqual(5L, Library.Abs(-5L));
    }

    #endregion

    #region Ceil Tests

    [TestMethod]
    public void Ceil_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Ceil(null));
    }

    [TestMethod]
    public void Ceil_PositiveDecimalFraction_ReturnsCeiling()
    {
        Assert.AreEqual(6m, Library.Ceil(5.3m));
    }

    [TestMethod]
    public void Ceil_NegativeDecimalFraction_ReturnsCeiling()
    {
        Assert.AreEqual(-5m, Library.Ceil(-5.3m));
    }

    #endregion

    #region Floor Tests

    [TestMethod]
    public void Floor_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Floor(null));
    }

    [TestMethod]
    public void Floor_PositiveDecimalFraction_ReturnsFloor()
    {
        Assert.AreEqual(5m, Library.Floor(5.7m));
    }

    [TestMethod]
    public void Floor_NegativeDecimalFraction_ReturnsFloor()
    {
        Assert.AreEqual(-6m, Library.Floor(-5.3m));
    }

    #endregion

    #region Sign Tests

    [TestMethod]
    public void Sign_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Sign((decimal?)null));
    }

    [TestMethod]
    public void Sign_PositiveDecimal_ReturnsOne()
    {
        Assert.AreEqual(1m, Library.Sign(5.5m));
    }

    [TestMethod]
    public void Sign_NegativeDecimal_ReturnsMinusOne()
    {
        Assert.AreEqual(-1m, Library.Sign(-5.5m));
    }

    [TestMethod]
    public void Sign_ZeroDecimal_ReturnsZero()
    {
        Assert.AreEqual(0m, Library.Sign(0m));
    }

    [TestMethod]
    public void Sign_NullLong_ReturnsNull()
    {
        Assert.IsNull(Library.Sign(null));
    }

    [TestMethod]
    public void Sign_PositiveLong_ReturnsOne()
    {
        Assert.AreEqual(1L, Library.Sign(5L));
    }

    [TestMethod]
    public void Sign_NegativeLong_ReturnsMinusOne()
    {
        Assert.AreEqual(-1L, Library.Sign(-5L));
    }

    [TestMethod]
    public void Sign_ZeroLong_ReturnsZero()
    {
        Assert.AreEqual(0L, Library.Sign(0L));
    }

    #endregion

    #region Round Tests

    [TestMethod]
    public void Round_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Round(null, 2));
    }

    [TestMethod]
    public void Round_ValidDecimal_RoundsToPlaces()
    {
        Assert.AreEqual(5.56m, Library.Round(5.555m, 2));
    }

    #endregion

}
