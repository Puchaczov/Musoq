using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Musoq.Converter.Tests;

public partial class QueryInspectionTests
{
    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderCteWrappedChainedApplyWithDuplicateColumns_ShouldPreserveDuplicateColumnsByPosition()
    {
        var compiled = CompileForExecution(@"
                with expanded as (
                    select i.Name as Name, n.Value as Value, m.Value as Value
                    from #apply.items() i
                    cross apply i.Numbers n
                    cross apply i.Numbers m
                )
                select * from expanded e", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();
        var columns = table.Columns.ToArray();

        Assert.HasCount(3, columns);
        Assert.AreEqual("e.Name", columns[0].ColumnName);
        Assert.AreEqual("e.Value", columns[1].ColumnName);
        Assert.AreEqual("e.Value", columns[2].ColumnName);
        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual("left", table[2][0]);
        Assert.AreEqual(2, table[2][1]);
        Assert.AreEqual(1, table[2][2]);
        Assert.AreEqual("right", table[4][0]);
        Assert.AreEqual(3, table[4][1]);
        Assert.AreEqual(3, table[4][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(5, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(1, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(1, table[1][1]);
        Assert.AreEqual(2, table[1][2]);
        Assert.AreEqual("left", table[2][0]);
        Assert.AreEqual(2, table[2][1]);
        Assert.AreEqual(1, table[2][2]);
        Assert.AreEqual("left", table[3][0]);
        Assert.AreEqual(2, table[3][1]);
        Assert.AreEqual(2, table[3][2]);
        Assert.AreEqual("right", table[4][0]);
        Assert.AreEqual(3, table[4][1]);
        Assert.AreEqual(3, table[4][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderFilteredChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m where n.Value < m.Value", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(1, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderSortedPaginatedChainedCrossApplyPropertySource_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name, n.Value as FirstValue, m.Value as SecondValue from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by FirstValue, SecondValue skip 1 take 2", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        Assert.AreEqual(2, table.Count);
        Assert.AreEqual("left", table[0][0]);
        Assert.AreEqual(1, table[0][1]);
        Assert.AreEqual(2, table[0][2]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual(2, table[1][1]);
        Assert.AreEqual(1, table[1][2]);
    }

    [TestMethod]
    public void CompileForExecution_WhenDefaultExecutionIrRoutingCanRenderNonProjectedChainedCrossApplyOrdering_ShouldRunExecutableQuery()
    {
        var compiled = CompileForExecution("select i.Name from #apply.items() i cross apply i.Numbers n cross apply i.Numbers m order by n.Value desc, m.Value take 3", CreateApplyCandidateSchemaProvider());

        var table = compiled.Run();

        var columns = table.Columns.ToArray();
        Assert.HasCount(1, columns);
        Assert.AreEqual("i.Name", columns[0].ColumnName);
        Assert.AreEqual(3, table.Count);
        Assert.AreEqual("right", table[0][0]);
        Assert.AreEqual("left", table[1][0]);
        Assert.AreEqual("left", table[2][0]);
    }

}
