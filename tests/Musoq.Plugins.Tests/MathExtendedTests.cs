using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

/// <summary>
///     Extended tests for math methods in LibraryBaseMath.cs to improve branch coverage
/// </summary>
[TestClass]
public class MathExtendedTests : LibraryBaseBaseTests
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
        Assert.IsTrue(result >= 0 && result < 100);
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

    #region LogBase Tests (static)

    [TestMethod]
    public void LogBase_NullValue_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase(null, 10.0));
    }

    [TestMethod]
    public void LogBase_NullBase_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.LogBase(100.0, null));
    }

    [TestMethod]
    public void LogBase_ValidValues_ReturnsLogBase()
    {
        var result = LibraryBase.LogBase(100.0, 10.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    #endregion

    #region Log10 Tests (static)

    [TestMethod]
    public void Log10_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log10(null));
    }

    [TestMethod]
    public void Log10_ValidDouble_ReturnsLog10()
    {
        var result = LibraryBase.Log10(100.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(2.0, result.Value, 0.0001);
    }

    #endregion

    #region Log2 Tests (static)

    [TestMethod]
    public void Log2_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.Log2(null));
    }

    [TestMethod]
    public void Log2_ValidDouble_ReturnsLog2()
    {
        var result = LibraryBase.Log2(8.0);
        Assert.IsNotNull(result);
        Assert.AreEqual(3.0, result.Value, 0.0001);
    }

    #endregion

    #region IsBetween Tests (static)

    [TestMethod]
    public void IsBetween_NullIntValue_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0, 10));
    }

    [TestMethod]
    public void IsBetween_NullIntMin_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(5, null, 10));
    }

    [TestMethod]
    public void IsBetween_NullIntMax_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(5, 0, null));
    }

    [TestMethod]
    public void IsBetween_IntInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(5, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntOutOfRange_ReturnsFalse()
    {
        Assert.AreEqual(false, LibraryBase.IsBetween(15, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntAtMin_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(0, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntAtMax_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(10, 0, 10));
    }

    [TestMethod]
    public void IsBetween_NullLong_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0L, 10L));
    }

    [TestMethod]
    public void IsBetween_LongInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(5L, 0L, 10L));
    }

    [TestMethod]
    public void IsBetween_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0m, 10m));
    }

    [TestMethod]
    public void IsBetween_DecimalInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(5m, 0m, 10m));
    }

    [TestMethod]
    public void IsBetween_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0.0, 10.0));
    }

    [TestMethod]
    public void IsBetween_DoubleInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetween(5.0, 0.0, 10.0));
    }

    #endregion

    #region IsBetweenExclusive Tests (static)

    [TestMethod]
    public void IsBetweenExclusive_NullIntValue_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(null, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_NullIntMin_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, null, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_NullIntMax_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(5, 0, null));
    }

    [TestMethod]
    public void IsBetweenExclusive_IntInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetweenExclusive(5, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_IntAtMin_ReturnsFalse()
    {
        Assert.AreEqual(false, LibraryBase.IsBetweenExclusive(0, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_IntAtMax_ReturnsFalse()
    {
        Assert.AreEqual(false, LibraryBase.IsBetweenExclusive(10, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(null, 0m, 10m));
    }

    [TestMethod]
    public void IsBetweenExclusive_DecimalInRange_ReturnsTrue()
    {
        Assert.AreEqual(true, LibraryBase.IsBetweenExclusive(5m, 0m, 10m));
    }

    #endregion
}
