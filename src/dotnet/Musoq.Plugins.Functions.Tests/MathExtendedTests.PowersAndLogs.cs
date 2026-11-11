using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathExtendedTests
{
    #region PercentOf Tests

    [TestMethod]
    public void PercentOf_NullValue_ReturnsNull()
    {
        Assert.IsNull(Library.PercentOf(null, 100m));
    }

    [TestMethod]
    public void PercentOf_NullTotal_ReturnsNull()
    {
        Assert.IsNull(Library.PercentOf(50m, null));
    }

    [TestMethod]
    public void PercentOf_ValidValues_ReturnsPercent()
    {
        Assert.AreEqual(50m, Library.PercentOf(50m, 100m));
    }

    #endregion

    #region Rand Tests

    [TestMethod]
    public void Rand_NullMin_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(null, 100));
    }

    [TestMethod]
    public void Rand_NullMax_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(0, null));
    }

    [TestMethod]
    public void Rand_ValidRange_ReturnsWithinRange()
    {
        var result = Library.Rand(0, 100);
        Assert.IsNotNull(result);
        Assert.IsTrue(result is >= 0 and < 100);
    }

    #endregion

    #region Pow Tests

    [TestMethod]
    public void Pow_NullX_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(null, 2m));
    }

    [TestMethod]
    public void Pow_NullY_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(2m, null));
    }

    [TestMethod]
    public void Pow_ValidDecimal_ReturnsPower()
    {
        Assert.AreEqual(8.0, Library.Pow(2m, 3m));
    }

    #endregion

    #region Sqrt Tests

    [TestMethod]
    public void Sqrt_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((decimal?)null));
    }

    [TestMethod]
    public void Sqrt_ValidDecimal_ReturnsSqrt()
    {
        Assert.AreEqual(4.0, Library.Sqrt(16m));
    }

    [TestMethod]
    public void Sqrt_NullDouble_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((double?)null));
    }

    [TestMethod]
    public void Sqrt_ValidDouble_ReturnsSqrt()
    {
        Assert.AreEqual(4.0, Library.Sqrt(16.0));
    }

    [TestMethod]
    public void Sqrt_NullLong_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt(null));
    }

    [TestMethod]
    public void Sqrt_ValidLong_ReturnsSqrt()
    {
        Assert.AreEqual(4.0, Library.Sqrt(16L));
    }

    #endregion

    #region Log Tests (with base)

    [TestMethod]
    public void Log_NullBase_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(null, 10m));
    }

    [TestMethod]
    public void Log_NullValue_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, null));
    }

    [TestMethod]
    public void Log_BaseZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(0m, 10m));
    }

    [TestMethod]
    public void Log_BaseNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(-1m, 10m));
    }

    [TestMethod]
    public void Log_BaseOne_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(1m, 10m));
    }

    [TestMethod]
    public void Log_ValueZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, 0m));
    }

    [TestMethod]
    public void Log_ValueNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, -1m));
    }

    [TestMethod]
    public void Log_ValidValues_ReturnsLog()
    {
        var result = LibraryBase.Log(10m, 100m);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    #endregion

    #region Exp Tests (static)

    [TestMethod]
    public void Exp_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((decimal?)null));
    }

    [TestMethod]
    public void Exp_ValidDecimal_ReturnsExp()
    {
        var result = LibraryBase.Exp(1m);
        Assert.IsNotNull(result);
        Assert.AreEqual((decimal)Math.E, result.Value, 0.0001m);
    }

    [TestMethod]
    public void Exp_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((double?)null));
    }

    [TestMethod]
    public void Exp_ValidDouble_ReturnsExp()
    {
        var result = LibraryBase.Exp(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(Math.E, result.Value, 0.0001);
    }

    #endregion
}
