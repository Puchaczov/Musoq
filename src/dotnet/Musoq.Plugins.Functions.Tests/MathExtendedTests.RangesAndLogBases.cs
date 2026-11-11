using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class MathExtendedTests
{
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
        Assert.IsTrue(LibraryBase.IsBetween(5, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntOutOfRange_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetween(15, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntAtMin_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(0, 0, 10));
    }

    [TestMethod]
    public void IsBetween_IntAtMax_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(10, 0, 10));
    }

    [TestMethod]
    public void IsBetween_NullLong_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0L, 10L));
    }

    [TestMethod]
    public void IsBetween_LongInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5L, 0L, 10L));
    }

    [TestMethod]
    public void IsBetween_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0m, 10m));
    }

    [TestMethod]
    public void IsBetween_DecimalInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5m, 0m, 10m));
    }

    [TestMethod]
    public void IsBetween_NullDouble_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetween(null, 0.0, 10.0));
    }

    [TestMethod]
    public void IsBetween_DoubleInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetween(5.0, 0.0, 10.0));
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
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_IntAtMin_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(0, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_IntAtMax_ReturnsFalse()
    {
        Assert.IsFalse(LibraryBase.IsBetweenExclusive(10, 0, 10));
    }

    [TestMethod]
    public void IsBetweenExclusive_NullDecimal_ReturnsNull()
    {
        Assert.IsNull(LibraryBase.IsBetweenExclusive(null, 0m, 10m));
    }

    [TestMethod]
    public void IsBetweenExclusive_DecimalInRange_ReturnsTrue()
    {
        Assert.IsTrue(LibraryBase.IsBetweenExclusive(5m, 0m, 10m));
    }

    #endregion
}
