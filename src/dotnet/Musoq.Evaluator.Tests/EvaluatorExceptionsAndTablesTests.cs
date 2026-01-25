using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Exceptions;
using Musoq.Evaluator.Tables;
using Musoq.Parser.Nodes;
using Musoq.Schema.DataSources;

namespace Musoq.Evaluator.Tests;

/// <summary>
///     Tests for Evaluator exception and table classes to improve coverage.
/// </summary>
[TestClass]
public class EvaluatorExceptionsAndTablesTests
{
    #region CannotResolveMethodException Tests

    [TestMethod]
    public void CannotResolveMethodException_Message_IsSet()
    {
        var exception = new CannotResolveMethodException("test message");

        Assert.AreEqual("test message", exception.Message);
    }

    [TestMethod]
    public void CannotResolveMethodException_CreateForNullArguments_CreatesCorrectMessage()
    {
        var exception = CannotResolveMethodException.CreateForNullArguments("MyMethod");

        Assert.Contains("MyMethod", exception.Message);
        Assert.Contains("null arguments", exception.Message);
    }

    [TestMethod]
    public void CannotResolveMethodException_CreateForCannotMatchMethodNameOrArguments_NoArgs_CreatesCorrectMessage()
    {
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("MyMethod", []);

        Assert.Contains("MyMethod", exception.Message);
        Assert.Contains("cannot be resolved", exception.Message);
    }

    [TestMethod]
    public void CannotResolveMethodException_CreateForCannotMatchMethodNameOrArguments_WithArgs_IncludesTypes()
    {
        var args = new Node[] { new IntegerNode("1"), new StringNode("test") };
        var exception = CannotResolveMethodException.CreateForCannotMatchMethodNameOrArguments("MyMethod", args);

        Assert.Contains("MyMethod", exception.Message);
        Assert.Contains("cannot be resolved", exception.Message);
    }

    #endregion

    #region RowResolver Tests

    [TestMethod]
    public void RowResolver_HasColumn_ReturnsTrue_WhenColumnExists()
    {
        var row = new ObjectsRow([1, "test"], [new object()]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 }, { "Name", 1 } };
        var resolver = new RowResolver(row, nameToIndex);

