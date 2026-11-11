using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Plugins.Tests;

public partial class LibraryBaseStrictConversionsDecimalTests
{
    #region Comparison Operator Tests

    [TestMethod]
    public void InternalGreaterThanOperator_WhenGreater_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WhenLess_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOperator(3, 5);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WhenEqual_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOperator(5, 5);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalGreaterThanOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalLessThanOperator_WhenLess_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOperator(3, 5);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOperator_WhenGreater_ShouldReturnFalse()
    {
        var result = Library.InternalLessThanOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOperator_WithNulls_ShouldReturnNull()
    {
        var result = Library.InternalLessThanOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenGreater_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(5, 5);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalGreaterThanOrEqualOperator_WhenLess_ShouldReturnFalse()
    {
        var result = Library.InternalGreaterThanOrEqualOperator(3, 5);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenLess_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOrEqualOperator(3, 5);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalLessThanOrEqualOperator(5, 5);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalLessThanOrEqualOperator_WhenGreater_ShouldReturnFalse()
    {
        var result = Library.InternalLessThanOrEqualOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WhenEqual_ShouldReturnTrue()
    {
        var result = Library.InternalEqualOperator(5, 5);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WhenNotEqual_ShouldReturnFalse()
    {
        var result = Library.InternalEqualOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalEqualOperator_WithNulls_ReturnsUnknown()
    {
        var result = Library.InternalEqualOperator(null, null);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WhenNotEqual_ShouldReturnTrue()
    {
        var result = Library.InternalNotEqualOperator(5, 3);

        Assert.IsNotNull(result);
        Assert.IsTrue(result.Value);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WhenEqual_ShouldReturnFalse()
    {
        var result = Library.InternalNotEqualOperator(5, 5);

        Assert.IsNotNull(result);
        Assert.IsFalse(result.Value);
    }

    [TestMethod]
    public void InternalNotEqualOperator_WithNulls_ReturnsUnknown()
    {
        var result = Library.InternalNotEqualOperator(null, null);

        Assert.IsNull(result);
    }

    #endregion
}
