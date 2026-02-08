using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Helpers;
using Musoq.Evaluator.Tables;
using Musoq.Schema;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Additional tests for Tables and Helpers with low coverage (Session 4 - Phase 2)
/// </summary>
[TestClass]
public class AdditionalTableAndHelpersTests
{
    #region SafeArrayAccess - Array Tests

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NullArray_ReturnsDefault()
    {
        // Arrange
        int[] array = null;

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_EmptyArray_ReturnsDefault()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_ValidIndex_ReturnsElement()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 2);

        // Assert
        Assert.AreEqual(3, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, -1);

        // Assert
        Assert.AreEqual(5, result); // Last element
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_NegativeIndex_SecondFromEnd()
    {
        // Arrange
        var array = new[] { 1, 2, 3, 4, 5 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, -2);

        // Assert
        Assert.AreEqual(4, result); // Second to last
    }

    [TestMethod]
    public void SafeArrayAccess_GetArrayElement_IndexOutOfBounds_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = SafeArrayAccess.GetArrayElement(array, 10);

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - String Tests

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NullString_ReturnsNullChar()
    {
        // Arrange
        string str = null;

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 0);

        // Assert
        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_EmptyString_ReturnsNullChar()
    {
        // Arrange
        var str = "";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 0);

        // Assert
        Assert.AreEqual('\0', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_ValidIndex_ReturnsChar()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 1);

        // Assert
        Assert.AreEqual('e', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, -1);

