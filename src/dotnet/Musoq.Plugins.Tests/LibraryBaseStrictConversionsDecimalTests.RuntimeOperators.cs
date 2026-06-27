using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region Runtime Operator Tests

    [TestMethod]
    public void InternalApplyAddOperator_WithInts_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2, 3);

        Assert.IsNotNull(result);
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyAddOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithOneNull_ShouldReturnNull()
    {
        var result = Library.InternalApplyAddOperator(2, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithDecimals_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2.5m, 3.5m);

        Assert.IsNotNull(result);
        Assert.AreEqual(6.0m, result);
    }

    [TestMethod]
    public void InternalApplyAddOperator_WithDoubles_ShouldAdd()
    {
        var result = Library.InternalApplyAddOperator(2.5, 3.5);

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void InternalApplySubtractOperator_WithInts_ShouldSubtract()
    {
        var result = Library.InternalApplySubtractOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.AreEqual(2L, result);
    }

    [TestMethod]
    public void InternalApplySubtractOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplySubtractOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyMultiplyOperator_WithInts_ShouldMultiply()
    {
        var result = Library.InternalApplyMultiplyOperator(4, 5);

        Assert.IsNotNull(result);
        Assert.AreEqual(20L, result);
    }

    [TestMethod]
    public void InternalApplyMultiplyOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyMultiplyOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_WithInts_ShouldDivide()
    {
        var result = Library.InternalApplyDivideOperator(10, 2);

        Assert.IsNotNull(result);
        Assert.AreEqual(5L, result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyDivideOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalApplyDivideOperator_DivisionByZero_ThrowsException()
    {
        var exceptionThrown = false;
        try
        {
            Library.InternalApplyDivideOperator(10, 0);
        }
        catch (DivideByZeroException)
        {
            exceptionThrown = true;
        }

        Assert.IsTrue(exceptionThrown, "DivideByZeroException should be thrown");
    }

    [TestMethod]
    public void InternalApplyModuloOperator_WithInts_ShouldGetRemainder()
    {
        var result = Library.InternalApplyModuloOperator(10, 3);

        Assert.IsNotNull(result);
        Assert.AreEqual(1L, result);
    }

    [TestMethod]
    public void InternalApplyModuloOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalApplyModuloOperator(null, null);

        Assert.IsNull(result);
    }

    #endregion
}
