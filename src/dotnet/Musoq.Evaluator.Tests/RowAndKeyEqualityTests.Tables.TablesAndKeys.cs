using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    #region Table Additional Tests

    [TestMethod]
    public void Table_Contains_ReturnsTrueForExistingRow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([42]);
        table.Add(row);

        var result = table.Contains(row);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_ReturnsFalseForNonExistingRow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([100]);
        var result = table.Contains(searchRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_Contains_WithComparer_ReturnsTrueWhenFound()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([42]);
        table.Add(row);

        var result = table.Contains(row, (a, b) => (int)a.Values[0] == (int)b.Values[0]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_WithComparer_ReturnsFalseWhenNotFound()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([100]);
        var result = table.Contains(searchRow, (a, b) => (int)a.Values[0] == (int)b.Values[0]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_TryGetIndexedValues_ReturnsFalseForMissingKey()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var key = new Key(["missing"], [0]);

        var result = table.TryGetIndexedValues(key, out var values);

        Assert.IsFalse(result);
        Assert.IsEmpty(values);
    }

    [TestMethod]
    public void Table_AddRange_AddsMultipleRows()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var rows = new[]
        {
            new TestRow([1]),
            new TestRow([2]),
            new TestRow([3])
        };

        table.AddRange(rows);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Table_Columns_ReturnsAllColumns()
    {
        var columns = new[]
        {
            new Column("Col1", typeof(int), 0),
            new Column("Col2", typeof(string), 1)
        };
        var table = new Table("Test", columns);

        var resultColumns = new List<Column>(table.Columns);

        Assert.HasCount(2, resultColumns);
    }

    [TestMethod]
    public void Table_Name_ReturnsCorrectName()
    {
        var table = new Table("TestTable", [new Column("Col1", typeof(int), 0)]);

        Assert.AreEqual("TestTable", table.Name);
    }

    [TestMethod]
    public void Table_IndexerByKey_ReturnsMatchingRows()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([42]);
        table.Add(row);


        Assert.AreEqual(row, table[0]);
    }

    [TestMethod]
    public void Table_ContainsKey_ReturnsFalseForNonExistingKey()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var key = new Key(["missing"], [0]);

        var result = table.ContainsKey(key);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_GetEnumerator_IteratesRows()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([1]), new TestRow([2]) };

        var count = 0;
        foreach (var _ in table) count++;

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void Table_Add_WithNullValues_Succeeds()
    {
        var table = new Table("Test", [new Column("Col1", typeof(object), 0)]);
        var row = new TestRow([null!]);

        table.Add(row);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_Add_TypeMismatch_Throws()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow(["not an int"]);

        Assert.Throws<NotSupportedException>(() => table.Add(row));
    }

    [TestMethod]
    public void Table_Add_WrongColumnCount_Throws()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new TestRow([1, 2]);

        Assert.Throws<NotSupportedException>(() => table.Add(row));
    }

    #endregion

    #region Key Additional Tests

    [TestMethod]
    public void Key_GetHashCode_SameForEqualKeys()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([1, "test"], [0, 1]);

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void Key_Equals_WithObject_ReturnsTrueForEqual()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        object key2 = new Key([1, "test"], [0, 1]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_WithObject_ReturnsFalseForNull()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals((object)null!));
    }

    [TestMethod]
    public void Key_Equals_WithObject_ReturnsFalseForDifferentType()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals("not a key"));
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsTrueForMatch()
    {
        var key = new Key([1], [0]);
        var row = new TestRow([1, "test"]);

        Assert.IsTrue(key.DoesRowMatchKey(row));
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsFalseForNoMatch()
    {
        var key = new Key([2], [0]);
        var row = new TestRow([1, "test"]);

        Assert.IsFalse(key.DoesRowMatchKey(row));
    }

    #endregion

    #region Table Edge Cases

    [TestMethod]
    public void Table_Add_MultipleSameValues_Works()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([1]), new TestRow([1]), new TestRow([2]) };

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Table_GetEnumerator_Explicit_Works()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([1]) };

        var enumerable = (IEnumerable)table;
        var count = 0;
        foreach (var _ in enumerable) count++;

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void Table_ColumnByIndex_ReturnsCorrectColumn()
    {
        var columns = new[]
        {
            new Column("Col1", typeof(int), 0),
            new Column("Col2", typeof(string), 1)
        };
        var table = new Table("Test", columns);

        Assert.AreEqual("Col1", table.Columns.First().ColumnName);
        Assert.AreEqual("Col2", table.Columns.Skip(1).First().ColumnName);
    }

    [TestMethod]
    public void Table_Add_MismatchedColumnCount_Throws()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);

        var ex = Assert.Throws<NotSupportedException>(() =>
            table.Add(new TestRow([1, 2])));

        Assert.Contains("2 values", ex.Message);
    }

    [TestMethod]
    public void Table_Add_MismatchedType_Throws()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);

        var ex = Assert.Throws<NotSupportedException>(() =>
            table.Add(new TestRow(["string"])));

        Assert.Contains("Mismatched", ex.Message);
    }

    [TestMethod]
    public void Table_Add_NullValue_Skips_TypeCheck()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns) { new TestRow([null!]) };

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_ContainsKey_ReturnsFalse_WhenKeyNotFound()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns) { new TestRow([1]) };

        var key = new Key([999], [0]);


        Assert.IsFalse(table.ContainsKey(key));
    }

    [TestMethod]
    public void Table_TryGetIndexedValues_ReturnsFalse_WhenNoIndex()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns) { new TestRow([42]) };

        var key = new Key([42], [0]);


        var result = table.TryGetIndexedValues(key, out var values);

        Assert.IsFalse(result);
        Assert.IsNotNull(values);
    }

    [TestMethod]
    public void Table_IndexerByIndex_ReturnsCorrectRow()
    {
        var table = new Table("Test", [new Column("Col", typeof(int), 0)]) { new TestRow([100]) };

        var row = table[0];

        Assert.AreEqual(100, row[0]);
    }

    #endregion
}
