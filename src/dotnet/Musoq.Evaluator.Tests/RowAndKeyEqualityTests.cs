using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Extended tests to improve branch coverage for low-coverage areas
/// </summary>
[TestClass]
public partial class RowAndKeyEqualityTests
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

        Assert.AreEqual(1, row[0]);
        Assert.AreEqual("test", row[1]);
        Assert.AreEqual(3.14, row[2]);
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
    public void Row_FitsTheIndex_ReturnsTrueForMatchingKey()
    {
        var row = new TestRow([1, "test", 3]);
        var key = new Key([1], [0]);

        Assert.IsTrue(row.FitsTheIndex(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_ReturnsTrueForMatchingKey()
    {
        var row = new TestRow([1, "test", 3]);
        var key = new Key([1, "test"], [0, 1]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_ReturnsFalseForNonMatchingKey()
    {
        var row = new TestRow([1, "test", 3]);
        var key = new Key([2, "test"], [0, 1]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_BothNullValues_ReturnsTrue()
    {
        var row = new TestRow([null!, "test"]);
        var key = new Key([null], [0]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_RowNullKeyNotNull_ReturnsFalse()
    {
        var row = new TestRow([null!, "test"]);
        var key = new Key([1], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_RowNotNullKeyNull_ReturnsFalse()
    {
        var row = new TestRow([1, "test"]);
        var key = new Key([null], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
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

    #region Key Tests - Additional Coverage

    [TestMethod]
    public void Key_Equals_DifferentColumnLengths_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([1, 2], [0, 1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_BothNullValues_ReturnsTrue()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ThisNullOtherNotNull_ReturnsFalse()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ThisNotNullOtherNull_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ObjectNull_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals((object?)null));
    }

    [TestMethod]
    public void Key_Equals_ObjectSameReference_ReturnsTrue()
    {
        var key1 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals((object)key1));
    }

    [TestMethod]
    public void Key_Equals_ObjectDifferentType_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals("not a key"));
    }

    [TestMethod]
    public void Key_Equals_ObjectSameValue_ReturnsTrue()
    {
        var key1 = new Key([1], [0]);
        object key2 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_ToString_SingleColumn()
    {
        var key = new Key([42], [0]);

        var result = key.ToString();

        Assert.AreEqual("0(42)", result);
    }

    [TestMethod]
    public void Key_ToString_MultipleColumns()
    {
        var key = new Key([1, "test"], [0, 1]);

        var result = key.ToString();

        Assert.Contains("0(1)", result);
        Assert.Contains("1(test)", result);
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsTrue()
    {
        var key = new Key([1], [0]);
        var row = new TestRow([1, "test"]);

        Assert.IsTrue(key.DoesRowMatchKey(row));
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsFalse()
    {
        var key = new Key([2], [0]);
        var row = new TestRow([1, "test"]);

        Assert.IsFalse(key.DoesRowMatchKey(row));
    }

    [TestMethod]
    public void Key_GetHashCode_HandlesNullValues()
    {
        var key = new Key([null, 2], [0, 1]);

        var hash = key.GetHashCode();
        Assert.AreEqual(hash, key.GetHashCode());
    }

    [TestMethod]
    public void Key_DifferentColumns_NotEqual()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([1], [1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    #endregion

}
