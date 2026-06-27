using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Musoq.Evaluator.Tables;
using Musoq.Evaluator.Tests.Schema.Generic;

namespace Musoq.Evaluator.Tests;

[TestClass]
public partial class BranchCoverageImprovementTests : GenericEntityTestBase
{
    private static object?[] RequireContexts(TestRow row)
    {
        var contexts = row.Contexts;
        Assert.IsNotNull(contexts);
        return contexts;
    }

    #region IndexedList / Table Contains(key, value) Bug Fix Verification

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyExistsAndValueMatches_ShouldReturnTrue()
    {
        var list = new TestIndexedList();
        var row = new TestRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var result = list.Contains(key, row);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyExistsButValueDiffers_ShouldReturnFalse()
    {
        var list = new TestIndexedList();
        var row = new TestRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var differentRow = new TestRow([99]);
        var result = list.Contains(key, differentRow);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        var list = new TestIndexedList();
        var row = new TestRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        var missingKey = new Key([999], [0]);
        var result = list.Contains(missingKey, row);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void IndexedList_ContainsWithKey_MultipleRows_ShouldFindMatchingRow()
    {
        var list = new TestIndexedList();
        var key = new Key([20], [0]);
        list.AddRowWithIndex(key, new TestRow([10]));
        list.AddRowWithIndex(key, new TestRow([20]));
        list.AddRowWithIndex(key, new TestRow([30]));

        var targetRow = new TestRow([20]);
        var result = list.Contains(key, targetRow);

        Assert.IsTrue(result);
    }

    #endregion

    #region IndexedList TryGetIndexedValues

    [TestMethod]
    public void Table_TryGetIndexedValues_WhenKeyDoesNotExist_ShouldReturnFalseWithEmptyList()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var key = new Key([999], [0]);
        var found = table.TryGetIndexedValues(key, out var values);

        Assert.IsFalse(found);
        Assert.IsEmpty(values);
    }

    #endregion

    #region IndexedList Contains(value, comparer)

    [TestMethod]
    public void Table_ContainsWithComparer_WhenMatch_ShouldReturnTrue()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([42]);
        var result = table.Contains(searchRow, (a, b) => (int)a[0] == (int)b[0]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_ContainsWithComparer_WhenNoMatch_ShouldReturnFalse()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([99]);
        var result = table.Contains(searchRow, (a, b) => (int)a[0] == (int)b[0]);

        Assert.IsFalse(result);
    }

    #endregion

    #region Row Equality Branch Coverage

    [TestMethod]
    public void Row_Equals_WithNull_ShouldReturnFalse()
    {
        var row = new TestRow([1, 2]);

        Assert.IsFalse(row.Equals(null));
    }

    [TestMethod]
    public void Row_Equals_DifferentCount_ShouldReturnFalse()
    {
        var row1 = new TestRow([1, 2]);
        var row2 = new TestRow([1, 2, 3]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_BothNullValues_ShouldBeEqual()
    {
        var row1 = new TestRow([null!, "test"]);
        var row2 = new TestRow([null!, "test"]);

        Assert.IsTrue(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_OneNullOneNotNull_ShouldNotBeEqual()
    {
        var row1 = new TestRow([null!, "test"]);
        var row2 = new TestRow([1, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_FirstNullSecondNotNull_ShouldNotBeEqual()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([null!, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_DifferentValues_ShouldNotBeEqual()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([2, "test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_ObjectOverload_WithNonRow_ShouldReturnFalse()
    {
        var row = new TestRow([1, 2]);

        Assert.IsFalse(row.Equals("not a row"));
    }

    [TestMethod]
    public void Row_GetHashCode_ShouldBeConsistentForEqualRows()
    {
        var row1 = new TestRow([1, "test"]);
        var row2 = new TestRow([1, "test"]);

        Assert.AreEqual(row1.GetHashCode(), row2.GetHashCode());
    }

    [TestMethod]
    public void Row_GetHashCode_WithNullValues_ShouldNotThrow()
    {
        var row = new TestRow([null!, 1, null!]);

        row.GetHashCode();
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenBothNullValues_ShouldMatch()
    {
        var row = new TestRow([null!, "test"]);
        var key = new Key([null], [0]);

        Assert.IsTrue(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenRowNullKeyNotNull_ShouldNotMatch()
    {
        var row = new TestRow([null!, "test"]);
        var key = new Key([42], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    [TestMethod]
    public void Row_CheckWithKey_WhenRowNotNullKeyNull_ShouldNotMatch()
    {
        var row = new TestRow([42, "test"]);
        var key = new Key([null], [0]);

        Assert.IsFalse(row.CheckWithKey(key));
    }

    #endregion

    #region TestRow Context Lazy Evaluation

    [TestMethod]
    public void TestRow_WithLeftAndRightContexts_ShouldLazilyMaterialize()
    {
        var leftCtx = new object[] { "left1", "left2" };
        var rightCtx = new object[] { "right1" };
        var row = new TestRow([1], leftCtx, rightCtx);

        var contexts = RequireContexts(row);

        Assert.HasCount(3, contexts);
        Assert.AreEqual("left1", contexts[0]);
        Assert.AreEqual("left2", contexts[1]);
        Assert.AreEqual("right1", contexts[2]);
    }

    [TestMethod]
    public void TestRow_WithSingleContext_ShouldLazilyMaterialize()
    {
        var context = new object();
        var row = new TestRow([1], context);

        var contexts = RequireContexts(row);

        Assert.HasCount(1, contexts);
        Assert.AreSame(context, contexts[0]);
        Assert.AreSame(contexts, row.Contexts);
    }

    [TestMethod]
    public void TestRow_WithSingleLeftAndRightContexts_ShouldLazilyMaterialize()
    {
        var left = new object();
        var right = new object();
        var row = new TestRow([1], left, right);

        var contexts = RequireContexts(row);

        Assert.HasCount(2, contexts);
        Assert.AreSame(left, contexts[0]);
        Assert.AreSame(right, contexts[1]);
    }

    [TestMethod]
    public void TestRow_WithArrayAndSingleContexts_ShouldLazilyMaterialize()
    {
        var left = new object[] { "left1", "left2" };
        var right = new object();
        var row = new TestRow([1], left, right);

        var contexts = RequireContexts(row);

        Assert.HasCount(3, contexts);
        Assert.AreEqual("left1", contexts[0]);
        Assert.AreEqual("left2", contexts[1]);
        Assert.AreSame(right, contexts[2]);
    }

    [TestMethod]
    public void TestRow_WithSingleAndArrayContexts_ShouldLazilyMaterialize()
    {
        var left = new object();
        var right = new object[] { "right1", "right2" };
        var row = new TestRow([1], left, right);

        var contexts = RequireContexts(row);

        Assert.HasCount(3, contexts);
        Assert.AreSame(left, contexts[0]);
        Assert.AreEqual("right1", contexts[1]);
        Assert.AreEqual("right2", contexts[2]);
    }

    [TestMethod]
    public void TestRow_WithNullLeftContext_ShouldPadWithNull()
    {
        var rightCtx = new object[] { "right1" };
        var row = new TestRow([1], null, rightCtx);

        var contexts = RequireContexts(row);

        Assert.HasCount(2, contexts);
        Assert.IsNull(contexts[0]);
        Assert.AreEqual("right1", contexts[1]);
    }

    [TestMethod]
    public void TestRow_WithNullRightContext_ShouldPadWithNull()
    {
        var leftCtx = new object[] { "left1" };
        var row = new TestRow([1], leftCtx, null);

        var contexts = RequireContexts(row);

        Assert.HasCount(2, contexts);
        Assert.AreEqual("left1", contexts[0]);
        Assert.IsNull(contexts[1]);
    }

    [TestMethod]
    public void TestRow_WithExplicitContexts_ShouldReturnDirectly()
    {
        var ctx = new object[] { "ctx1" };
        var row = new TestRow([1], ctx);

        Assert.AreSame(ctx, row.Contexts);
    }

    [TestMethod]
    public void TestRow_WithNoContexts_ShouldReturnNull()
    {
        var row = new TestRow([1]);

        Assert.IsNull(row.Contexts);
    }

    [TestMethod]
    public void TestRow_WithBothNullContexts_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() => new TestRow([1], null, null));
    }

    #endregion

    #region Key Equality Branch Coverage

    [TestMethod]
    public void Key_Equals_SameReference_ShouldReturnTrue()
    {
        var key = new Key([1], [0]);

        Assert.IsTrue(key.Equals(key));
    }

    [TestMethod]
    public void Key_Equals_Null_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals(null));
    }

    [TestMethod]
    public void Key_Equals_DifferentColumnsLength_ShouldReturnFalse()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([1, 2], [0, 1]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_BothNullValues_ShouldBeEqual()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_OneNullValue_ShouldNotBeEqual()
    {
        var key1 = new Key([null], [0]);
        var key2 = new Key([1], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_OtherValueNull_ShouldNotBeEqual()
    {
        var key1 = new Key([1], [0]);
        var key2 = new Key([null], [0]);

        Assert.IsFalse(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_SameKey_ShouldReturnTrue()
    {
        var key1 = new Key([1], [0]);
        object key2 = new Key([1], [0]);

        Assert.IsTrue(key1.Equals(key2));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_Null_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals((object?)null));
    }

    [TestMethod]
    public void Key_Equals_ObjectOverload_DifferentType_ShouldReturnFalse()
    {
        var key = new Key([1], [0]);

        Assert.IsFalse(key.Equals("not a key"));
    }

    [TestMethod]
    public void Key_GetHashCode_ShouldBeConsistent()
    {
        var key1 = new Key([1, "test"], [0, 1]);
        var key2 = new Key([1, "test"], [0, 1]);

        Assert.AreEqual(key1.GetHashCode(), key2.GetHashCode());
    }

    [TestMethod]
    public void Key_ToString_ShouldFormatCorrectly()
    {
        var key = new Key([42, "abc"], [0, 1]);

        var str = key.ToString();

        Assert.Contains("42", str);
        Assert.Contains("abc", str);
    }

    #endregion

    #region Table Operations Branch Coverage

    [TestMethod]
    public void Table_AddRange_ShouldAddMultipleRows()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);

        table.AddRange([
            new TestRow([1]),
            new TestRow([2]),
            new TestRow([3])
        ]);

        Assert.AreEqual(3, table.Count);
    }

    [TestMethod]
    public void Table_Add_WrongColumnCount_ShouldThrow()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]);

        Assert.Throws<NotSupportedException>(() => table.Add(new TestRow([1, 2])));
    }

    [TestMethod]
    public void Table_Name_ShouldBeAccessible()
    {
        var table = new Table("MyTable", [new Column("Col1", typeof(int), 0)]);

        Assert.AreEqual("MyTable", table.Name);
    }

    [TestMethod]
    public void Table_Columns_ShouldReturnAllColumns()
    {
        var table = new Table("Test", [
            new Column("Col1", typeof(int), 0),
            new Column("Col2", typeof(string), 1)
        ]);

        var columns = table.Columns.ToList();

        Assert.HasCount(2, columns);
    }

    [TestMethod]
    public void Table_ConstructedFromSharedColumns_ShouldNotMutateMetadataArray()
    {
        var idColumn = new Column("Id", typeof(int), 0);
        var sharedColumns = new[] { idColumn };

        var first = new Table("First", sharedColumns);
        var second = new Table("Second", sharedColumns);
        first.AddDirect(new TestRow([1]));
        second.AddDirect(new TestRow([2]));

        Assert.AreSame(idColumn, sharedColumns[0]);
        Assert.AreEqual("Id", sharedColumns[0].ColumnName);
        Assert.AreEqual(typeof(int), sharedColumns[0].ColumnType);
        Assert.AreEqual(0, sharedColumns[0].ColumnIndex);
        Assert.AreEqual(1, first.Count);
        Assert.AreEqual(1, second.Count);
    }

    #endregion

    #region Row DebugInfo Bug Fix Verification

    [TestMethod]
    public void Row_DebugInfo_WhenCountIsZero_ShouldReturnEmpty()
    {
        var row = new TestRow([]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual(string.Empty, debugInfo);
    }

    [TestMethod]
    public void Row_DebugInfo_WithSingleValue_ShouldReturnValue()
    {
        var row = new TestRow([42]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual("42", debugInfo);
    }

    [TestMethod]
    public void Row_DebugInfo_WithMultipleValues_ShouldFormatWithCommas()
    {
        var row = new TestRow([1, "hello", 3]);

        var debugInfo = row.DebugInfo();

        Assert.AreEqual("1, hello, 3", debugInfo);
    }

    [TestMethod]
    public void Row_Equals_ObjectOverload_WithRow_ShouldDelegate()
    {
        var row1 = new TestRow([42]);
        var row2 = new TestRow([42]);

        Assert.IsTrue(row1.Equals((object)row2));
    }

    [TestMethod]
    public void Row_FitsTheIndex_ShouldDelegateToDoesRowMatchKey()
    {
        var row = new TestRow([10]);
        var key = new Key([10], [0]);

        Assert.IsTrue(row.FitsTheIndex(key));
    }

    [TestMethod]
    public void Row_FitsTheIndex_WhenNoMatch_ShouldReturnFalse()
    {
        var row = new TestRow([10]);
        var key = new Key([99], [0]);

        Assert.IsFalse(row.FitsTheIndex(key));
    }

    #endregion

    #region IndexedList Additional Branch Coverage

    [TestMethod]
    public void IndexedList_ContainsValue_EmptyList_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.Contains(new TestRow([1])));
    }

    [TestMethod]
    public void IndexedList_ContainsWithComparer_EmptyList_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.Contains(new TestRow([1]), (_, _) => true));
    }

    [TestMethod]
    public void IndexedList_Indexer_Int_ShouldReturnRow()
    {
        var list = new TestIndexedList();
        var row = new TestRow([42]);
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, row);

        Assert.AreEqual(row, list[0]);
    }

    [TestMethod]
    public void IndexedList_Indexer_Key_ShouldReturnMatchingRows()
    {
        var list = new TestIndexedList();
        var key = new Key([42], [0]);
        var row1 = new TestRow([42]);
        var row2 = new TestRow([42]);
        list.AddRowWithIndex(key, row1);
        list.AddRowWithIndex(key, row2);

        var results = list[key].ToArray();

        Assert.HasCount(2, results);
    }

    [TestMethod]
    public void IndexedList_ContainsKey_WhenKeyExists_ShouldReturnTrue()
    {
        var list = new TestIndexedList();
        var key = new Key([1], [0]);
        list.AddRowWithIndex(key, new TestRow([1]));

        Assert.IsTrue(list.ContainsKey(key));
    }

    [TestMethod]
    public void IndexedList_ContainsKey_WhenKeyMissing_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        Assert.IsFalse(list.ContainsKey(new Key([999], [0])));
    }

    [TestMethod]
    public void IndexedList_TryGetIndexedValues_WhenKeyExists_ShouldReturnRows()
    {
        var list = new TestIndexedList();
        var key = new Key([42], [0]);
        list.AddRowWithIndex(key, new TestRow([42]));

        var found = list.TryGetIndexedValues(key, out var values);

        Assert.IsTrue(found);
        Assert.HasCount(1, values);
    }

    [TestMethod]
    public void IndexedList_TryGetIndexedValues_WhenKeyMissing_ShouldReturnFalse()
    {
        var list = new TestIndexedList();

        var found = list.TryGetIndexedValues(new Key([999], [0]), out _);

        Assert.IsFalse(found);
    }

    [TestMethod]
    public void IndexedList_Count_ShouldReturnRowCount()
    {
        var list = new TestIndexedList();
        var key = new Key([1], [0]);
        list.AddRowWithIndex(key, new TestRow([1]));
        list.AddRowWithIndex(key, new TestRow([2]));

        Assert.AreEqual(2, list.Count);
    }

    #endregion
}
