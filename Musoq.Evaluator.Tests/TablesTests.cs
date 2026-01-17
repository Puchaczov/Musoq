using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Plugins;

namespace Musoq.Evaluator.Tests;

/// <summary>
/// Comprehensive tests for Musoq.Evaluator.Tables classes
/// </summary>
[TestClass]
public class TablesTests
{
    #region TableIndex Tests
    
    [TestMethod]
    public void TableIndex_Constructor_ShouldSetColumnName()
    {
        // Arrange & Act
        var index = new TableIndex("TestColumn");
        
        // Assert
        Assert.AreEqual("TestColumn", index.ColumnName);
    }
    
    [TestMethod]
    public void TableIndex_Equals_SameColumnName_ShouldReturnTrue()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        var index2 = new TableIndex("Column1");
        
        // Act & Assert
        Assert.IsTrue(index1.Equals(index2));
    }
    
    [TestMethod]
    public void TableIndex_Equals_DifferentColumnName_ShouldReturnFalse()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        var index2 = new TableIndex("Column2");
        
        // Act & Assert
        Assert.IsFalse(index1.Equals(index2));
    }
    
    [TestMethod]
    public void TableIndex_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        
        // Act & Assert
        Assert.IsFalse(index1.Equals(null));
    }
    
    [TestMethod]
    public void TableIndex_Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        
        // Act & Assert
        Assert.IsTrue(index1.Equals(index1));
    }
    
    [TestMethod]
    public void TableIndex_Equals_Object_ShouldWork()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        object index2 = new TableIndex("Column1");
        
        // Act & Assert
        Assert.IsTrue(index1.Equals(index2));
    }
    
    [TestMethod]
    public void TableIndex_Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var index = new TableIndex("Column1");
        
        // Act & Assert
        Assert.IsFalse(index.Equals("Column1"));
    }
    
    [TestMethod]
    public void TableIndex_GetHashCode_SameColumnName_ShouldBeSame()
    {
        // Arrange
        var index1 = new TableIndex("Column1");
        var index2 = new TableIndex("Column1");
        
        // Act & Assert
        Assert.AreEqual(index1.GetHashCode(), index2.GetHashCode());
    }
    
    [TestMethod]
    public void TableIndex_GetHashCode_NullColumnName_ShouldReturnZero()
    {
        // Arrange
        var index = new TableIndex(null);
        
        // Act & Assert
        Assert.AreEqual(0, index.GetHashCode());
    }
    
    #endregion
    
    #region Key Tests
    
    [TestMethod]
    public void Key_Constructor_ShouldSetValuesAndColumns()
    {
        // Arrange
        var values = new object[] { "A", 1, true };
        var columns = new int[] { 0, 1, 2 };
        
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
        var key1 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        var key2 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        
        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }
    
    [TestMethod]
    public void Key_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        var key2 = new Key(new object[] { "B", 1 }, new int[] { 0, 1 });
        
        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }
    
    [TestMethod]
    public void Key_Equals_DifferentColumns_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        var key2 = new Key(new object[] { "A", 1 }, new int[] { 0, 2 });
        
        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }
    
    [TestMethod]
    public void Key_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new Key(new object[] { "A" }, new int[] { 0 });
        
        // Act & Assert
        Assert.IsFalse(key1.Equals((Key)null));
    }
    
    [TestMethod]
    public void Key_Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var key = new Key(new object[] { "A" }, new int[] { 0 });
        
        // Act & Assert
        Assert.IsTrue(key.Equals(key));
    }
    
    [TestMethod]
    public void Key_Equals_Object_ShouldWork()
    {
        // Arrange
        var key1 = new Key(new object[] { "A" }, new int[] { 0 });
        object key2 = new Key(new object[] { "A" }, new int[] { 0 });
        
        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }
    
    [TestMethod]
    public void Key_Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var key = new Key(new object[] { "A" }, new int[] { 0 });
        
        // Act & Assert
        Assert.IsFalse(key.Equals("key"));
    }
    
    [TestMethod]
    public void Key_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var key = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        
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
        var key1 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        var key2 = new Key(new object[] { "A", 1 }, new int[] { 0, 1 });
        
        // Act & Assert
        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }
    
    [TestMethod]
    public void Key_DoesRowMatchKey_ShouldDelegateToRow()
    {
        // Arrange
        var key = new Key(new object[] { "A" }, new int[] { 0 });
        var row = new ObjectsRow(new object[] { "A", 1 });
        
        // Act
        var result = key.DoesRowMatchKey(row);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void Key_DoesRowMatchKey_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        var key = new Key(new object[] { "B" }, new int[] { 0 });
        var row = new ObjectsRow(new object[] { "A", 1 });
        
        // Act
        var result = key.DoesRowMatchKey(row);
        
        // Assert
        Assert.IsFalse(result);
    }
    
    #endregion
    
    #region ObjectsRow Tests
    
    [TestMethod]
    public void ObjectsRow_Constructor_WithValues_ShouldSetProperties()
    {
        // Arrange
        var values = new object[] { "A", 1, true };
        
        // Act
        var row = new ObjectsRow(values);
        
        // Assert
        Assert.AreEqual(3, row.Count);
        Assert.AreEqual("A", row[0]);
        Assert.AreEqual(1, row[1]);
        Assert.IsTrue((bool?)row[2]);
    }
    
    [TestMethod]
    public void ObjectsRow_Constructor_WithContexts_ShouldSetContexts()
    {
        // Arrange
        var values = new object[] { "A" };
        var contexts = new object[] { "context1" };
        
        // Act
        var row = new ObjectsRow(values, contexts);
        
        // Assert
        Assert.AreEqual(contexts, row.Contexts);
    }
    
    [TestMethod]
    public void ObjectsRow_Constructor_WithLeftAndRightContexts_ShouldConcatenate()
    {
        // Arrange
        var values = new object[] { "A" };
        var leftContexts = new object[] { "left" };
        var rightContexts = new object[] { "right" };
        
        // Act
        var row = new ObjectsRow(values, leftContexts, rightContexts);
        
        // Assert
        Assert.HasCount(2, row.Contexts);
        Assert.AreEqual("left", row.Contexts[0]);
        Assert.AreEqual("right", row.Contexts[1]);
    }
    
    [TestMethod]
    public void ObjectsRow_Constructor_WithNullLeftContext_ShouldHandleCorrectly()
    {
        // Arrange
        var values = new object[] { "A" };
        object[] leftContexts = null;
        var rightContexts = new object[] { "right" };
        
        // Act
        var row = new ObjectsRow(values, leftContexts, rightContexts);
        
        // Assert
        Assert.HasCount(2, row.Contexts);
        Assert.IsNull(row.Contexts[0]);
        Assert.AreEqual("right", row.Contexts[1]);
    }
    
    [TestMethod]
    public void ObjectsRow_Constructor_WithNullRightContext_ShouldHandleCorrectly()
    {
        // Arrange
        var values = new object[] { "A" };
        var leftContexts = new object[] { "left" };
        object[] rightContexts = null;
        
        // Act
        var row = new ObjectsRow(values, leftContexts, rightContexts);
        
        // Assert
        Assert.HasCount(2, row.Contexts);
        Assert.AreEqual("left", row.Contexts[0]);
        Assert.IsNull(row.Contexts[1]);
    }
    
    [TestMethod]
    public void ObjectsRow_Constructor_BothContextsNull_ShouldThrow()
    {
        // Arrange
        var values = new object[] { "A" };
        
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => new ObjectsRow(values, null, null));
    }
    
    [TestMethod]
    public void ObjectsRow_Values_ShouldReturnOriginalArray()
    {
        // Arrange
        var values = new object[] { "A", 1 };
        var row = new ObjectsRow(values);
        
        // Act & Assert
        Assert.AreSame(values, row.Values);
    }
    
    [TestMethod]
    public void ObjectsRow_Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A", 1 });
        var row2 = new ObjectsRow(new object[] { "A", 1 });
        
        // Act & Assert
        Assert.IsTrue(row1.Equals(row2));
    }
    
    [TestMethod]
    public void ObjectsRow_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A", 1 });
        var row2 = new ObjectsRow(new object[] { "B", 1 });
        
        // Act & Assert
        Assert.IsFalse(row1.Equals(row2));
    }
    
    [TestMethod]
    public void ObjectsRow_Equals_DifferentCount_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A", 1 });
        var row2 = new ObjectsRow(new object[] { "A" });
        
        // Act & Assert
        Assert.IsFalse(row1.Equals(row2));
    }
    
    [TestMethod]
    public void ObjectsRow_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A" });
        
        // Act & Assert
        Assert.IsFalse(row1.Equals((Row)null));
    }
    
    [TestMethod]
    public void ObjectsRow_Equals_Object_ShouldWork()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A" });
        object row2 = new ObjectsRow(new object[] { "A" });
        
        // Act & Assert
        Assert.IsTrue(row1.Equals(row2));
    }
    
    [TestMethod]
    public void ObjectsRow_GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var row1 = new ObjectsRow(new object[] { "A", 1 });
        var row2 = new ObjectsRow(new object[] { "A", 1 });
        
        // Act & Assert
        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }
    
    [TestMethod]
    public void ObjectsRow_FitsTheIndex_ShouldCheckKey()
    {
        // Arrange
        var row = new ObjectsRow(new object[] { "A", 1 });
        var key = new Key(new object[] { "A" }, new int[] { 0 });
        
        // Act
        var result = row.FitsTheIndex(key);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void ObjectsRow_CheckWithKey_ShouldMatchKey()
    {
        // Arrange
        var row = new ObjectsRow(new object[] { "A", 1, true });
        var key = new Key(new object[] { 1 }, new int[] { 1 });
        
        // Act
        var result = row.CheckWithKey(key);
        
        // Assert
        Assert.IsTrue(result);
    }
    
    [TestMethod]
    public void ObjectsRow_CheckWithKey_NoMatch_ShouldReturnFalse()
    {
        // Arrange
        var row = new ObjectsRow(new object[] { "A", 1 });
        var key = new Key(new object[] { 2 }, new int[] { 1 });
        
        // Act
        var result = row.CheckWithKey(key);
        
        // Assert
        Assert.IsFalse(result);
    }
    
    #endregion
    
    #region GroupKey Tests
    
    [TestMethod]
    public void GroupKey_Constructor_ShouldSetValues()
    {
        // Arrange & Act
        var key = new GroupKey("A", 1, true);
        
        // Assert
        Assert.HasCount(3, key.Values);
        Assert.AreEqual("A", key.Values[0]);
        Assert.AreEqual(1, key.Values[1]);
        Assert.IsTrue((bool?)key.Values[2]);
    }
    
    [TestMethod]
    public void GroupKey_Equals_SameValues_ShouldReturnTrue()
    {
        // Arrange
        var key1 = new GroupKey("A", 1);
        var key2 = new GroupKey("A", 1);
        
        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_DifferentValues_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new GroupKey("A", 1);
        var key2 = new GroupKey("B", 1);
        
        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_Null_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new GroupKey("A");
        
        // Act & Assert
        Assert.IsFalse(key1.Equals((GroupKey)null));
    }
    
    [TestMethod]
    public void GroupKey_Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var key = new GroupKey("A");
        
        // Act & Assert
        Assert.IsTrue(key.Equals(key));
    }
    
    [TestMethod]
    public void GroupKey_Equals_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        var key1 = new GroupKey(null, "A");
        var key2 = new GroupKey(null, "A");
        
        // Act & Assert - Fixed: Now uses typed Equals(GroupKey) which handles null values correctly
        Assert.IsTrue(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_OneNullOneValue_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new GroupKey(null, "A");
        var key2 = new GroupKey("B", "A");
        
        // Act & Assert - Fixed: Now uses typed Equals(GroupKey) which handles null values correctly
        Assert.IsFalse(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_ValueNull_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new GroupKey("B", "A");
        var key2 = new GroupKey(null, "A");
        
        // Act & Assert
        Assert.IsFalse(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_Object_ShouldWork()
    {
        // Arrange
        var key1 = new GroupKey("A");
        object key2 = new GroupKey("A");
        
        // Act & Assert
        Assert.IsTrue(key1.Equals(key2));
    }
    
    [TestMethod]
    public void GroupKey_Equals_DifferentType_ShouldReturnFalse()
    {
        // Arrange
        var key = new GroupKey("A");
        
        // Act & Assert
        Assert.IsFalse(key.Equals("A"));
    }
    
    [TestMethod]
    public void GroupKey_Equals_DifferentCounts_ShouldReturnFalse()
    {
        // Arrange
        var key1 = new GroupKey("A", "B");
        var key2 = new GroupKey("A");
        
        // Act & Assert - Note: Use Equals(object) as Equals(GroupKey) throws on different lengths
        Assert.IsFalse(key1.Equals((object)key2));
    }
    
    [TestMethod]
    public void GroupKey_ToString_ShouldReturnCommaSeparatedValues()
    {
        // Arrange
        var key = new GroupKey("A", 1, true);
        
        // Act
        var result = key.ToString();
        
        // Assert
        Assert.AreEqual("A,1,True", result);
    }
    
    [TestMethod]
    public void GroupKey_ToString_WithNull_ShouldShowNull()
    {
        // Arrange
        var key = new GroupKey(null, "A");
        
        // Act
        var result = key.ToString();
        
        // Assert
        Assert.Contains("null", result);
    }
    
    [TestMethod]
    public void GroupKey_GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var key1 = new GroupKey("A", 1);
        var key2 = new GroupKey("A", 1);
        
        // Act & Assert
        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }
    
    [TestMethod]
    public void GroupKey_GetHashCode_WithNull_ShouldNotThrow()
    {
        // Arrange
        var key = new GroupKey(null, "A");
        
        // Act & Assert
        var hash = key.GetHashCode(); // Should not throw
        Assert.IsNotNull(hash);
    }
    
    #endregion
    
    #region GroupRow Tests
    
    [TestMethod]
    public void GroupRow_Count_ShouldReturnColumnCount()
    {
        // Arrange
        var group = new Group(null, new[] { "col1" }, new object[] { "value1" });
        
        var columnMapping = new Dictionary<int, string>
        {
            { 0, "col1" }
        };
        
        var row = new GroupRow(group, columnMapping);
        
        // Act & Assert
        Assert.AreEqual(1, row.Count);
    }
    
    [TestMethod]
    public void GroupRow_Indexer_ShouldReturnValueFromGroup()
    {
        // Arrange
        var group = new Group(null, new[] { "col1" }, new object[] { "testValue" });
        
        var columnMapping = new Dictionary<int, string>
        {
            { 0, "col1" }
        };
        
        var row = new GroupRow(group, columnMapping);
        
        // Act
        var result = row[0];
        
        // Assert
        Assert.AreEqual("testValue", result);
    }
    
    [TestMethod]
    public void GroupRow_Values_ShouldReturnAllValues()
    {
        // Arrange
        var group = new Group(null, new[] { "col1", "col2" }, new object[] { "value1", 42 });
        
        var columnMapping = new Dictionary<int, string>
        {
            { 0, "col1" },
            { 1, "col2" }
        };
        
        var row = new GroupRow(group, columnMapping);
        
        // Act
        var values = row.Values;
        
        // Assert
        Assert.HasCount(2, values);
        Assert.AreEqual("value1", values[0]);
        Assert.AreEqual(42, values[1]);
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
        var row = new ObjectsRow(new object[] { "value1" });
        
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
        var row = new ObjectsRow(new object[] { "value1" });
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
        var row = new ObjectsRow(new object[] { "value1" });
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
        var row1 = new ObjectsRow(new object[] { "value1" });
        var row2 = new ObjectsRow(new object[] { "value2" });
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
        var table = new Table("TestTable", [new Column("Col1", typeof(string), 0)]);
        table.Add(new ObjectsRow(new object[] { "value1" }));
        table.Add(new ObjectsRow(new object[] { "value2" }));
        
        // Act
        var count = 0;
        foreach (var row in table)
        {
            count++;
        }
        
        // Assert
        Assert.AreEqual(2, count);
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
