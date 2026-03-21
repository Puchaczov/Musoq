using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathTests
{
    #region Log Tests

    [TestMethod]
    public void Log_WhenBaseNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(null, 100m));
    }

    [TestMethod]
    public void Log_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, null));
    }

    [TestMethod]
    public void Log_WhenBaseIsZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(0m, 100m));
    }

    [TestMethod]
    public void Log_WhenBaseIsOne_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(1m, 100m));
    }

    [TestMethod]
    public void Log_WhenBaseIsNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(-10m, 100m));
    }

    [TestMethod]
    public void Log_WhenValueIsZero_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, 0m));
    }

    [TestMethod]
    public void Log_WhenValueIsNegative_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log(10m, -100m));
    }

    [TestMethod]
    public void Log_ValidValues_ReturnsCorrectResult()
    {
        var result = LibraryBase.Log(10m, 100m);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log_Base2_ReturnsCorrectResult()
    {
        var result = LibraryBase.Log(2m, 8m);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    #endregion

    #region Sin Tests

    [TestMethod]
    public void Sin_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((decimal?)null));
    }

    [TestMethod]
    public void Sin_Decimal_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Sin_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((double?)null));
    }

    [TestMethod]
    public void Sin_Double_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Sin_Double_PiOver2_ReturnsOne()
    {
        var result = LibraryBase.Sin(Math.PI / 2);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sin_Float_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Sin((float?)null));
    }

    [TestMethod]
    public void Sin_Float_Zero_ReturnsZero()
    {
        var result = LibraryBase.Sin(0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(0f, result);
    }

    #endregion

    #region Cos Tests

    [TestMethod]
    public void Cos_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((decimal?)null));
    }

    [TestMethod]
    public void Cos_Decimal_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void Cos_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((double?)null));
    }

    [TestMethod]
    public void Cos_Double_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Cos_Double_Pi_ReturnsNegativeOne()
    {
        var result = LibraryBase.Cos(Math.PI);
        Assert.IsNotNull(result);
        Assert.AreEqual(-1.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Cos_Float_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Cos((float?)null));
    }

    [TestMethod]
    public void Cos_Float_Zero_ReturnsOne()
    {
        var result = LibraryBase.Cos(0f);
        Assert.IsNotNull(result);
        Assert.AreEqual(1f, result);
    }

    #endregion

    #region Additional Clamp Tests

    [TestMethod]
    public void Clamp_Long_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5L, null, 10L));
    }

    [TestMethod]
    public void Clamp_Long_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5L, 0L, null));
    }

    [TestMethod]
    public void Clamp_Long_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5L, LibraryBase.Clamp(5L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_ValueBelowMin_ReturnsMin()
    {
        Assert.AreEqual(0L, LibraryBase.Clamp(-5L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Long_ValueAboveMax_ReturnsMax()
    {
        Assert.AreEqual(10L, LibraryBase.Clamp(15L, 0L, 10L));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5m, null, 10m));
    }

    [TestMethod]
    public void Clamp_Decimal_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5m, 0m, null));
    }

    [TestMethod]
    public void Clamp_Double_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0.0, 10.0));
    }

    [TestMethod]
    public void Clamp_Double_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5.0, null, 10.0));
    }

    [TestMethod]
    public void Clamp_Double_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5.0, 0.0, null));
    }

    #endregion

    #region Additional Tan Tests

    [TestMethod]
    public void Tan_Decimal_PiOver4_ReturnsOne()
    {
        var result = LibraryBase.Tan((decimal)(Math.PI / 4));
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, Math.Round(result.Value, 0));
    }

    [TestMethod]
    public void Tan_Double_PiOver4_ReturnsOne()
    {
        var result = LibraryBase.Tan(Math.PI / 4);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    #endregion
}
