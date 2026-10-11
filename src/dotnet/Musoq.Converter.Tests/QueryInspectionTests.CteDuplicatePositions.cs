using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CteWildcard_DuplicateNamesPreserveDistinctValuesAcrossRows()
    {
        const string query = "with data as (select v.First as Value, v.Second as Value from values { (First: 1i, Second: 11i), (First: 2i, Second: 22i) } v) select * from data d";
        using var compiled = CompileForExecution(query);
        using var table = compiled.Run();
        var columns = table.Columns.ToArray();

        Assert.HasCount(2, columns);
        Assert.AreEqual("Value", columns[0].ColumnName);
        Assert.AreEqual(typeof(int), columns[0].ColumnType);
        Assert.AreEqual("Value", columns[1].ColumnName);
        Assert.AreEqual(typeof(int), columns[1].ColumnType);
        Assert.AreEqual(2, table.Count);
        Assert.AreEqual(1, table[0].ReadValue(0));
        Assert.AreEqual(11, table[0].ReadValue(1));
        Assert.AreEqual(2, table[1].ReadValue(0));
        Assert.AreEqual(22, table[1].ReadValue(1));
    }

    [TestMethod]
    public void CteWildcard_UnusedLeadingColumnKeepsDuplicateSourcePositions()
    {
        const string query = "with data as (select v.Unused as Unused, v.First as Value, v.Second as Value from values { (Unused: 0i, First: 1i, Second: 2i) } v) select * exclude (Unused) from data d";
        using var compiled = CompileForExecution(query);
        using var table = compiled.Run();

        Assert.HasCount(2, table.Columns.ToArray());
        Assert.AreEqual(1, table.Count);
        Assert.AreEqual(1, table[0].ReadValue(0));
        Assert.AreEqual(2, table[0].ReadValue(1));
    }
}
