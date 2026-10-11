using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Extended tests to improve branch coverage for low-coverage areas
/// </summary>
[TestClass]
public partial class RowAndKeyEqualityTests : Musoq.Evaluator.Tests.Schema.Generic.GenericEntityTestBase
{
    #region Row Tests

    [TestMethod]
    public void Row_ToString_ReturnsFormattedString()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new TestRow(values);

        var str = row.ToString();

        Assert.IsNotNull(str);
    }

    [TestMethod]
    public void TestRow_Indexer_ReturnsCorrectValue()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new TestRow(values);

        Assert.AreEqual(1, row.ReadValue(0));
        Assert.AreEqual("test", row.ReadValue(1));
        Assert.AreEqual(3.14, row.ReadValue(2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsTrueForEqualRows()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForDifferentCounts()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForDifferentValues()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([2, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForNull()
    {
        var row = new TestRow([1]);

        Assert.IsFalse(row.Equals(null));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsTrueForEqualRows()
    {
        var row1 = new TestRow([1, "test"]);
        object row2 = new TestRow([1, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsFalseForNull()
    {
        var row = new TestRow([1]);

        Assert.IsFalse(row.Equals((object?)null));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsFalseForDifferentType()
    {
        var row = new TestRow([1]);

        Assert.IsFalse(row.Equals("not a row"));
    }

    [TestMethod]
    public void Row_GetHashCode_ReturnsSameForEqualRows()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1, "test"]);

        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    [TestMethod]
    public void Row_Equals_BothWithNullValues_ReturnsTrue()
    {
        var row1 = new TestRow([null!, "test"]);
        var row2 = new TestRow([null!, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OneNullOneNotNull_ReturnsFalse()
    {
        var row1 = new TestRow([null!, "test"]);
        var row2 = new TestRow([1, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OtherValueNull_ReturnsFalse()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([null!, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void TestRow_Contexts_ReturnsSetContexts()
    {
        var values = new object[] { 1 };
        var contexts = new object[] { "context1" };
        var row = new TestRow(values, contexts);

        var actualContexts = row.Contexts;
        Assert.IsNotNull(actualContexts);
        Assert.AreEqual("context1", actualContexts[0]);
    }

    #endregion

}
