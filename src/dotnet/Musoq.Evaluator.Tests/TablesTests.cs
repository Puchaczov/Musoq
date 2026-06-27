using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Comprehensive tests for Musoq.Evaluator.Tables classes
/// </summary>
[TestClass]
public class TablesTests
{
    private static object?[] RequireContexts(TestRow row) =>
        row.Contexts ?? throw new AssertFailedException("Expected row contexts.");

    #region Key Tests

    [TestMethod]
    public void Key_Constructor_ShouldSetValuesAndColumns()
    {
        // Arrange
        var values = new object[] { "A", 1, true };
        var columns = new[] { 0, 1, 2 };

        // Act
        var key = new Key(values, columns);

        // Assert
        Assert.AreEqual(values, key.Values);
        Assert.AreEqual(columns, key.Columns);
    }

    [TestMethod]
    public void Key_Equals_SameKey_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new Key(["A", 1], [0, 1]);
        var key2 = new Key(["A", 1], [0, 1]);

        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(["A", 1], [0, 1]);
        var key2 = new Key(["B", 1], [0, 1]);

        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_DifferentColumns_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(["A", 1], [0, 1]);
        var key2 = new Key(["A", 1], [0, 2]);

        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(["A"], [0]);

        // Act & Assert
        Assert.IsFalse(key1.Equals(null));
    }

    [TestMethod]
    public void Key_Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var key = new Key(["A"], [0]);

        // Act & Assert
        Assert.IsTrue(key.Equals(key));
    }

    [TestMethod]
    public void Key_Equals_Object_ShouldWork()
    {
        // Arrange
        var key1 = new Key(["A"], [0]);
        object key2 = new Key(["A"], [0]);

        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var key = new Key(["A"], [0]);

        // Act & Assert
        Assert.IsFalse(key.Equals("key"));
    }

    [TestMethod]
    public void Key_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var key = new Key(["A", 1], [0, 1]);

        // Act
        var result = key.ToString();

        // Assert
        Assert.Contains("0", result);
        Assert.Contains("A", result);
        Assert.Contains("1", result);
    }

    [TestMethod]
    public void Key_GetHashCode_SameKey_ShouldBeSame()
    {
        // Arrange
        var key1 = new Key(["A", 1], [0, 1]);
        var key2 = new Key(["A", 1], [0, 1]);

        // Act & Assert
        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_ShouldDelegateToRow()
    {
        // Arrange
        var key = new Key(["A"], [0]);
        var row = new TestRow(["A", 1]);

        // Act
        var result = key.DoesRowMatchKey(row);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Key_DoesRowMatchKey_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        var key = new Key(["B"], [0]);
        var row = new TestRow(["A", 1]);

        // Act
        var result = key.DoesRowMatchKey(row);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region TestRow Tests

    [TestMethod]
    public void TestRow_Constructor_WithValues_ShouldSetProperties()
    {
        // Arrange
        var values = new object[] { "A", 1, true };

        // Act
        var row = new TestRow(values);

        // Assert
        Assert.AreEqual(3, row.Count);
        Assert.AreEqual("A", row[0]);
        Assert.AreEqual(1, row[1]);
        Assert.IsTrue((bool?)row[2]);
    }

    [TestMethod]
    public void TestRow_Constructor_WithContexts_ShouldSetContexts()
    {
        // Arrange
        var values = new object[] { "A" };
        var contexts = new object[] { "context1" };

        // Act
        var row = new TestRow(values, contexts);

        // Assert
        Assert.AreEqual(contexts, row.Contexts);
    }

    [TestMethod]
    public void TestRow_Constructor_WithLeftAndRightContexts_ShouldConcatenate()
    {
        // Arrange
        var values = new object[] { "A" };
        var leftContexts = new object[] { "left" };
        var rightContexts = new object[] { "right" };

        // Act
        var row = new TestRow(values, leftContexts, rightContexts);

        // Assert
        var contexts = RequireContexts(row);
        Assert.HasCount(2, contexts);
        Assert.AreEqual("left", contexts[0]);
        Assert.AreEqual("right", contexts[1]);
    }

    [TestMethod]
    public void TestRow_Constructor_WithNullLeftContext_ShouldHandleCorrectly()
    {
        // Arrange
        var values = new object[] { "A" };
        object?[]? leftContexts = null;
        var rightContexts = new object[] { "right" };

        // Act
        var row = new TestRow(values, leftContexts, rightContexts);

        // Assert
        var contexts = RequireContexts(row);
        Assert.HasCount(2, contexts);
        Assert.IsNull(contexts[0]);
        Assert.AreEqual("right", contexts[1]);
    }

    [TestMethod]
    public void TestRow_Constructor_WithNullRightContext_ShouldHandleCorrectly()
    {
        // Arrange
        var values = new object[] { "A" };
        var leftContexts = new object[] { "left" };
        object?[]? rightContexts = null;

        // Act
        var row = new TestRow(values, leftContexts, rightContexts);

        // Assert
        var contexts = RequireContexts(row);
        Assert.HasCount(2, contexts);
        Assert.AreEqual("left", contexts[0]);
        Assert.IsNull(contexts[1]);
    }

    [TestMethod]
    public void TestRow_Constructor_BothContextsNull_ShouldThrow()
    {
        // Arrange
        var values = new object[] { "A" };

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => new TestRow(values, null, null));
    }

    [TestMethod]
    public void TestRow_Values_ShouldReturnOriginalArray()
    {
        // Arrange
        var values = new object[] { "A", 1 };
        var row = new TestRow(values);

        // Act & Assert
        Assert.AreSame(values, row.Values);
    }

    [TestMethod]
    public void TestRow_Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var row1 = new TestRow(["A", 1]);
        var row2 = new TestRow(["A", 1]);

        // Act & Assert
        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void TestRow_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new TestRow(["A", 1]);
        var row2 = new TestRow(["B", 1]);

        // Act & Assert
        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void TestRow_Equals_DifferentCount_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new TestRow(["A", 1]);
        var row2 = new TestRow(["A"]);

        // Act & Assert
        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void TestRow_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new TestRow(["A"]);

        // Act & Assert
        Assert.IsFalse(row1.Equals(null));
    }

    [TestMethod]
    public void TestRow_Equals_Object_ShouldWork()
    {
        // Arrange
        var row1 = new TestRow(["A"]);
        object row2 = new TestRow(["A"]);

        // Act & Assert
        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void TestRow_GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var row1 = new TestRow(["A", 1]);
        var row2 = new TestRow(["A", 1]);

        // Act & Assert
        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    [TestMethod]
    public void TestRow_FitsTheIndex_ShouldCheckKey()
    {
        // Arrange
        var row = new TestRow(["A", 1]);
        var key = new Key(["A"], [0]);

        // Act
        var result = row.FitsTheIndex(key);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TestRow_CheckWithKey_ShouldMatchKey()
    {
        // Arrange
        var row = new TestRow(["A", 1, true]);
        var key = new Key([1], [1]);

        // Act
        var result = row.CheckWithKey(key);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TestRow_CheckWithKey_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        var row = new TestRow(["A", 1]);
        var key = new Key([2], [1]);

        // Act
        var result = row.CheckWithKey(key);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Table Tests

    [TestMethod]
    public void Table_Constructor_ShouldSetName()
    {
        // Arrange & Act
        var table = new Table("TestTable", []);

        // Assert
        Assert.AreEqual("TestTable", table.Name);
    }

    [TestMethod]
    public void Table_Constructor_WithColumns_ShouldAddColumns()
    {
        // Arrange
        var columns = new Column[]
        {
            new("Col1", typeof(string), 0),
            new("Col2", typeof(int), 1)
        };

        // Act
        var table = new Table("TestTable", columns);

        // Assert
        var tableColumns = new List<Column>(table.Columns);
        Assert.HasCount(2, tableColumns);
    }

    [TestMethod]
    public void Table_Add_ShouldAddRow()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var row = new TestRow(["value1"]);

        // Act
        table.Add(row);

        // Assert
        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_Indexer_ShouldReturnRow()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var row = new TestRow(["value1"]);
        table.Add(row);

        // Act
        var retrievedRow = table[0];

        // Assert
        Assert.AreEqual("value1", retrievedRow[0]);
    }

    [TestMethod]
    public void Table_Contains_ExistingRow_ShouldReturnTrue()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var row = new TestRow(["value1"]);
        table.Add(row);

        // Act
        var result = table.Contains(row);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_NonExistingRow_ShouldReturnFalse()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var row1 = new TestRow(["value1"]);
        var row2 = new TestRow(["value2"]);
        table.Add(row1);

        // Act
        var result = table.Contains(row2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_GetEnumerator_ShouldEnumerateRows()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]) { new TestRow(["value1"]), new TestRow(["value2"]) };

        // Act
        var count = 0;
        foreach (var _ in table) count++;

        // Assert
        Assert.AreEqual(2, count);
    }

    [TestMethod]
    public void Table_AddDirectDeferred_ShouldFlushRowsWhenRead()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        IReadOnlyList<TestRow>[] shards =
        [
            [new TestRow(["value1"]), new TestRow(["value2"])],
            [new TestRow(["value3"])]
        ];

        // Act
        table.AddDirectDeferred(shards);

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("value1", table[0][0]);
        Assert.AreEqual("value2", table[1][0]);
        Assert.AreEqual("value3", table[2][0]);
    }

    [TestMethod]
    public void Table_AddDirectDeferred_WithRowShards_ShouldFlushExactCountsWhenRead()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var firstShardRows = new[]
        {
            new TestRow(["value1"]),
            new TestRow(["value2"]),
            new TestRow(["ignored"])
        };
        RowShard<TestRow>[] shards =
        [
            new(firstShardRows, 2),
            new([new TestRow(["value3"])], 1)
        ];

        // Act
        table.AddDirectDeferred(shards);

        // Assert
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("value1", table[0][0]);
        Assert.AreEqual("value2", table[1][0]);
        Assert.AreEqual("value3", table[2][0]);
    }

    [TestMethod]
    public void Table_AddDirectDeferred_WithRowsAndCount_ShouldFlushExactCountWhenRead()
    {
        // Arrange
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        var rows = new[]
        {
            new TestRow(["value1"]),
            new TestRow(["value2"]),
            new TestRow(["ignored"])
        };

        // Act
        table.AddDirectDeferred(rows, 2);

        // Assert
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("value1", table[0][0]);
        Assert.AreEqual("value2", table[1][0]);
    }

    #endregion

    #region Column Tests

    [TestMethod]
    public void Column_Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var column = new Column("TestCol", typeof(string), 0);

        // Assert
        Assert.AreEqual("TestCol", column.ColumnName);
        Assert.AreEqual(typeof(string), column.ColumnType);
        Assert.AreEqual(0, column.ColumnIndex);
    }

    [TestMethod]
    public void Column_Equals_SameColumn_ShouldReturnTrue()
    {
        // Arrange
        var col1 = new Column("Col", typeof(string), 0);
        var col2 = new Column("Col", typeof(string), 0);

        // Act & Assert
        Assert.IsTrue(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_DifferentName_ShouldReturnFalse()
    {
        // Arrange
        var col1 = new Column("Col1", typeof(string), 0);
        var col2 = new Column("Col2", typeof(string), 0);

        // Act & Assert
        Assert.IsFalse(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var col1 = new Column("Col", typeof(string), 0);
        var col2 = new Column("Col", typeof(int), 0);

        // Act & Assert
        Assert.IsFalse(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_GetHashCode_SameColumn_ShouldBeSame()
    {
        // Arrange
        var col1 = new Column("Col", typeof(string), 0);
        var col2 = new Column("Col", typeof(string), 0);

        // Act & Assert
        Assert.AreEqual(col1.GetHashCode(), col2.GetHashCode());
    }

    #endregion
}
