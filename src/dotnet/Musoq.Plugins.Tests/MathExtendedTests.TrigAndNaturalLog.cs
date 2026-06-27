using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathExtendedTests
{
    #region Trig Tests (static)

    [TestMethod]
    public void Sin_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((decimal?)null));
    }

    [TestMethod]
    public void Sin_ValidDecimal_ReturnsSin()
    {
        var result = LibraryBase.Sin((decimal?)Math.PI);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, (double)result.Value, 0.0001);
    }

    [TestMethod]
    public void Sin_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((double?)null));
    }

    [TestMethod]
    public void Sin_ValidDouble_ReturnsSin()
    {
        var result = LibraryBase.Sin(Math.PI);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sin_NullFloat_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((float?)null));
    }

    [TestMethod]
    public void Sin_ValidFloat_ReturnsSin()
    {
        var result = LibraryBase.Sin((float?)Math.PI);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value, 0.0001f);
    }

    [TestMethod]
    public void Cos_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((decimal?)null));
    }

    [TestMethod]
    public void Cos_ValidDecimal_ReturnsCos()
    {
        var result = LibraryBase.Cos(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, (double)result.Value, 0.0001);
    }

    [TestMethod]
    public void Cos_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((double?)null));
    }

    [TestMethod]
    public void Cos_ValidDouble_ReturnsCos()
    {
        var result = LibraryBase.Cos(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value, 0.0001);
    }

    [TestMethod]
    public void Cos_NullFloat_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((float?)null));
    }

    [TestMethod]
    public void Cos_ValidFloat_ReturnsCos()
    {
        var result = LibraryBase.Cos(0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Value, 0.0001f);
    }

    [TestMethod]
    public void Tan_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((decimal?)null));
    }

    [TestMethod]
    public void Tan_ValidDecimal_ReturnsTan()
    {
        var result = LibraryBase.Tan(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, (double)result.Value, 0.0001);
    }

    [TestMethod]
    public void Tan_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((double?)null));
    }

    [TestMethod]
    public void Tan_ValidDouble_ReturnsTan()
    {
        var result = LibraryBase.Tan(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Value, 0.0001);
    }

    #endregion

    #region Ln Tests (natural log - static)

    [TestMethod]
    public void Ln_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((decimal?)null));
    }

    [TestMethod]
    public void Ln_ValidDecimal_ReturnsLn()
    {
        var result = LibraryBase.Ln((decimal?)Math.E);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, (double)result.Value, 0.0001);
    }

    [TestMethod]
    public void Ln_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((double?)null));
    }

    [TestMethod]
    public void Ln_ValidDouble_ReturnsLn()
    {
        var result = LibraryBase.Ln(Math.E);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    #endregion

    #region Clamp Tests (static)

    [TestMethod]
    public void Clamp_NullInt_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0, 10));
    }

    [TestMethod]
    public void Clamp_NullMinInt_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, null, 10));
    }

    [TestMethod]
    public void Clamp_NullMaxInt_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, 0, null));
    }

    [TestMethod]
    public void Clamp_IntBelowMin_ReturnsMin()
    {
        Assert.AreEqual(0, LibraryBase.Clamp(-5, 0, 10));
    }

    [TestMethod]
    public void Clamp_IntAboveMax_ReturnsMax()
    {
        Assert.AreEqual(10, LibraryBase.Clamp(15, 0, 10));
    }

    [TestMethod]
    public void Clamp_IntInRange_ReturnsValue()
    {
        Assert.AreEqual(5, LibraryBase.Clamp(5, 0, 10));
    }

    [TestMethod]
    public void Clamp_NullLong_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_LongInRange_ReturnsValue()
    {
        Assert.AreEqual(5L, LibraryBase.Clamp(5L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_DecimalInRange_ReturnsValue()
    {
        Assert.AreEqual(5m, LibraryBase.Clamp(5m, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0.0, 10.0));
    }

    [TestMethod]
    public void Clamp_DoubleInRange_ReturnsValue()
    {
        Assert.AreEqual(5.0, LibraryBase.Clamp(5.0, 0.0, 10.0));
    }

    #endregion
}
