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

    #region IndexedList TryGetIndexedValues

    [TestMethod]
    public void TypedQuery_Lookup_WhenKeyDoesNotExist_ShouldReturnFalseWithEmptyList()
    {
        const string query = "select s.Value as Value from #schema.first() s where s.Value = 999";
        using var compiled = CreateAndRunVirtualMachine<NativeLookupInput>(query, new[] { new NativeLookupInput(1) });
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Value", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
    }

    #endregion

    #region IndexedList Contains(value, comparer)

    [TestMethod]
    public void Table_ContainsWithComparer_WhenMatch_ShouldReturnTrue()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([42]);
        var result = table.Contains(searchRow, (a, b) => (int)a.ReadValue(0) == (int)b.ReadValue(0));

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_ContainsWithComparer_WhenNoMatch_ShouldReturnFalse()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([99]);
        var result = table.Contains(searchRow, (a, b) => (int)a.ReadValue(0) == (int)b.ReadValue(0));

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
    public void Row_Equals_ObjectOverload_WithNonRow_ShouldReturnFalse()
    {
        var row = new TestRow([1, 2]);

        Assert.IsFalse(row.Equals("not a row"));
    }

    [TestMethod]
    public void Row_GetHashCode_WithNullValues_ShouldNotThrow()
    {
        var row = new TestRow([null!, 1, null!]);

        row.GetHashCode();
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
    public void Table_DuplicateLabelsWithDifferentTypes_ShouldPreserveOrdinals()
    {
        using var table = new Table("Test", [
            new Column("Id", typeof(int), 0),
            new Column("Id", typeof(string), 1)
        ]);
        table.AddDirect(new TestRow([7, "seven"]));

        var columns = table.Columns.ToArray();
        Assert.AreEqual(typeof(int), columns[0].ColumnType);
        Assert.AreEqual(typeof(string), columns[1].ColumnType);
        Assert.AreEqual(7, table[0].ReadValue(0));
        Assert.AreEqual("seven", table[0].ReadValue(1));
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

    #endregion

    public sealed record NativeLookupInput(int Value);
}
