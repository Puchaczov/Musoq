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

        var result = table.Contains(row, (a, b) => (int)a.ReadValues()[0] == (int)b.ReadValues()[0]);

        Assert.IsTrue(result);
    }

    [TestMethod]
    public void Table_Contains_WithComparer_ReturnsFalseWhenNotFound()
    {
        var table = new Table("Test", [new Column("Col1", typeof(int), 0)]) { new TestRow([42]) };

        var searchRow = new TestRow([100]);
        var result = table.Contains(searchRow, (a, b) => (int)a.ReadValues()[0] == (int)b.ReadValues()[0]);

        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypedTableQuery_WhenTheLookupKeyIsAbsent_ShouldReturnNoRows()
    {
        const string query = "select s.Position as Position from #schema.first() s where s.Position = 999";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
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
    public void TypedTableQuery_WhenNoRowMatchesTheKey_ShouldReturnNoRows()
    {
        const string query = "select s.One as One from #schema.first() s where s.One = 999";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
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
    public void TypedTableQuery_WhenAMaterializedKeyIsAbsent_ShouldReturnNoRows()
    {
        const string query = "with rows as (select s.Position as Position from #schema.first() s) select r.Position as Position from rows r where r.Position = 999";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("Position", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
    }

    [TestMethod]
    public void TypedTableQuery_WhenACompositeLookupDoesNotMatch_ShouldReturnNoRows()
    {
        const string query = "select s.One as One from #schema.first() s where s.Position = 1 and s.One = 42";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
    }

    [TestMethod]
    public void Table_IndexerByIndex_ReturnsCorrectRow()
    {
        var table = new Table("Test", [new Column("Col", typeof(int), 0)]) { new TestRow([100]) };

        var row = table[0];

        Assert.AreEqual(100, row.ReadValue(0));
    }

    #endregion
}