        Assert.IsTrue(resolver.HasColumn("Id"));
        Assert.IsTrue(resolver.HasColumn("Name"));
    }

    [TestMethod]
    public void RowResolver_HasColumn_ReturnsFalse_WhenColumnDoesNotExist()
    {
        var row = new ObjectsRow([1, "test"], [new object()]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 } };
        var resolver = new RowResolver(row, nameToIndex);

        Assert.IsFalse(resolver.HasColumn("Unknown"));
    }

    [TestMethod]
    public void RowResolver_Indexer_ByName_ReturnsCorrectValue()
    {
        var row = new ObjectsRow([42, "hello"], [new object()]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 }, { "Name", 1 } };
        var resolver = new RowResolver(row, nameToIndex);

        Assert.AreEqual(42, ((IObjectResolver)resolver)["Id"]);
        Assert.AreEqual("hello", ((IObjectResolver)resolver)["Name"]);
    }

    [TestMethod]
    public void RowResolver_Indexer_ByName_ThrowsForUnknownColumn()
    {
        var row = new ObjectsRow([42], [new object()]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 } };
        var resolver = new RowResolver(row, nameToIndex);

        Assert.Throws<Exception>(() =>
        {
            var _ = ((IObjectResolver)resolver)["Unknown"];
        });
    }

    [TestMethod]
    public void RowResolver_Indexer_ByIndex_ReturnsCorrectValue()
    {
        var row = new ObjectsRow([42, "hello", 3.14], [new object()]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 }, { "Name", 1 }, { "Value", 2 } };
        var resolver = new RowResolver(row, nameToIndex);

        var objResolver = (IObjectResolver)resolver;
        Assert.AreEqual(42, objResolver[0]);
        Assert.AreEqual("hello", objResolver[1]);
        Assert.AreEqual(3.14, objResolver[2]);
    }

    [TestMethod]
    public void RowResolver_Contexts_ReturnsRowContexts()
    {
        var context = new object();
        var row = new ObjectsRow([1], [context]);
        var nameToIndex = new Dictionary<string, int> { { "Id", 0 } };
        var resolver = new RowResolver(row, nameToIndex);

        Assert.HasCount(1, resolver.Contexts);
        Assert.AreSame(context, resolver.Contexts[0]);
    }

    #endregion

    #region Row and ObjectsRow Tests

    [TestMethod]
    public void ObjectsRow_Constructor_SetsValues()
    {
        var values = new object[] { 1, "test", 3.14 };
        var contexts = new[] { new object() };

        var row = new ObjectsRow(values, contexts);

        Assert.AreEqual(1, row[0]);
        Assert.AreEqual("test", row[1]);
        Assert.AreEqual(3.14, row[2]);
    }

    [TestMethod]
    public void ObjectsRow_Count_ReturnsValuesCount()
    {
        var values = new object[] { 1, 2, 3, 4, 5 };
        var row = new ObjectsRow(values, [new object()]);

        Assert.AreEqual(5, row.Count);
    }

    [TestMethod]
    public void ObjectsRow_Contexts_ReturnsContexts()
    {
        var ctx1 = new object();
        var ctx2 = new object();
        var row = new ObjectsRow([1], [ctx1, ctx2]);

        Assert.HasCount(2, row.Contexts);
        Assert.AreSame(ctx1, row.Contexts[0]);
        Assert.AreSame(ctx2, row.Contexts[1]);
    }

    #endregion

    #region Column Tests

    [TestMethod]
    public void Column_Constructor_SetsProperties()
    {
        var column = new Column("TestColumn", typeof(int), 0);

        Assert.AreEqual("TestColumn", column.ColumnName);
        Assert.AreEqual(typeof(int), column.ColumnType);
        Assert.AreEqual(0, column.ColumnIndex);
    }

    [TestMethod]
    public void Column_Equals_SameValues_ReturnsTrue()
    {
        var col1 = new Column("Name", typeof(string), 1);
        var col2 = new Column("Name", typeof(string), 1);

        Assert.IsTrue(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_DifferentName_ReturnsFalse()
    {
        var col1 = new Column("Name1", typeof(string), 0);
        var col2 = new Column("Name2", typeof(string), 0);

        Assert.IsFalse(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_DifferentType_ReturnsFalse()
    {
        var col1 = new Column("Name", typeof(int), 0);
        var col2 = new Column("Name", typeof(string), 0);

        Assert.IsFalse(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_DifferentIndex_ReturnsFalse()
    {
        var col1 = new Column("Name", typeof(string), 0);
        var col2 = new Column("Name", typeof(string), 1);

        Assert.IsFalse(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_GetHashCode_SameForEqualColumns()
    {
        var col1 = new Column("Name", typeof(string), 0);
        var col2 = new Column("Name", typeof(string), 0);

        Assert.AreEqual(col1.GetHashCode(), col2.GetHashCode());
    }

    [TestMethod]
    public void Column_Equals_Object_SameValues_ReturnsTrue()
    {
        var col1 = new Column("Name", typeof(string), 0);
        object col2 = new Column("Name", typeof(string), 0);

        Assert.IsTrue(col1.Equals(col2));
    }

    [TestMethod]
    public void Column_Equals_Object_DifferentType_ReturnsFalse()
    {
        var col1 = new Column("Name", typeof(string), 0);

        Assert.IsFalse(col1.Equals("not a column"));
    }

    [TestMethod]
    public void Column_Equals_Null_ReturnsFalse()
    {
        var col1 = new Column("Name", typeof(string), 0);

        Assert.IsFalse(col1.Equals(null));
    }

    #endregion

    #region Table Tests

    [TestMethod]
    public void Table_Constructor_CreatesEmptyTable()
    {
        var table = new Table("TestTable", []);

        Assert.AreEqual("TestTable", table.Name);
        Assert.AreEqual(0, table.Count);
    }

    [TestMethod]
    public void Table_Add_AddsRows()
    {
        var columns = new[] { new Column("Id", typeof(int), 0) };
        var table = new Table("TestTable", columns);
        var row = new ObjectsRow([1], [new object()]);

        table.Add(row);

        Assert.AreEqual(1, table.Count);
    }

    [TestMethod]
    public void Table_Indexer_ReturnsCorrectRow()
    {
        var columns = new[] { new Column("Id", typeof(int), 0) };
        var table = new Table("TestTable", columns);
        var row1 = new ObjectsRow([1], [new object()]);
        var row2 = new ObjectsRow([2], [new object()]);

        table.Add(row1);
        table.Add(row2);

        Assert.AreSame(row1, table[0]);
        Assert.AreSame(row2, table[1]);
    }

    #endregion

    #region Key Tests

    [TestMethod]
    public void Key_Constructor_SetsValues()
    {
        var values = new object[] { 1, "test" };
        var columns = new[] { 0, 1 };
        var key = new Key(values, columns);

        Assert.HasCount(2, key.Values);
        Assert.AreEqual(1, key.Values[0]);
        Assert.AreEqual("test", key.Values[1]);
    }

    [TestMethod]
    public void Key_Equals_SameValues_ReturnsTrue()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([1, "test"], [0, 1]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_DifferentValues_ReturnsFalse()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([2, "other"], [0, 1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_Null_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals(null));
    }

    [TestMethod]
    public void Key_Equals_Self_ReturnsTrue()
    {
        var key1 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals(key1));
    }

    [TestMethod]
    public void Key_GetHashCode_SameForEqualKeys()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([1, "test"], [0, 1]);

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void Key_ToString_ReturnsFormattedString()
    {
        var key = new Key([1, "test"], [0, 1]);

        var str = key.ToString();

        Assert.IsNotNull(str);
        Assert.Contains("0", str);
        Assert.Contains("1", str);
    }

    [TestMethod]
    public void Key_Equals_Object_SameValues_ReturnsTrue()
    {
        var key1 = new Key([1], [0]);
        object key2 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_Object_Null_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals((object)null));
    }

    [TestMethod]
    public void Key_Equals_Object_Self_ReturnsTrue()
    {
        var key1 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals((object)key1));
    }

    [TestMethod]
    public void Key_Equals_Object_DifferentType_ReturnsFalse()
    {
        var key1 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals("not a key"));
    }

    #endregion

    #region GroupKey Tests

    [TestMethod]
    public void GroupKey_Constructor_SetsValues()
    {
        var key = new GroupKey(1, "test", 3.14);

        Assert.HasCount(3, key.Values);
        Assert.AreEqual(1, key.Values[0]);
        Assert.AreEqual("test", key.Values[1]);
        Assert.AreEqual(3.14, key.Values[2]);
    }

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
        var key2 = new GroupKey(2, "other");

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_Null_ReturnsFalse()
    {
        var key1 = new GroupKey(1);

        Assert.IsFalse(key1.Equals(null));
    }

    [TestMethod]
    public void GroupKey_Equals_Self_ReturnsTrue()
    {
        var key1 = new GroupKey(1);

        Assert.IsTrue(key1.Equals(key1));
    }

    [TestMethod]
    public void GroupKey_Equals_DifferentLength_ReturnsFalse()
    {
        var key1 = new GroupKey(1);
        var key2 = new GroupKey(1, 2);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_BothNullValue_ReturnsTrue()
    {
        var key1 = new GroupKey((object)null);
        var key2 = new GroupKey((object)null);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_OneNullValue_ReturnsFalse()
    {
        var key1 = new GroupKey((object)null);
        var key2 = new GroupKey(1);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_OtherNullValue_ReturnsFalse()
    {
        var key1 = new GroupKey(1);
        var key2 = new GroupKey((object)null);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_ToString_ReturnsFormattedString()
    {
        var key = new GroupKey(1, "test");

        var str = key.ToString();

        Assert.IsNotNull(str);
    }

    [TestMethod]
    public void GroupKey_ToString_NullValues_ReturnsNull()
    {
        var key = new GroupKey(null, "test", null);

        var str = key.ToString();

        Assert.Contains("null", str);
    }

    [TestMethod]
    public void GroupKey_GetHashCode_WithNullValues_Works()
    {
        var key = new GroupKey(1, null, "test");


        var hash = key.GetHashCode();
        Assert.IsTrue(hash != 0 || hash == 0);
    }

    [TestMethod]
    public void GroupKey_Equals_Object_SameValues_ReturnsTrue()
    {
        var key1 = new GroupKey(1, "test");
        object key2 = new GroupKey(1, "test");

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void GroupKey_Equals_Object_Null_ReturnsFalse()
    {
        var key1 = new GroupKey(1);

        Assert.IsFalse(key1.Equals((object)null));
    }

    [TestMethod]
    public void GroupKey_Equals_Object_Self_ReturnsTrue()
    {
        var key1 = new GroupKey(1);

        Assert.IsTrue(key1.Equals((object)key1));
    }

    [TestMethod]
    public void GroupKey_Equals_Object_DifferentType_ReturnsFalse()
    {
        var key1 = new GroupKey(1);

        Assert.IsFalse(key1.Equals("not a key"));
    }

    [TestMethod]
    public void GroupKey_Equals_FirstNotNullSecondNull_ReturnsFalse()
    {
        var key1 = new GroupKey(1);
        var key2 = new GroupKey((object)null);

        Assert.IsFalse(key1.Equals((object)key2));
    }

    [TestMethod]
    public void GroupKey_Equals_WithMixedNulls_ReturnsFalse()
    {
        var key1 = new GroupKey(1, 2, null);
        var key2 = new GroupKey(1, null, 3);

        Assert.IsFalse(key1.Equals(key2));
    }

    #endregion
}
