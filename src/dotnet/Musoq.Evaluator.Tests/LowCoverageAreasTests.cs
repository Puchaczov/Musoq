using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Utils.Symbols;
using Musoq.Parser.Nodes;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Extended tests to improve branch coverage for low-coverage areas
/// </summary>
[TestClass]
public class LowCoverageAreasTests
{
    #region Row Tests

    [TestMethod]
    public void Row_ToString_ReturnsFormattedString()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new ObjectsRow(values);

        var str = row.ToString();

        Assert.IsNotNull(str);
    }

    [TestMethod]
    public void ObjectsRow_Indexer_ReturnsCorrectValue()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new ObjectsRow(values);

        Assert.AreEqual(1, row[0]);
        Assert.AreEqual("test", row[1]);
        Assert.AreEqual(3.14, row[2]);
    }

    [TestMethod]
    public void Row_Equals_ReturnsTrueForEqualRows()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForDifferentCounts()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForDifferentValues()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([2, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ReturnsFalseForNull()
    {
        var row = new ObjectsRow([1]);

        Assert.IsFalse(row.Equals(null));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsTrueForEqualRows()
    {
        var row1 = new ObjectsRow([1, "test"]);
        object row2 = new ObjectsRow([1, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsFalseForNull()
    {
        var row = new ObjectsRow([1]);

        Assert.IsFalse(row.Equals((object)null));
    }

    [TestMethod]
    public void Row_EqualsObject_ReturnsFalseForDifferentType()
    {
        var row = new ObjectsRow([1]);

        Assert.IsFalse(row.Equals("not a row"));
    }

    [TestMethod]
    public void Row_GetHashCode_ReturnsSameForEqualRows()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    [TestMethod]
    public void Row_FitsTheIndex_ReturnsTrueForMatchingKey()
    {
        var row = new ObjectsRow([1, "test", 3]);
        var key = new Key([1], [0]);

        Assert.IsTrue(row.FitsTheIndex(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_ReturnsTrueForMatchingKey()
    {
        var row = new ObjectsRow([1, "test", 3]);
        var key = new Key([1, "test"], [0, 1]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_ReturnsFalseForNonMatchingKey()
    {
        var row = new ObjectsRow([1, "test", 3]);
        var key = new Key([2, "test"], [0, 1]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_BothNullValues_ReturnsTrue()
    {
        var row = new ObjectsRow([null, "test"]);
        var key = new Key([null], [0]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_RowNullKeyNotNull_ReturnsFalse()
    {
        var row = new ObjectsRow([null, "test"]);
        var key = new Key([1], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_RowNotNullKeyNull_ReturnsFalse()
    {
        var row = new ObjectsRow([1, "test"]);
        var key = new Key([null], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_Equals_BothWithNullValues_ReturnsTrue()
    {
        var row1 = new ObjectsRow([null, "test"]);
        var row2 = new ObjectsRow([null, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OneNullOneNotNull_ReturnsFalse()
    {
        var row1 = new ObjectsRow([null, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OtherValueNull_ReturnsFalse()
    {
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([null, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void ObjectsRow_Contexts_ReturnsSetContexts()
    {
        var values = new object[] { 1 };
        var contexts = new object[] { "context1" };
        var row = new ObjectsRow(values, contexts);

        Assert.IsNotNull(row.Contexts);
        Assert.AreEqual("context1", row.Contexts[0]);
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

        Assert.IsFalse(key1.Equals((object)null));
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
        var row = new ObjectsRow([1, "test"]);

        Assert.IsTrue(key.DoesRowMatchKey(row));
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsFalse()
    {
        var key = new Key([2], [0]);
        var row = new ObjectsRow([1, "test"]);

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

    #region Table Additional Tests

    [TestMethod]
    public void Table_Contains_ReturnsTrueForExistingRow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow([42]);
        table.Add(row);

        var result = table.Contains(row);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_ReturnsFalseForNonExistingRow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([42]));

        var searchRow = new ObjectsRow([100]);
        var result = table.Contains(searchRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_Contains_WithComparer_ReturnsTrueWhenFound()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow([42]);
        table.Add(row);

        var result = table.Contains(row, (a, b) => (int)a.Values[0] == (int)b.Values[0]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_WithComparer_ReturnsFalseWhenNotFound()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([42]));

        var searchRow = new ObjectsRow([100]);
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
            new ObjectsRow([1]),
            new ObjectsRow([2]),
            new ObjectsRow([3])
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
        var row = new ObjectsRow([42]);
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
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([2]));

        var count = 0;
        foreach (var row in table) count++;

        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void Table_Add_WithNullValues_Succeeds()
    {
        var table = new Table("Test", [new Column("Col1", typeof(object), 0)]);
        var row = new ObjectsRow([null]);

        table.Add(row);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_Add_TypeMismatch_Throws()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow(["not an int"]);

        Assert.Throws<NotSupportedException>(() => table.Add(row));
    }

    [TestMethod]
    public void Table_Add_WrongColumnCount_Throws()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow([1, 2]);

        Assert.Throws<NotSupportedException>(() => table.Add(row));
    }

    #endregion

    #region GroupRow Tests

    [TestMethod]
    public void GroupRow_Indexer_ReturnsValue()
    {
        var group = new Group(null, ["col1"], [1]);
        var columnMap = new Dictionary<int, string> { { 0, "val1" } };
        group.GetOrCreateValue("val1", () => 42);

        var groupRow = new GroupRow(group, columnMap);

        Assert.AreEqual(42, groupRow[0]);
    }

    [TestMethod]
    public void GroupRow_Count_ReturnsColumnCount()
    {
        var group = new Group(null, ["col1"], [1]);
        var columnMap = new Dictionary<int, string>
        {
            { 0, "val1" },
            { 1, "val2" }
        };
        group.GetOrCreateValue("val1", () => 42);
        group.GetOrCreateValue("val2", () => "hello");

        var groupRow = new GroupRow(group, columnMap);

        Assert.AreEqual(2, groupRow.Count);
    }

    [TestMethod]
    public void GroupRow_Values_ReturnsAllValues()
    {
        var group = new Group(null, ["col1"], [1]);
        var columnMap = new Dictionary<int, string>
        {
            { 0, "val1" }
        };
        group.GetOrCreateValue("val1", () => 42);

        var groupRow = new GroupRow(group, columnMap);

        var values = groupRow.Values;

        Assert.HasCount(1, values);
        Assert.AreEqual(42, values[0]);
    }

    [TestMethod]
    public void GroupRow_Values_CachesResult()
    {
        var group = new Group(null, ["col1"], [1]);
        var columnMap = new Dictionary<int, string> { { 0, "val1" } };
        group.GetOrCreateValue("val1", () => 42);

        var groupRow = new GroupRow(group, columnMap);

        var values1 = groupRow.Values;
        var values2 = groupRow.Values;


        Assert.AreSame(values1, values2);
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

        Assert.IsFalse(key.Equals((object)null));
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
        var row = new ObjectsRow([1, "test"]);

        Assert.IsTrue(key.DoesRowMatchKey(row));
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ReturnsFalseForNoMatch()
    {
        var key = new Key([2], [0]);
        var row = new ObjectsRow([1, "test"]);

        Assert.IsFalse(key.DoesRowMatchKey(row));
    }

    #endregion

    #region TableIndex Tests

    [TestMethod]
    public void TableIndex_Constructor_SetsProperties()
    {
        var index = new TableIndex("TestColumn");

        Assert.AreEqual("TestColumn", index.ColumnName);
    }

    [TestMethod]
    public void TableIndex_Equals_ReturnsTrueForSameValues()
    {
        var index1 = new TableIndex("TestColumn");
        var index2 = new TableIndex("TestColumn");

        Assert.IsTrue(index1.Equals(index2));
    }

    [TestMethod]
    public void TableIndex_Equals_ReturnsFalseForDifferentNames()
    {
        var index1 = new TableIndex("Column1");
        var index2 = new TableIndex("Column2");

        Assert.IsFalse(index1.Equals(index2));
    }

    [TestMethod]
    public void TableIndex_Equals_ReturnsFalseForNull()
    {
        var index = new TableIndex("TestColumn");

        Assert.IsFalse(index.Equals(null));
    }

    [TestMethod]
    public void TableIndex_Equals_WithObject_ReturnsTrueForEqual()
    {
        var index1 = new TableIndex("TestColumn");
        object index2 = new TableIndex("TestColumn");

        Assert.IsTrue(index1.Equals(index2));
    }

    [TestMethod]
    public void TableIndex_Equals_WithObject_ReturnsFalseForNull()
    {
        var index = new TableIndex("TestColumn");

        Assert.IsFalse(index.Equals((object)null));
    }

    [TestMethod]
    public void TableIndex_Equals_WithObject_ReturnsFalseForDifferentType()
    {
        var index = new TableIndex("TestColumn");

        Assert.IsFalse(index.Equals("not a TableIndex"));
    }

    [TestMethod]
    public void TableIndex_Equals_ReturnsTrueForSameReference()
    {
        var index = new TableIndex("TestColumn");

        Assert.IsTrue(index.Equals(index));
    }

    [TestMethod]
    public void TableIndex_Equals_WithObject_ReturnsTrueForSameReference()
    {
        var index = new TableIndex("TestColumn");

        Assert.IsTrue(index.Equals((object)index));
    }

    [TestMethod]
    public void TableIndex_GetHashCode_SameForEqualIndexes()
    {
        var index1 = new TableIndex("TestColumn");
        var index2 = new TableIndex("TestColumn");

        Assert.AreEqual(index1.GetHashCode(), index2.GetHashCode());
    }

    [TestMethod]
    public void TableIndex_GetHashCode_WithNullColumnName_Returns0()
    {
        var index = new TableIndex(null);

        Assert.AreEqual(0, index.GetHashCode());
    }

    #endregion

    #region IndexedList Additional Coverage Tests

    [TestMethod]
    public void Table_ContainsWithKey_ReturnsFalseForNonExistingKey()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow([42]);
        table.Add(row);

        var key = new Key([999], [0]);
        var result = table.Contains(key, row);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_ContainsWithKey_ReturnsFalseForNonMatchingValue()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        var row = new ObjectsRow([42]);
        table.Add(row);

        var key = new Key([42], [0]);
        var searchRow = new ObjectsRow([100]);
        var result = table.Contains(key, searchRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_WithNullValue_HandlesCorrectly()
    {
        var row1 = new ObjectsRow([null, "test"]);
        var row2 = new ObjectsRow([null, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_NullVsNonNull_ReturnsFalse()
    {
        var row1 = new ObjectsRow([null]);
        var row2 = new ObjectsRow(["test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_NonNullVsNull_ReturnsFalse()
    {
        var row1 = new ObjectsRow(["test"]);
        var row2 = new ObjectsRow([null]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_GetHashCode_HandlesNullValues()
    {
        var row = new ObjectsRow([null, "test"]);


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
    public void ObjectsRow_Count_ReturnsCorrectCount()
    {
        var row = new ObjectsRow([1, 2, 3]);

        Assert.AreEqual(3, row.Count);
    }

    [TestMethod]
    public void ObjectsRow_Values_ReturnsAllValues()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new ObjectsRow(values);

        Assert.HasCount(3, row.Values);
        Assert.AreEqual(1, row.Values[0]);
    }

    #endregion

    #region Table Edge Cases

    [TestMethod]
    public void Table_Add_MultipleSameValues_Works()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);

        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([2]));

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Table_GetEnumerator_Explicit_Works()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);
        table.Add(new ObjectsRow([1]));

        var enumerable = (IEnumerable)table;
        var count = 0;
        foreach (var item in enumerable) count++;

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
            table.Add(new ObjectsRow([1, 2])));

        Assert.Contains("2 values", ex.Message);
    }

    [TestMethod]
    public void Table_Add_MismatchedType_Throws()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);

        var ex = Assert.Throws<NotSupportedException>(() =>
            table.Add(new ObjectsRow(["string"])));

        Assert.Contains("Mismatched", ex.Message);
    }

    [TestMethod]
    public void Table_Add_NullValue_Skips_TypeCheck()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);


        table.Add(new ObjectsRow([null]));

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_ContainsKey_ReturnsFalse_WhenKeyNotFound()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);
        table.Add(new ObjectsRow([1]));

        var key = new Key([999], [0]);


        Assert.IsFalse(table.ContainsKey(key));
    }

    [TestMethod]
    public void Table_TryGetIndexedValues_ReturnsFalse_WhenNoIndex()
    {
        var columns = new[] { new Column("Col1", typeof(int), 0) };
        var table = new Table("Test", columns);
        table.Add(new ObjectsRow([42]));

        var key = new Key([42], [0]);


        var result = table.TryGetIndexedValues(key, out var values);

        Assert.IsFalse(result);
        Assert.IsNotNull(values);
    }

    [TestMethod]
    public void Table_IndexerByIndex_ReturnsCorrectRow()
    {
        var table = new Table("Test", [new Column("Col", typeof(int), 0)]);
        table.Add(new ObjectsRow([100]));

        var row = table[0];

        Assert.AreEqual(100, row[0]);
    }

    #endregion

    #region GroupKey Tests

    [TestMethod]
    public void GroupKey_Equals_SameValues_ReturnsTrue()
    {
        var key1 = new GroupKey(1, "test");
        var key2 = new GroupKey(1, "test");

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_DifferentValues_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");
        var key2 = new GroupKey(2, "test");

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_DifferentLength_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");
        var key2 = new GroupKey(1);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_BothNullValues_ReturnsTrue()
    {
        var key1 = new GroupKey(null, "test");
        var key2 = new GroupKey(null, "test");

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_NullVsNonNull_ReturnsFalse()
    {
        var key1 = new GroupKey(null, "test");
        var key2 = new GroupKey(1, "test");

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_NonNullVsNull_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");
        var key2 = new GroupKey(null, "test");

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_Null_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");

        Assert.IsFalse(key1.Equals(null));
    }

    [TestMethod]
    public void GroupKey_Equals_SameReference_ReturnsTrue()
    {
        var key1 = new GroupKey(1, "test");

        Assert.IsTrue(key1.Equals(key1));
    }

    [TestMethod]
    public void GroupKey_EqualsObject_SameValues_ReturnsTrue()
    {
        var key1 = new GroupKey(1, "test");
        object key2 = new GroupKey(1, "test");

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_EqualsObject_NullObject_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");

        Assert.IsFalse(key1.Equals((object)null));
    }

    [TestMethod]
    public void GroupKey_EqualsObject_SameReference_ReturnsTrue()
    {
        var key1 = new GroupKey(1, "test");

        Assert.IsTrue(key1.Equals((object)key1));
    }

    [TestMethod]
    public void GroupKey_EqualsObject_DifferentType_ReturnsFalse()
    {
        var key1 = new GroupKey(1, "test");

        Assert.IsFalse(key1.Equals("not a key"));
    }

    [TestMethod]
    public void GroupKey_GetHashCode_SameForEqualKeys()
    {
        var key1 = new GroupKey(1, "test");
        var key2 = new GroupKey(1, "test");

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void GroupKey_GetHashCode_HandlesNull()
    {
        var key1 = new GroupKey(null, "test");

        var hash = key1.GetHashCode();
        Assert.AreEqual(hash, key1.GetHashCode());
    }

    [TestMethod]
    public void GroupKey_GetHashCode_AllNulls()
    {
        var key1 = new GroupKey(null, null);
        var key2 = new GroupKey(null, null);

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void GroupKey_ToString_ReturnsFormattedString()
    {
        var key = new GroupKey(1, "test", 3.14);

        var result = key.ToString();

        Assert.Contains("1", result);
        Assert.Contains("test", result);
    }

    [TestMethod]
    public void GroupKey_ToString_HandlesNullValues()
    {
        var key = new GroupKey(null, "test");

        var result = key.ToString();

        Assert.Contains("null", result);
    }

    [TestMethod]
    public void GroupKey_ToString_SingleValue()
    {
        var key = new GroupKey("only");

        var result = key.ToString();

        Assert.AreEqual("only", result);
    }

    #endregion

    #region SchemaRegistry Tests

    [TestMethod]
    public void SchemaRegistry_Register_AddsSchema()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        registry.Register("test", node);

        Assert.AreEqual(1, registry.Count);
        Assert.IsTrue(registry.ContainsSchema("test"));
    }

    [TestMethod]
    public void SchemaRegistry_Register_NullName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => registry.Register(null, node));
    }

    [TestMethod]
    public void SchemaRegistry_Register_EmptyName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => registry.Register("", node));
    }

    [TestMethod]
    public void SchemaRegistry_Register_NullNode_Throws()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register("test", null));
    }

    [TestMethod]
    public void SchemaRegistry_Register_DuplicateName_Throws()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");

        registry.Register("test", node);

        Assert.Throws<InvalidOperationException>(() => registry.Register("test", node));
    }

    [TestMethod]
    public void SchemaRegistry_TryGetSchema_ReturnsTrue_WhenFound()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test", node);

        var result = registry.TryGetSchema("test", out var registration);

        Assert.IsTrue(result);
        Assert.IsNotNull(registration);
        Assert.AreEqual("test", registration.Name);
    }

    [TestMethod]
    public void SchemaRegistry_TryGetSchema_ReturnsFalse_WhenNotFound()
    {
        var registry = new SchemaRegistry();

        var result = registry.TryGetSchema("nonexistent", out var registration);

        Assert.IsFalse(result);
        Assert.IsNull(registration);
    }

    [TestMethod]
    public void SchemaRegistry_GetSchema_ReturnsSchema_WhenFound()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test", node);

        var registration = registry.GetSchema("test");

        Assert.IsNotNull(registration);
        Assert.AreEqual("test", registration.Name);
    }

    [TestMethod]
    public void SchemaRegistry_GetSchema_Throws_WhenNotFound()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetSchema("nonexistent"));
    }

    [TestMethod]
    public void SchemaRegistry_Clear_RemovesAllSchemas()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("test1", node);
        registry.Register("test2", node);

        registry.Clear();

        Assert.AreEqual(0, registry.Count);
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_Throws_WhenSchemaNotFound()
    {
        var registry = new SchemaRegistry();

        Assert.Throws<InvalidOperationException>(() =>
            registry.ValidateReference("nonexistent", "referencing"));
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_Throws_WhenReferencedAfterReferencing()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("referencing", node);
        registry.Register("referenced", node);

        Assert.Throws<InvalidOperationException>(() =>
            registry.ValidateReference("referenced", "referencing"));
    }

    [TestMethod]
    public void SchemaRegistry_ValidateReference_DoesNotThrow_WhenValid()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("referenced", node);
        registry.Register("referencing", node);


        registry.ValidateReference("referenced", "referencing");
    }

    [TestMethod]
    public void SchemaRegistry_Schemas_ReturnsInOrder()
    {
        var registry = new SchemaRegistry();
        var node = new IntegerNode("1");
        registry.Register("first", node);
        registry.Register("second", node);
        registry.Register("third", node);

        var schemas = registry.Schemas;

        Assert.HasCount(3, schemas);
        Assert.AreEqual("first", schemas[0].Name);
        Assert.AreEqual("second", schemas[1].Name);
        Assert.AreEqual("third", schemas[2].Name);
    }

    #endregion

    #region SchemaRegistration Tests

    [TestMethod]
    public void SchemaRegistration_NullName_Throws()
    {
        var node = new IntegerNode("1");

        Assert.Throws<ArgumentNullException>(() => new SchemaRegistration(null, node));
    }

    [TestMethod]
    public void SchemaRegistration_NullNode_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SchemaRegistration("test", null));
    }

    [TestMethod]
    public void SchemaRegistration_IsBinarySchema_ReturnsFalse_ForNonBinaryNode()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        Assert.IsFalse(registration.IsBinarySchema);
    }

    [TestMethod]
    public void SchemaRegistration_IsTextSchema_ReturnsFalse_ForNonTextNode()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        Assert.IsFalse(registration.IsTextSchema);
    }

    [TestMethod]
    public void SchemaRegistration_GeneratedType_CanBeSet()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        registration.GeneratedType = typeof(string);

        Assert.AreEqual(typeof(string), registration.GeneratedType);
    }

    [TestMethod]
    public void SchemaRegistration_GeneratedTypeName_CanBeSet()
    {
        var node = new IntegerNode("1");
        var registration = new SchemaRegistration("test", node);

        registration.GeneratedTypeName = "MyNamespace.MyType";

        Assert.AreEqual("MyNamespace.MyType", registration.GeneratedTypeName);
    }

    #endregion

    #region Scope Tests

    [TestMethod]
    public void Scope_AddScope_CreatesChildScope()
    {
        var parent = new Scope(null, 0, "parent");

        var child = parent.AddScope("child");

        Assert.HasCount(1, parent.Child);
        Assert.AreEqual("child", child.Name);
        Assert.AreSame(parent, child.Parent);
    }

    [TestMethod]
    public void Scope_AddScope_MultipleChildren()
    {
        var parent = new Scope(null, 0, "parent");

        var child1 = parent.AddScope("child1");
        var child2 = parent.AddScope("child2");

        Assert.HasCount(2, parent.Child);
        Assert.AreEqual(0, child1.SelfIndex);
        Assert.AreEqual(1, child2.SelfIndex);
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsTrueForLocalAttribute()
    {
        var scope = new Scope(null, 0);
        scope["test"] = "value";

        Assert.IsTrue(scope.ContainsAttribute("test"));
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsTrueForParentAttribute()
    {
        var parent = new Scope(null, 0);
        parent["test"] = "value";
        var child = parent.AddScope();

        Assert.IsTrue(child.ContainsAttribute("test"));
    }

    [TestMethod]
    public void Scope_ContainsAttribute_ReturnsFalseForMissingAttribute()
    {
        var scope = new Scope(null, 0);

        Assert.IsFalse(scope.ContainsAttribute("nonexistent"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsTrueForSelf()
    {
        var scope = new Scope(null, 0, "myScope");

        Assert.IsTrue(scope.IsInsideNamedScope("myScope"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsTrueForAncestor()
    {
        var grandparent = new Scope(null, 0, "grandparent");
        var parent = grandparent.AddScope("parent");
        var child = parent.AddScope("child");

        Assert.IsTrue(child.IsInsideNamedScope("grandparent"));
    }

    [TestMethod]
    public void Scope_IsInsideNamedScope_ReturnsFalseForMissingScope()
    {
        var scope = new Scope(null, 0, "myScope");

        Assert.IsFalse(scope.IsInsideNamedScope("otherScope"));
    }

    [TestMethod]
    public void Scope_Indexer_ReturnsLocalValue()
    {
        var scope = new Scope(null, 0);
        scope["key"] = "value";

        Assert.AreEqual("value", scope["key"]);
    }

    [TestMethod]
    public void Scope_Indexer_ReturnsParentValue()
    {
        var parent = new Scope(null, 0);
        parent["key"] = "parentValue";
        var child = parent.AddScope();

        Assert.AreEqual("parentValue", child["key"]);
    }

    [TestMethod]
    public void Scope_ScopeSymbolTable_IsNotNull()
    {
        var scope = new Scope(null, 0);

        Assert.IsNotNull(scope.ScopeSymbolTable);
    }

    #endregion

    #region SymbolTable Tests

    [TestMethod]
    public void SymbolTable_AddSymbol_CanBeRetrieved()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();

        table.AddSymbol("key", symbol);
        var result = table.GetSymbol("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_GetSymbolGeneric_ReturnsTypedSymbol()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.GetSymbol<AliasesSymbol>("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsTrue_WhenFound()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.TryGetSymbol<AliasesSymbol>("key", out var foundSymbol);

        Assert.IsTrue(result);
        Assert.AreSame(symbol, foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsFalse_WhenNotFound()
    {
        var table = new SymbolTable();

        var result = table.TryGetSymbol<AliasesSymbol>("key", out var foundSymbol);

        Assert.IsFalse(result);
        Assert.IsNull(foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_TryGetSymbol_ReturnsFalse_WhenWrongType()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.TryGetSymbol<TypeSymbol>("key", out var foundSymbol);

        Assert.IsFalse(result);
        Assert.IsNull(foundSymbol);
    }

    [TestMethod]
    public void SymbolTable_AddOrGetSymbol_CreatesNewWhenNotExists()
    {
        var table = new SymbolTable();

        var result = table.AddOrGetSymbol<AliasesSymbol>("key");

        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void SymbolTable_AddOrGetSymbol_ReturnsExistingWhenExists()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        var result = table.AddOrGetSymbol<AliasesSymbol>("key");

        Assert.AreSame(symbol, result);
    }

    [TestMethod]
    public void SymbolTable_AddSymbolIfNotExist_DoesNotOverwrite()
    {
        var table = new SymbolTable();
        var symbol1 = new AliasesSymbol();
        var symbol2 = new AliasesSymbol();
        table.AddSymbol("key", symbol1);

        table.AddSymbolIfNotExist("key", symbol2);

        Assert.AreSame(symbol1, table.GetSymbol("key"));
    }

    [TestMethod]
    public void SymbolTable_MoveSymbol_MovesToNewKey()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("oldKey", symbol);

        table.MoveSymbol("oldKey", "newKey");

        Assert.AreSame(symbol, table.GetSymbol("newKey"));
    }

    [TestMethod]
    public void SymbolTable_UpdateSymbol_ReplacesSymbol()
    {
        var table = new SymbolTable();
        var symbol1 = new AliasesSymbol();
        var symbol2 = new AliasesSymbol();
        table.AddSymbol("key", symbol1);

        table.UpdateSymbol("key", symbol2);

        Assert.AreSame(symbol2, table.GetSymbol("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsTrue_WhenMatches()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        Assert.IsTrue(table.SymbolIsOfType<AliasesSymbol>("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsFalse_WhenNotMatches()
    {
        var table = new SymbolTable();
        var symbol = new AliasesSymbol();
        table.AddSymbol("key", symbol);

        Assert.IsFalse(table.SymbolIsOfType<TypeSymbol>("key"));
    }

    [TestMethod]
    public void SymbolTable_SymbolIsOfType_ReturnsFalse_WhenKeyNotFound()
    {
        var table = new SymbolTable();

        Assert.IsFalse(table.SymbolIsOfType<AliasesSymbol>("key"));
    }

    #endregion

    #region AliasesSymbol Tests

    [TestMethod]
    public void AliasesSymbol_AddAlias_CanBeFound()
    {
        var symbol = new AliasesSymbol();

        symbol.AddAlias("test");

        Assert.IsTrue(symbol.ContainsAlias("test"));
    }

    [TestMethod]
    public void AliasesSymbol_ContainsAlias_ReturnsFalse_WhenNotFound()
    {
        var symbol = new AliasesSymbol();

        Assert.IsFalse(symbol.ContainsAlias("nonexistent"));
    }

    #endregion
}
