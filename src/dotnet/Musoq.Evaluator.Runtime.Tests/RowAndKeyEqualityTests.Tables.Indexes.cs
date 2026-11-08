using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Evaluator.Tests;

public partial class RowAndKeyEqualityTests
{
    #region IndexedList Additional Coverage Tests

    [TestMethod]
    public void TypedTableQuery_WhenAJoinKeyIsAbsent_ShouldReturnNoRows()
    {
        const string query = "select a.One as One from #schema.first() a inner join #schema.first() b on a.Position = b.Position where b.Position = 999";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
    }

    [TestMethod]
    public void TypedTableQuery_WhenTheKeyMatchesButTheValueDoesNot_ShouldReturnNoRows()
    {
        const string query = "select a.One as One from #schema.first() a inner join #schema.first() b on a.Position = b.Position where a.One = 100";
        using var compiled = CreateAndRunVirtualMachine<ContextInput, ContextLibrary>(query, ContextRows());
        using var table = compiled.Run(TestContext.CancellationToken);
        TableMaterializationTestHelper.AssertColumns(table, ("One", typeof(int)));
        Assert.AreEqual(0, table.Count);
        foreach (var row in table)
            Assert.Fail($"Unexpected row: {row.GetValue<int>(0)}");
    }

    [TestMethod]
    public void Row_Equals_NullVsNonNull_ReturnsFalse()
    {
        var row1 = new TestRow([null!]);
        var row2 = new TestRow(["test"]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_Equals_NonNullVsNull_ReturnsFalse()
    {
        var row1 = new TestRow(["test"]);
        var row2 = new TestRow([null!]);

        Assert.IsFalse(row1.Equals(row2));
    }

    [TestMethod]
    public void Row_GetHashCode_HandlesNullValues()
    {
        var row = new TestRow([null!, "test"]);

        var hash = row.GetHashCode();
        Assert.AreEqual(hash, row.GetHashCode());
    }

    [TestMethod]
    public void TestRow_Count_ReturnsCorrectCount()
    {
        var row = new TestRow([1, 2, 3]);

        Assert.AreEqual(3, row.Count);
    }

    [TestMethod]
    public void TestRow_Values_ReturnsAllValues()
    {
        var values = new object[] { 1, "test", 3.14 };
        var row = new TestRow(values);

        Assert.HasCount(3, row.ReadValues());
        Assert.AreEqual(1, row.ReadValues()[0]);
    }

    #endregion
}
