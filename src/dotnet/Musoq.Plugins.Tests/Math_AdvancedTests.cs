using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathTests
{
    #region Tan Tests

    [TestMethod]
    public void Tan_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((decimal?)null));
    }

    [TestMethod]
    public void Tan_Decimal_Zero_ReturnsZero()
    {
        var result = LibraryBase.Tan(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Tan_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Tan((double?)null));
    }

    [TestMethod]
    public void Tan_Double_Zero_ReturnsZero()
    {
        var result = LibraryBase.Tan(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region Exp Tests

    [TestMethod]
    public void Exp_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((decimal?)null));
    }

    [TestMethod]
    public void Exp_Decimal_Zero_ReturnsOne()
    {
        var result = LibraryBase.Exp(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(1m, result);
    }

    [TestMethod]
    public void Exp_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Exp((double?)null));
    }

    [TestMethod]
    public void Exp_Double_Zero_ReturnsOne()
    {
        var result = LibraryBase.Exp(0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Exp_Double_One_ReturnsE()
    {
        var result = LibraryBase.Exp(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(Math.E, result.Value, 0.0001);
    }

    #endregion

    #region Ln Tests

    [TestMethod]
    public void Ln_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((decimal?)null));
    }

    [TestMethod]
    public void Ln_Decimal_One_ReturnsZero()
    {
        var result = LibraryBase.Ln(1m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result);
    }

    [TestMethod]
    public void Ln_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Ln((double?)null));
    }

    [TestMethod]
    public void Ln_Double_One_ReturnsZero()
    {
        var result = LibraryBase.Ln(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Ln_Double_E_ReturnsOne()
    {
        var result = LibraryBase.Ln(Math.E);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result.Value, 0.0001);
    }

    #endregion

    #region Clamp Tests

    [TestMethod]
    public void Clamp_Int_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(null, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, null, 10));
    }

    [TestMethod]
    public void Clamp_Int_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Clamp(5, 0, null));
    }

    [TestMethod]
    public void Clamp_Int_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5, LibraryBase.Clamp(5, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_ValueBelowMin_ReturnsMin()
    {
        Assert.AreEqual(0, LibraryBase.Clamp(-5, 0, 10));
    }

    [TestMethod]
    public void Clamp_Int_ValueAboveMax_ReturnsMax()
    {
        Assert.AreEqual(10, LibraryBase.Clamp(15, 0, 10));
    }

    [TestMethod]
    public void Clamp_Decimal_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5.5m, LibraryBase.Clamp(5.5m, 0m, 10m));
    }

    [TestMethod]
    public void Clamp_Double_ValueInRange_ReturnsValue()
    {
        Assert.AreEqual(5.5, LibraryBase.Clamp(5.5, 0.0, 10.0));
    }

    #endregion

    #region LogBase Tests

    [TestMethod]
    public void LogBase_WhenValueNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase(null, 10.0));
    }

    [TestMethod]
    public void LogBase_WhenBaseNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase(100.0, null));
    }

    [TestMethod]
    public void LogBase_Base10_ReturnsCorrectValue()
    {
        var result = LibraryBase.LogBase(100.0, 10.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void LogBase_Base2_ReturnsCorrectValue()
    {
        var result = LibraryBase.LogBase(8.0, 2.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    #endregion

    #region Log10 Tests

    [TestMethod]
    public void Log10_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log10(null));
    }

    [TestMethod]
    public void Log10_Of100_Returns2()
    {
        var result = LibraryBase.Log10(100.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log10_Of1_ReturnsZero()
    {
        var result = LibraryBase.Log10(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region Log2 Tests

    [TestMethod]
    public void Log2_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log2(null));
    }

    [TestMethod]
    public void Log2_Of8_Returns3()
    {
        var result = LibraryBase.Log2(8.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Log2_Of1_ReturnsZero()
    {
        var result = LibraryBase.Log2(1.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    #endregion

    #region IsBetween Tests

    [TestMethod]
    public void IsBetween_Int_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 1, 10));
        Assert.IsNull(LibraryBase.IsBetween(5, null, 10));
        Assert.IsNull(LibraryBase.IsBetween(5, 1, null));
    }

    [TestMethod]
    public void IsBetween_Int_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5, 1, 10));
        Assert.IsTrue(LibraryBase.IsBetween(1, 1, 10));
        Assert.IsTrue(LibraryBase.IsBetween(10, 1, 10));
    }

    [TestMethod]
    public void IsBetween_Int_WhenOutOfRange_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetween(0, 1, 10));
        Assert.IsFalse(LibraryBase.IsBetween(11, 1, 10));
    }

    [TestMethod]
    public void IsBetween_Long_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5L, 1L, 10L));
    }

    [TestMethod]
    public void IsBetween_Decimal_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5.5m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetween_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetween_Double_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5.5, 1.0, 10.0));
    }

    [TestMethod]
    public void IsBetween_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 1.0, 10.0));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(null, 1, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5, 1, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenAtBoundary_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(1, 1, 10));
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(10, 1, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5.5m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenAtBoundary_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(1.0m, 1.0m, 10.0m));
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(10.0m, 1.0m, 10.0m));
    }

    [TestMethod]
    public void IsBetweenExclusive_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(null, 1.0m, 10.0m));
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5.0m, null, 10.0m));
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5.0m, 1.0m, null));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenMinNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, null, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_Int_WhenMaxNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, 1, null));
    }

    [TestMethod]
    public void IsBetween_Long_WhenNull_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 1L, 10L));
        Assert.IsNull(LibraryBase.IsBetween(5L, null, 10L));
        Assert.IsNull(LibraryBase.IsBetween(5L, 1L, null));
    }

    #endregion

    #region Rand Tests

    [TestMethod]
    public void Rand_ReturnsInteger()
    {
        var result = Library.Rand();
        Assert.IsGreaterThanOrEqualTo(0, result);
    }

    [TestMethod]
    public void Rand_WithRange_ReturnsValueInRange()
    {
        var result = Library.Rand(5, 10);
        Assert.IsNotNull(result);
        Assert.IsTrue(result >= 5 && result < 10);
    }

    [TestMethod]
    public void Rand_WithMinNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(null, 10));
    }

    [TestMethod]
    public void Rand_WithMaxNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(5, null));
    }

    [TestMethod]
    public void Rand_WithBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Rand(null, null));
    }

    #endregion

    #region Pow Tests

    [TestMethod]
    public void Pow_Decimal_WhenXNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(null, 2m));
    }

    [TestMethod]
    public void Pow_Decimal_WhenYNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(2m, null));
    }

    [TestMethod]
    public void Pow_Decimal_WhenBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(null, (decimal?)null));
    }

    [TestMethod]
    public void Pow_Decimal_ValidValues_ReturnsCorrectResult()
    {
        var result = Library.Pow(2m, 3m);
        Assert.IsNotNull(result);
        Assert.AreEqual(8.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Pow_Double_WhenXNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(null, 2.0));
    }

    [TestMethod]
    public void Pow_Double_WhenYNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(2.0, null));
    }

    [TestMethod]
    public void Pow_Double_WhenBothNull_ReturnsNull()
    {
        Assert.IsNull(Library.Pow(null, (double?)null));
    }

    [TestMethod]
    public void Pow_Double_ValidValues_ReturnsCorrectResult()
    {
        var result = Library.Pow(2.0, 3.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(8.0, result);
    }

    [TestMethod]
    public void Pow_Double_Zero_ReturnsOne()
    {
        var result = Library.Pow(5.0, 0.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(1.0, result);
    }

    [TestMethod]
    public void Pow_Double_Fractional_ReturnsCorrectResult()
    {
        var result = Library.Pow(4.0, 0.5);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    #endregion

    #region Sqrt Tests

    [TestMethod]
    public void Sqrt_Decimal_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((decimal?)null));
    }

    [TestMethod]
    public void Sqrt_Decimal_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(4m);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sqrt_Decimal_Zero_ReturnsZero()
    {
        var result = Library.Sqrt(0m);
        Assert.IsNotNull(result);
        Assert.AreEqual(0.0, result);
    }

    [TestMethod]
    public void Sqrt_Double_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt((double?)null));
    }

    [TestMethod]
    public void Sqrt_Double_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(9.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    [TestMethod]
    public void Sqrt_Long_WhenNull_ReturnsNull()
    {
        Assert.IsNull(Library.Sqrt(null));
    }

    [TestMethod]
    public void Sqrt_Long_ValidValue_ReturnsCorrectResult()
    {
        var result = Library.Sqrt(16L);
        Assert.IsNotNull(result);
        Assert.AreEqual(4.0, result.Value, 0.0001);
    }

    #endregion

    #region PercentRank Tests

    [TestMethod]
    public void PercentRank_WhenWindowNull_ReturnsNull()
    {
        Assert.IsNull(Library.PercentRank(null, 5));
    }

    [TestMethod]
    public void PercentRank_WhenValueNull_ReturnsNull()
    {
        IEnumerable<string> window = ["a", "b", "c"];
        Assert.IsNull(Library.PercentRank(window, null));
    }

    [TestMethod]
    public void PercentRank_ValidValues_ReturnsCorrectResult()
    {
        var window = new[] { 1, 2, 3, 4, 5 };
        var result = Library.PercentRank(window, 3);
        Assert.IsNotNull(result);
    }

    #endregion
}
