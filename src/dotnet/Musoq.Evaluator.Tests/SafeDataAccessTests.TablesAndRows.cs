using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

public partial class SafeDataAccessTests
{
    #region VariableTable Tests

    [TestMethod]
    public void VariableTable_Constructor_ShouldSetColumns()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Col1", typeof(int), 0),
            new TestSchemaColumn("Col2", typeof(string), 1)
        };

        // Act
        var table = CreateVariableTable(columns);

        // Assert
        Assert.HasCount(2, table.Columns);
    }

    [TestMethod]
    public void VariableTable_GetColumnByName_ExistingColumn_ReturnsColumn()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Name", typeof(string), 0),
            new TestSchemaColumn("Age", typeof(int), 1)
        };
        var table = CreateVariableTable(columns);

        // Act
        var column = table.GetColumnByName("Name");

        // Assert
        Assert.IsNotNull(column);
        Assert.AreEqual("Name", column.ColumnName);
    }

    [TestMethod]
    public void VariableTable_GetColumnByName_NonExistingColumn_ReturnsNull()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Name", typeof(string), 0)
        };
        var table = CreateVariableTable(columns);

        // Act
        var column = table.GetColumnByName("NonExistent");

        // Assert
        Assert.IsNull(column);
    }

    [TestMethod]
    public void VariableTable_GetColumnsByName_ReturnsMatchingColumns()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Name", typeof(string), 0),
            new TestSchemaColumn("Name", typeof(string), 1), // Duplicate name
            new TestSchemaColumn("Age", typeof(int), 2)
        };
        var table = CreateVariableTable(columns);

        // Act
        var result = table.GetColumnsByName("Name");

        // Assert
        Assert.HasCount(2, result);
    }

    [TestMethod]
    public void VariableTable_Metadata_ShouldReturnCorrectMetadata()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Col1", typeof(int), 0)
        };

        // Act
        var table = CreateVariableTable(columns, typeof(string));

        // Assert
        Assert.AreEqual(typeof(string), table.Metadata.TableEntityType);
    }

    [TestMethod]
    public void VariableTable_Metadata_DefaultType_ShouldBeObject()
    {
        // Arrange
        var columns = new ISchemaColumn[]
        {
            new TestSchemaColumn("Col1", typeof(int), 0)
        };

        // Act
        var table = CreateVariableTable(columns);

        // Assert
        Assert.AreEqual(typeof(object), table.Metadata.TableEntityType);
    }

    #endregion

    #region TestRow Additional Tests

    [TestMethod]
    public void TestRow_Constructor_WithValues_SetsValues()
    {
        // Arrange
        var values = new object[] { 1, "test", 3.14 };

        // Act
        var row = new TestRow(values);

        // Assert
        Assert.AreEqual(3, row.Count);
        Assert.AreEqual(1, row[0]);
        Assert.AreEqual("test", row[1]);
        Assert.AreEqual(3.14, row[2]);
    }

    [TestMethod]
    public void TestRow_Constructor_WithContexts_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        var contexts = new object[] { "ctx1", "ctx2" };

        // Act
        var row = new TestRow(values, contexts);

        // Assert
        Assert.HasCount(2, row.Contexts ?? throw new AssertFailedException("Expected row contexts."));
    }

    [TestMethod]
    public void TestRow_Constructor_WithLeftRightContexts_LeftNull_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        object[]? leftContexts = null;
        var rightContexts = new object[] { "right" };

        // Act
        var row = new TestRow(values, leftContexts, rightContexts);

        // Assert
        Assert.IsNotNull(row.Contexts);
        var contexts = row.Contexts ?? throw new AssertFailedException("Expected row contexts.");
        Assert.IsNull(contexts[0]); // Left is null
    }

    [TestMethod]
    public void TestRow_Constructor_WithLeftRightContexts_RightNull_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        var leftContexts = new object[] { "left" };
        object[]? rightContexts = null;

        // Act
        var row = new TestRow(values, leftContexts, rightContexts);

        // Assert
        Assert.IsNotNull(row.Contexts);
    }

    [TestMethod]
    public void TestRow_Constructor_BothContextsNull_ThrowsException()
    {
        // Arrange
        var values = new object[] { 1 };

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            new TestRow(values, null, null));
    }

    [TestMethod]
    public void TestRow_Values_ReturnsValuesArray()
    {
        // Arrange
        var values = new object[] { 1, 2, 3 };
        var row = new TestRow(values);

        // Act
        var result = row.Values;

        // Assert
        Assert.AreSame(values, result);
    }

    #endregion

    #region Row Additional Tests

    [TestMethod]
    public void Row_Equals_DifferentCount_ReturnsFalse()
    {
        // Arrange
        var row1 = new TestRow([1, 2]);
        var row2 = new TestRow([1, 2, 3]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var row = new TestRow([1]);

        // Act
        var result = row.Equals(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1, "test"]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Row_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1, "other"]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_Object_SameType_ReturnsTrue()
    {
        // Arrange
        var row1 = new TestRow([1]);
        object row2 = new TestRow([1]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Row_GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var row1 = new TestRow([1, 2, 3]);
        var row2 = new TestRow([1, 2, 3]);

        // Act & Assert
        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    #endregion

    #region IndexedList Additional Tests (via Table)

    [TestMethod]
    public void Table_ContainsWithComparer_MatchingValue_ReturnsTrue()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]) { new TestRow([1]), new TestRow([2]) };

        var searchRow = new TestRow([1]);

        // Act
        var result = table.Contains(searchRow, (r1, r2) => (int)r1[0] == (int)r2[0]);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_ContainsWithComparer_NoMatch_ReturnsFalse()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]) { new TestRow([1]) };

        var searchRow = new TestRow([99]);

        // Act
        var result = table.Contains(searchRow, (r1, r2) => (int)r1[0] == (int)r2[0]);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_ContainsWithKey_EmptyTable_ReturnsFalse()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]);

        var row = new TestRow([1]);
        var key = new Key(["key"], [0]);

        // Act
        var result = table.Contains(key, row);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_ContainsKey_NotPresent_ReturnsFalse()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]) { new TestRow([1]) };

        var key = new Key(["nonexistent"], [0]);

        // Act
        var result = table.ContainsKey(key);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Table_TryGetIndexedValues_KeyNotFound_ReturnsFalseWithEmptyList()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]) { new TestRow([1]) };

        var key = new Key(["missing"], [0]);

        // Act
        var result = table.TryGetIndexedValues(key, out var values);

        // Assert
        Assert.IsFalse(result);
        Assert.IsEmpty(values);
    }

    #endregion
}