        // Assert
        Assert.AreEqual('o', result); // Last character
    }

    [TestMethod]
    public void SafeArrayAccess_GetStringCharacter_OutOfBounds_ReturnsNullChar()
    {
        // Arrange
        var str = "Hi";

        // Act
        var result = SafeArrayAccess.GetStringCharacter(str, 10);

        // Assert
        Assert.AreEqual('\0', result);
    }

    #endregion

    #region SafeArrayAccess - Dictionary Tests

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullDictionary_ReturnsDefault()
    {
        // Arrange
        Dictionary<string, int> dict = null;

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "key");

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_NullKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, null);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_ValidKey_ReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "b");

        // Assert
        Assert.AreEqual(2, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetDictionaryValue_MissingKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "a", 1 } };

        // Act
        var result = SafeArrayAccess.GetDictionaryValue(dict, "missing");

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - List Tests

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NullList_ReturnsDefault()
    {
        // Arrange
        IList<int> list = null;

        // Act
        var result = SafeArrayAccess.GetListElement(list, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_EmptyList_ReturnsDefault()
    {
        // Arrange
        var list = new List<int>();

        // Act
        var result = SafeArrayAccess.GetListElement(list, 0);

        // Assert
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_ValidIndex_ReturnsElement()
    {
        // Arrange
        var list = new List<int> { 10, 20, 30 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, 1);

        // Assert
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var list = new List<int> { 10, 20, 30 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, -1);

        // Assert
        Assert.AreEqual(30, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetListElement_OutOfBounds_ReturnsDefault()
    {
        // Arrange
        var list = new List<int> { 10, 20 };

        // Act
        var result = SafeArrayAccess.GetListElement(list, 100);

        // Assert
        Assert.AreEqual(default, result);
    }

    #endregion

    #region SafeArrayAccess - GetIndexedElement Tests

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullIndexable_ReturnsDefault()
    {
        // Arrange & Act
        var result = SafeArrayAccess.GetIndexedElement(null, 0, typeof(string));

        // Assert - reference types return null
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullIndex_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2, 3 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, null, typeof(string));

        // Assert - reference types return null
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_String_ValidIndex_ReturnsChar()
    {
        // Arrange
        var str = "Hello";

        // Act
        var result = SafeArrayAccess.GetIndexedElement(str, 0, typeof(char));

        // Assert
        Assert.AreEqual('H', result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_ValidIndex_ReturnsElement()
    {
        // Arrange
        var array = new[] { 100, 200, 300 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 1, typeof(int));

        // Assert
        Assert.AreEqual(200, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_NegativeIndex_ReturnsFromEnd()
    {
        // Arrange
        var array = new[] { 100, 200, 300 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, -1, typeof(int));

        // Assert
        Assert.AreEqual(300, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Array_OutOfBounds_ReturnsDefault()
    {
        // Arrange
        var array = new[] { 1, 2 };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 100, typeof(int));

        // Assert
        Assert.AreEqual(0, result); // Default for int
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_EmptyArray_ReturnsDefault()
    {
        // Arrange
        var array = Array.Empty<int>();

        // Act
        var result = SafeArrayAccess.GetIndexedElement(array, 0, typeof(int));

        // Assert
        Assert.AreEqual(0, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Dictionary_ValidKey_ReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { { "key1", 42 } };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(dict, "key1", typeof(int));

        // Assert
        Assert.AreEqual(42, result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_Dictionary_MissingKey_ReturnsDefault()
    {
        // Arrange
        var dict = new Dictionary<string, string> { { "key1", "value1" } };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(dict, "missing", typeof(string));

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_List_ValidIndex_ReturnsElement()
    {
        // Arrange
        var list = new List<string> { "a", "b", "c" };

        // Act
        var result = SafeArrayAccess.GetIndexedElement(list, 1, typeof(string));

        // Assert
        Assert.AreEqual("b", result);
    }

    [TestMethod]
    public void SafeArrayAccess_GetIndexedElement_NullableType_ReturnsNull()
    {
        // Arrange & Act
        var result = SafeArrayAccess.GetIndexedElement(null, 0, typeof(int?));

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region StringHelpers Tests

    [TestMethod]
    public void StringHelpers_GenerateNamespaceIdentifier_ShouldReturnUniqueValues()
    {
        // Act
        var id1 = StringHelpers.GenerateNamespaceIdentifier();
        var id2 = StringHelpers.GenerateNamespaceIdentifier();
        var id3 = StringHelpers.GenerateNamespaceIdentifier();

        // Assert
        Assert.IsLessThan(id2, id1);
        Assert.IsLessThan(id3, id2);
    }

    [TestMethod]
    public void StringHelpers_GenerateNamespaceIdentifier_ShouldBePositive()
    {
        // Act
        var id = StringHelpers.GenerateNamespaceIdentifier();

        // Assert
        Assert.IsGreaterThan(0, id);
    }

    #endregion

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

    #region ObjectsRow Additional Tests

    [TestMethod]
    public void ObjectsRow_Constructor_WithValues_SetsValues()
    {
        // Arrange
        var values = new object[] { 1, "test", 3.14 };

        // Act
        var row = new ObjectsRow(values);

        // Assert
        Assert.AreEqual(3, row.Count);
        Assert.AreEqual(1, row[0]);
        Assert.AreEqual("test", row[1]);
        Assert.AreEqual(3.14, row[2]);
    }

    [TestMethod]
    public void ObjectsRow_Constructor_WithContexts_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        var contexts = new object[] { "ctx1", "ctx2" };

        // Act
        var row = new ObjectsRow(values, contexts);

        // Assert
        Assert.HasCount(2, row.Contexts);
    }

    [TestMethod]
    public void ObjectsRow_Constructor_WithLeftRightContexts_LeftNull_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        object[] leftContexts = null;
        var rightContexts = new object[] { "right" };

        // Act
        var row = new ObjectsRow(values, leftContexts, rightContexts);

        // Assert
        Assert.IsNotNull(row.Contexts);
        Assert.IsNull(row.Contexts[0]); // Left is null
    }

    [TestMethod]
    public void ObjectsRow_Constructor_WithLeftRightContexts_RightNull_SetsContexts()
    {
        // Arrange
        var values = new object[] { 1 };
        var leftContexts = new object[] { "left" };
        object[] rightContexts = null;

        // Act
        var row = new ObjectsRow(values, leftContexts, rightContexts);

        // Assert
        Assert.IsNotNull(row.Contexts);
    }

    [TestMethod]
    public void ObjectsRow_Constructor_BothContextsNull_ThrowsException()
    {
        // Arrange
        var values = new object[] { 1 };

        // Act & Assert
        Assert.Throws<NotSupportedException>(() =>
            new ObjectsRow(values, null, null));
    }

    [TestMethod]
    public void ObjectsRow_Values_ReturnsValuesArray()
    {
        // Arrange
        var values = new object[] { 1, 2, 3 };
        var row = new ObjectsRow(values);

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
        var row1 = new ObjectsRow([1, 2]);
        var row2 = new ObjectsRow([1, 2, 3]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_Null_ReturnsFalse()
    {
        // Arrange
        var row = new ObjectsRow([1]);

        // Act
        var result = row.Equals(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1, "test"]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Row_Equals_DifferentValues_ReturnsFalse()
    {
        // Arrange
        var row1 = new ObjectsRow([1, "test"]);
        var row2 = new ObjectsRow([1, "other"]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Row_Equals_Object_SameType_ReturnsTrue()
    {
        // Arrange
        var row1 = new ObjectsRow([1]);
        object row2 = new ObjectsRow([1]);

        // Act
        var result = row1.Equals(row2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Row_GetHashCode_SameValues_ShouldBeSame()
    {
        // Arrange
        var row1 = new ObjectsRow([1, 2, 3]);
        var row2 = new ObjectsRow([1, 2, 3]);

        // Act & Assert
        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    #endregion

    #region TableHelper Tests

    [TestMethod]
    public void TableHelper_OrderBy_ShouldOrderRowsAscending()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]);
        table.Add(new ObjectsRow([3]));
        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([2]));

        // Act
        var ordered = table.OrderBy(rows => rows.OrderBy(r => (int)r[0]).ToList());

        // Assert
        Assert.AreEqual(3, ordered.Count);
        Assert.AreEqual(1, ordered[0][0]);
        Assert.AreEqual(2, ordered[1][0]);
        Assert.AreEqual(3, ordered[2][0]);
    }

    [TestMethod]
    public void TableHelper_OrderBy_ShouldOrderRowsDescending()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]);
        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([3]));
        table.Add(new ObjectsRow([2]));

        // Act
        var ordered = table.OrderBy(rows => rows.OrderByDescending(r => (int)r[0]).ToList());

        // Assert
        Assert.AreEqual(3, ordered.Count);
        Assert.AreEqual(3, ordered[0][0]);
        Assert.AreEqual(2, ordered[1][0]);
        Assert.AreEqual(1, ordered[2][0]);
    }

    [TestMethod]
    public void TableHelper_OrderBy_EmptyTable_ReturnsEmpty()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]);

        // Act
        var ordered = table.OrderBy(rows => rows.ToList());

        // Assert
        Assert.AreEqual(0, ordered.Count);
    }

    [TestMethod]
    public void TableHelper_OrderBy_PreservesTableName()
    {
        // Arrange
        var table = new Table("MyTable", [
            new Column("Value", typeof(int), 0)
        ]);
        table.Add(new ObjectsRow([1]));

        // Act
        var ordered = table.OrderBy(rows => rows.ToList());

        // Assert
        Assert.AreEqual("MyTable", ordered.Name);
    }

    [TestMethod]
    public void TableHelper_OrderBy_PreservesColumns()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Col1", typeof(int), 0),
            new Column("Col2", typeof(string), 1)
        ]);
        table.Add(new ObjectsRow([1, "a"]));

        // Act
        var ordered = table.OrderBy(rows => rows.ToList());

        // Assert
        var columns = ordered.Columns.ToList();
        Assert.HasCount(2, columns);
        Assert.AreEqual("Col1", columns[0].ColumnName);
        Assert.AreEqual("Col2", columns[1].ColumnName);
    }

    [TestMethod]
    public void TableHelper_OrderBy_WithMultipleColumns_OrdersByFirstThenSecond()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Group", typeof(string), 0),
            new Column("Value", typeof(int), 1)
        ]);
        table.Add(new ObjectsRow(["B", 2]));
        table.Add(new ObjectsRow(["A", 3]));
        table.Add(new ObjectsRow(["A", 1]));

        // Act
        var ordered = table.OrderBy(rows => rows
            .OrderBy(r => (string)r[0])
            .ThenBy(r => (int)r[1])
            .ToList());

        // Assert
        Assert.AreEqual(3, ordered.Count);
        Assert.AreEqual("A", ordered[0][0]);
        Assert.AreEqual(1, ordered[0][1]);
        Assert.AreEqual("A", ordered[1][0]);
        Assert.AreEqual(3, ordered[1][1]);
        Assert.AreEqual("B", ordered[2][0]);
        Assert.AreEqual(2, ordered[2][1]);
    }

    #endregion

    #region IndexedList Additional Tests (via Table)

    [TestMethod]
    public void Table_ContainsWithComparer_MatchingValue_ReturnsTrue()
    {
        // Arrange
        var table = new Table("test", [
            new Column("Value", typeof(int), 0)
        ]);
        table.Add(new ObjectsRow([1]));
        table.Add(new ObjectsRow([2]));

        var searchRow = new ObjectsRow([1]);

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
        ]);
        table.Add(new ObjectsRow([1]));

        var searchRow = new ObjectsRow([99]);

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

        var row = new ObjectsRow([1]);
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
        ]);
        table.Add(new ObjectsRow([1]));

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
        ]);
        table.Add(new ObjectsRow([1]));

        var key = new Key(["missing"], [0]);

        // Act
        var result = table.TryGetIndexedValues(key, out var values);

        // Assert
        Assert.IsFalse(result);
        Assert.IsEmpty(values);
    }

    #endregion

    #region Helper Classes

    private static ISchemaTable CreateVariableTable(ISchemaColumn[] columns, Type metadata = null)
    {
        var type = typeof(Table).Assembly.GetType("Musoq.Evaluator.Tables.VariableTable");
        return (ISchemaTable)Activator.CreateInstance(type, columns, metadata);
    }

    private class TestSchemaColumn : ISchemaColumn
    {
        public TestSchemaColumn(string name, Type type, int index)
        {
            ColumnName = name;
            ColumnType = type;
            ColumnIndex = index;
        }

        public string ColumnName { get; }
        public int ColumnIndex { get; }
        public Type ColumnType { get; }
    }

    #endregion
}
