using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    #region IndexedList Additional Coverage Tests

    [TestMethod]
    public void Table_ContainsWithKey_ReturnsFalseForNonExistingKey()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([42]);
        table.Add(row);

        var key = new Key([999], [0]);
        var result = table.Contains(key, row);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_ContainsWithKey_ReturnsFalseForNonMatchingValue()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([42]);
        table.Add(row);

        var key = new Key([42], [0]);
        var searchRow = new TestRow([100]);
        var result = table.Contains(key, searchRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_WithNullValue_HandlesCorrectly()
    {
        var row1 = new TestRow([null!, "test"]);
        var row2 = new TestRow([null!, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_NullVsNonNull_ReturnsFalse()
    {
        var row1 = new TestRow([null!]);
        var row2 = new TestRow(["test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_NonNullVsNull_ReturnsFalse()
    {
        var row1 = new TestRow(["test"]);
        var row2 = new TestRow([null!]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_GetHashCode_HandlesNullValues()
    {
        var row = new TestRow([null!, "test"]);


        var hash = row.GetHashCode();
        Assert.AreEqual(hash, row.GetHashCode());
    }

    [TestMethod]
    public void Key_Equals_DifferentLengths_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([1, 2], [0, 1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_NullValues_HandleCorrectly()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_NullVsNonNull_ReturnsFalse()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key(["test"], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void TestRow_Count_ReturnsCorrectCount()
    {
        var row = new TestRow([1, 2, 3]);

        Assert.AreEqual(3, row.Count);
    }

    [TestMethod]
    public void TestRow_Values_ReturnsAllValues()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new TestRow(values);

        Assert.HasCount(3, row.Values);
        Assert.AreEqual(1, row.Values[0]);
    }

    #endregion
}
